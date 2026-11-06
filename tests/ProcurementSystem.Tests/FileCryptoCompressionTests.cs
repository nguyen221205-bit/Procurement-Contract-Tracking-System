using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ProcurementSystem.Core.DTOs.Common;
using ProcurementSystem.Infrastructure.Services;
using Xunit;

namespace ProcurementSystem.Tests
{
    public class FileCryptoCompressionTests
    {
        private const string ValidMasterKeyA = "Yj794nLSArXvXh23bxXrCi40ZK4a0/M5lQLBxl8cge0=";
        private const string ValidMasterKeyB = "m8Z4Zf7P5Y0r1K6sN9t2Q4u8V1w3X5y7A9b0C2d4E6g=";

        private readonly FileCryptoCompressionService _cryptoService;

        public FileCryptoCompressionTests()
        {
            var options = Options.Create(new StorageSettings
            {
                Encryption = new StorageEncryptionConfig
                {
                    Enabled = true,
                    MasterKey = ValidMasterKeyA
                }
            });
            _cryptoService = new FileCryptoCompressionService(options, NullLogger<FileCryptoCompressionService>.Instance);
        }

        [Fact]
        public async Task Constructor_WhenMasterKeyMissing_AndEncryptionEnabled_ShouldNotThrow_AndDeferExceptionToExecution()
        {
            // R-B01: Khởi tạo không ném lỗi dù Encryption:Enabled = true nhưng MasterKey rỗng
            var options = Options.Create(new StorageSettings
            {
                Encryption = new StorageEncryptionConfig
                {
                    Enabled = true,
                    MasterKey = "" // Rỗng
                }
            });

            var serviceWithoutKey = new FileCryptoCompressionService(options, NullLogger<FileCryptoCompressionService>.Instance);
            Assert.NotNull(serviceWithoutKey);

            // Nhưng khi thực thi ghi (ProcessForStorageAsync) -> Bắt buộc ném InvalidOperationException
            using var samplePlainStream = new MemoryStream(Encoding.UTF8.GetBytes("Thử nghiệm mã hóa khi thiếu MasterKey"));
            var exProcess = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                serviceWithoutKey.ProcessForStorageAsync(samplePlainStream));
            Assert.Contains("MasterKey", exProcess.Message);

            // Và khi thực thi đọc tệp đã mã hóa PSEN (RestoreFromStorageAsync) -> Bắt buộc ném InvalidOperationException
            using var validInputStream = new MemoryStream(Encoding.UTF8.GetBytes("Dữ liệu cần giải mã"));
            using var encryptedStream = await _cryptoService.ProcessForStorageAsync(validInputStream);
            using var msEnc = new MemoryStream();
            await encryptedStream.CopyToAsync(msEnc);
            msEnc.Position = 0;

            var exRestore = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                serviceWithoutKey.RestoreFromStorageAsync(msEnc));
            Assert.Contains("MasterKey", exRestore.Message);
        }

        [Fact]
        public void Constructor_WhenMasterKeyInvalidBase64_ShouldThrowInvalidOperationException()
        {
            var options = Options.Create(new StorageSettings
            {
                Encryption = new StorageEncryptionConfig
                {
                    Enabled = true,
                    MasterKey = "This is definitely not a base64 string!!!"
                }
            });

            var ex = Assert.Throws<InvalidOperationException>(() =>
                new FileCryptoCompressionService(options, NullLogger<FileCryptoCompressionService>.Instance));

            Assert.Contains("Base64", ex.Message);
        }

        [Fact]
        public void Constructor_WhenMasterKeyWrongLength_ShouldThrowInvalidOperationException()
        {
            // 16 bytes thay vì 32 bytes
            var shortKey = Convert.ToBase64String(new byte[16]);
            var options = Options.Create(new StorageSettings
            {
                Encryption = new StorageEncryptionConfig
                {
                    Enabled = true,
                    MasterKey = shortKey
                }
            });

            var ex = Assert.Throws<InvalidOperationException>(() =>
                new FileCryptoCompressionService(options, NullLogger<FileCryptoCompressionService>.Instance));

            Assert.Contains("32 bytes", ex.Message);
        }

        [Fact]
        public async Task ProcessForStorage_ThenRestoreFromStorage_ShouldRestoreOriginalBytesExactly()
        {
            // Arrange: Tạo dữ liệu mẫu giả lập hồ sơ dự thầu dung lượng ~50KB
            var sampleText = "HỒ SƠ DỰ THẦU ĐIỆN TỬ (E-HSDT) - BẢO MẬT TUYỆT ĐỐI\n";
            var builder = new StringBuilder();
            for (int i = 0; i < 1000; i++)
            {
                builder.AppendLine($"{i}: {sampleText}");
            }
            var originalBytes = Encoding.UTF8.GetBytes(builder.ToString());
            var originalSha256 = Convert.ToHexString(SHA256.HashData(originalBytes)).ToLowerInvariant();

            // Act 1: Nén GZip và mã hóa AES-256-CBC
            using var inputStream = new MemoryStream(originalBytes);
            using var encryptedStream = await _cryptoService.ProcessForStorageAsync(inputStream);
            using var msEnc = new MemoryStream();
            await encryptedStream.CopyToAsync(msEnc);
            var encryptedBytes = msEnc.ToArray();

            // Assert 1: Dữ liệu đã mã hóa phải khác hoàn toàn dữ liệu gốc
            Assert.NotEqual(originalBytes, encryptedBytes);
            var encryptedText = Encoding.UTF8.GetString(encryptedBytes);
            Assert.DoesNotContain("HỒ SƠ DỰ THẦU", encryptedText);

            // Act 2: Giải mã và giải nén ngược lại
            using var decryptInputStream = new MemoryStream(encryptedBytes);
            using var decryptedStream = await _cryptoService.RestoreFromStorageAsync(decryptInputStream);
            using var msDec = new MemoryStream();
            await decryptedStream.CopyToAsync(msDec);
            var restoredBytes = msDec.ToArray();
            var restoredSha256 = Convert.ToHexString(SHA256.HashData(restoredBytes)).ToLowerInvariant();

            // Assert 2: Dữ liệu khôi phục phải khớp 100% mã băm SHA256 với tệp gốc ban đầu
            Assert.Equal(originalSha256, restoredSha256);
            Assert.Equal(originalBytes.Length, restoredBytes.Length);
        }

        [Fact]
        public async Task RestoreFromStorage_WhenDecryptedWithWrongKey_ShouldThrowCryptographicException()
        {
            // Arrange: Mã hóa bằng Khóa A
            var originalBytes = Encoding.UTF8.GetBytes("Tài liệu bảo mật cực kỳ quan trọng");
            using var inputStream = new MemoryStream(originalBytes);
            using var encryptedStream = await _cryptoService.ProcessForStorageAsync(inputStream);
            using var msEnc = new MemoryStream();
            await encryptedStream.CopyToAsync(msEnc);
            var encryptedBytes = msEnc.ToArray();

            // Khởi tạo Service thứ hai với Khóa B khác hoàn toàn Khóa A
            var optionsB = Options.Create(new StorageSettings
            {
                Encryption = new StorageEncryptionConfig
                {
                    Enabled = true,
                    MasterKey = ValidMasterKeyB
                }
            });
            var cryptoServiceB = new FileCryptoCompressionService(optionsB, NullLogger<FileCryptoCompressionService>.Instance);

            // Act & Assert: Cố gắng giải mã tệp của Khóa A bằng Khóa B -> Bắt buộc phải ném CryptographicException
            using var decryptStream = new MemoryStream(encryptedBytes);
            await Assert.ThrowsAsync<CryptographicException>(() => cryptoServiceB.RestoreFromStorageAsync(decryptStream));
        }

        [Fact]
        public async Task RestoreFromStorage_WhenUnencryptedData_ShouldPassThroughSafely()
        {
            // Arrange: Dữ liệu cũ chưa mã hóa (legacy plain data)
            var plainData = Encoding.UTF8.GetBytes("Tài liệu cũ chưa qua mã hóa cloud");
            using var inputStream = new MemoryStream(plainData);

            // Act: Gọi hàm giải mã
            using var outputStream = await _cryptoService.RestoreFromStorageAsync(inputStream);
            using var ms = new MemoryStream();
            await outputStream.CopyToAsync(ms);
            var outputBytes = ms.ToArray();

            // Assert: Hệ thống tự nhận diện không có header mã hóa và trả về nguyên vẹn (backward compatibility)
            Assert.Equal(plainData, outputBytes);
        }

        #region R-B03 Truncated PSEN & Edge Cases Tests

        [Fact]
        public async Task RestoreFromStorage_WhenPsenHeaderOnly_4Bytes_ShouldThrowInvalidDataException()
        {
            // Input chỉ có 4 bytes "PSEN"
            var bytes = new byte[] { 0x50, 0x53, 0x45, 0x4E };
            using var ms = new MemoryStream(bytes);

            var ex = await Assert.ThrowsAsync<InvalidDataException>(() => _cryptoService.RestoreFromStorageAsync(ms));
            Assert.Contains("bị cắt cụt", ex.Message);
        }

        [Fact]
        public async Task RestoreFromStorage_WhenPsenTruncated_6Bytes_ShouldThrowInvalidDataException()
        {
            // Input 6 bytes ("PSEN" + 2 bytes rác)
            var bytes = new byte[] { 0x50, 0x53, 0x45, 0x4E, 0xAA, 0xBB };
            using var ms = new MemoryStream(bytes);

            var ex = await Assert.ThrowsAsync<InvalidDataException>(() => _cryptoService.RestoreFromStorageAsync(ms));
            Assert.Contains("bị cắt cụt", ex.Message);
        }

        [Fact]
        public async Task RestoreFromStorage_WhenPsenHeaderAndIvOnly_NoCiphertext_20Bytes_ShouldThrowInvalidDataException()
        {
            // Input 20 bytes ("PSEN" + 16 bytes IV), không có ciphertext payload
            var bytes = new byte[20];
            bytes[0] = 0x50; bytes[1] = 0x53; bytes[2] = 0x45; bytes[3] = 0x4E;
            for (int i = 4; i < 20; i++) bytes[i] = 0x12;

            using var ms = new MemoryStream(bytes);

            var ex = await Assert.ThrowsAsync<InvalidDataException>(() => _cryptoService.RestoreFromStorageAsync(ms));
            Assert.Contains("không có nội dung ciphertext", ex.Message);
        }

        [Fact]
        public async Task RestoreFromStorage_WhenEmptyStream_0Bytes_ShouldPassThroughSafely()
        {
            // Input rỗng (0 bytes)
            var bytes = Array.Empty<byte>();
            using var ms = new MemoryStream(bytes);

            using var outputStream = await _cryptoService.RestoreFromStorageAsync(ms);
            Assert.Equal(0, outputStream.Length);
        }

        [Fact]
        public async Task RestoreFromStorage_WhenLessThan4Bytes_3Bytes_ShouldPassThroughSafely()
        {
            // Input 3 bytes "ABC" (< 4 bytes header)
            var bytes = Encoding.UTF8.GetBytes("ABC");
            using var ms = new MemoryStream(bytes);

            using var outputStream = await _cryptoService.RestoreFromStorageAsync(ms);
            using var msOut = new MemoryStream();
            await outputStream.CopyToAsync(msOut);

            Assert.Equal(bytes, msOut.ToArray());
        }

        #endregion

        [Fact]
        public void ComputeSha256_ShouldReturnConsistentHash()
        {
            var data = Encoding.UTF8.GetBytes("Thực tập tốt nghiệp 2026");
            var expected = Convert.ToHexString(SHA256.HashData(data)).ToLowerInvariant();

            using var ms = new MemoryStream(data);
            var actual = _cryptoService.ComputeSha256(ms);

            Assert.Equal(expected, actual.ToLowerInvariant());
        }
    }
}
