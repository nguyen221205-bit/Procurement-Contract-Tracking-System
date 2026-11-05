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
        public void Constructor_WhenMasterKeyMissing_AndEncryptionEnabled_ShouldThrowInvalidOperationException()
        {
            var options = Options.Create(new StorageSettings
            {
                Encryption = new StorageEncryptionConfig
                {
                    Enabled = true,
                    MasterKey = "" // Rỗng
                }
            });

            var ex = Assert.Throws<InvalidOperationException>(() =>
                new FileCryptoCompressionService(options, NullLogger<FileCryptoCompressionService>.Instance));

            Assert.Contains("StorageSettings:Encryption:MasterKey", ex.Message);
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
