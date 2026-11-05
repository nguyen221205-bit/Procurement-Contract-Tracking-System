using System.IO.Compression;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProcurementSystem.Core.DTOs.Common;
using ProcurementSystem.Core.Interfaces;

namespace ProcurementSystem.Infrastructure.Services
{
    public class FileCryptoCompressionService : IFileCryptoCompressionService
    {
        private readonly StorageSettings _settings;
        private readonly ILogger<FileCryptoCompressionService> _logger;
        private readonly byte[]? _masterKey;

        private static readonly byte[] EncryptedHeader = new byte[] { 0x50, 0x53, 0x45, 0x4E }; // "PSEN" (Procurement System Encrypted)

        public FileCryptoCompressionService(
            IOptions<StorageSettings> settings,
            ILogger<FileCryptoCompressionService> logger)
        {
            _settings = settings.Value;
            _logger = logger;

            var keyString = _settings.Encryption?.MasterKey;
            var isEncryptionEnabled = _settings.Encryption?.Enabled == true;

            if (!string.IsNullOrWhiteSpace(keyString))
            {
                byte[] rawKey;
                try
                {
                    rawKey = Convert.FromBase64String(keyString.Trim());
                }
                catch (FormatException ex)
                {
                    _logger.LogError("Cấu hình StorageSettings:Encryption:MasterKey không đúng định dạng Base64 hợp lệ.");
                    throw new InvalidOperationException(
                        "Cấu hình StorageSettings:Encryption:MasterKey không đúng định dạng Base64 hợp lệ. " +
                        "Khóa mã hóa chính bắt buộc phải là chuỗi Base64 giải mã đúng 32 bytes (256 bits).", ex);
                }

                if (rawKey.Length != 32)
                {
                    _logger.LogError("Cấu hình StorageSettings:Encryption:MasterKey có độ dài {Length} bytes, không đúng chuẩn 32 bytes (256 bits).", rawKey.Length);
                    throw new InvalidOperationException(
                        $"Cấu hình StorageSettings:Encryption:MasterKey không hợp lệ. Khóa mã hóa sau khi giải mã Base64 có độ dài {rawKey.Length} bytes, " +
                        "trong khi thuật toán AES-256 yêu cầu chính xác 32 bytes (256 bits).");
                }

                _masterKey = rawKey;
            }
            else
            {
                // Khi bật mã hóa, MasterKey là bắt buộc; loại bỏ hoàn toàn fallback key mặc định và DeriveKeyFromSeed
                if (isEncryptionEnabled)
                {
                    _logger.LogError("Tính năng mã hóa tài liệu đang được bật (StorageSettings:Encryption:Enabled = true) nhưng chưa cấu hình StorageSettings:Encryption:MasterKey.");
                    throw new InvalidOperationException(
                        "Cấu hình StorageSettings:Encryption:MasterKey là bắt buộc khi bật tính năng mã hóa lưu trữ (StorageSettings:Encryption:Enabled = true). " +
                        "Khóa mã hóa chính bắt buộc phải là chuỗi Base64 giải mã đúng 32 bytes (256 bits). " +
                        "Vui lòng thiết lập khóa hợp lệ thông qua biến môi trường hoặc User Secrets.");
                }

                _masterKey = null;
            }
        }

        public async Task<Stream> ProcessForStorageAsync(Stream inputStream)
        {
            if (_masterKey == null)
            {
                throw new InvalidOperationException(
                    "Không thể mã hóa tệp dữ liệu vì hệ thống chưa được cấu hình MasterKey 32 bytes Base64 hợp lệ.");
            }

            if (inputStream.CanSeek)
            {
                inputStream.Position = 0;
            }

            // 1. Nén luồng dữ liệu bằng GZip
            using var compressedStream = new MemoryStream();
            using (var gzipStream = new GZipStream(compressedStream, CompressionLevel.Optimal, leaveOpen: true))
            {
                await inputStream.CopyToAsync(gzipStream);
            }
            compressedStream.Position = 0;

            // 2. Mã hóa dữ liệu nén bằng AES-256-CBC với IV ngẫu nhiên 16 bytes
            var outputStream = new MemoryStream();

            // Ghi 4 bytes Header nhận diện tệp đã mã hóa ("PSEN")
            await outputStream.WriteAsync(EncryptedHeader, 0, EncryptedHeader.Length);

            using (var aes = Aes.Create())
            {
                aes.Key = _masterKey;
                aes.GenerateIV();
                var iv = aes.IV;

                // Ghi 16 bytes IV vào sau Header
                await outputStream.WriteAsync(iv, 0, iv.Length);

                using (var encryptor = aes.CreateEncryptor())
                using (var cryptoStream = new CryptoStream(outputStream, encryptor, CryptoStreamMode.Write, leaveOpen: true))
                {
                    await compressedStream.CopyToAsync(cryptoStream);
                    cryptoStream.FlushFinalBlock();
                }
            }

            outputStream.Position = 0;
            return outputStream;
        }

        public async Task<Stream> RestoreFromStorageAsync(Stream encryptedStream)
        {
            MemoryStream memorySource;
            bool shouldDisposeMemorySource = false;
            if (encryptedStream is MemoryStream ms)
            {
                memorySource = ms;
            }
            else
            {
                memorySource = new MemoryStream();
                if (encryptedStream.CanSeek)
                {
                    encryptedStream.Position = 0;
                }
                await encryptedStream.CopyToAsync(memorySource);
                shouldDisposeMemorySource = true;
            }

            memorySource.Position = 0;

            // Kiểm tra kích thước tối thiểu: 4 bytes Header + 16 bytes IV = 20 bytes
            if (memorySource.Length < EncryptedHeader.Length + 16)
            {
                // Tệp quá ngắn hoặc không có Header mã hóa -> trả về dữ liệu plain gốc (backward compatibility)
                var passthroughStream = new MemoryStream();
                memorySource.Position = 0;
                await memorySource.CopyToAsync(passthroughStream);
                passthroughStream.Position = 0;
                if (shouldDisposeMemorySource) memorySource.Dispose();
                return passthroughStream;
            }

            // Đọc và kiểm tra 4 bytes Header
            var headerBuffer = new byte[EncryptedHeader.Length];
            int headerRead = await memorySource.ReadAsync(headerBuffer, 0, headerBuffer.Length);
            if (headerRead != EncryptedHeader.Length || !headerBuffer.SequenceEqual(EncryptedHeader))
            {
                // Không chứa Header "PSEN" -> Dữ liệu legacy plain data -> Trả về stream gốc
                var passthroughStream = new MemoryStream();
                memorySource.Position = 0;
                await memorySource.CopyToAsync(passthroughStream);
                passthroughStream.Position = 0;
                if (shouldDisposeMemorySource) memorySource.Dispose();
                return passthroughStream;
            }

            // Tệp có Header "PSEN" -> Bắt buộc phải có _masterKey để giải mã
            if (_masterKey == null)
            {
                if (shouldDisposeMemorySource) memorySource.Dispose();
                throw new InvalidOperationException(
                    "Tệp dữ liệu đã được mã hóa bảo mật nhưng hệ thống hiện chưa được cấu hình MasterKey hợp lệ để giải mã.");
            }

            try
            {
                // 1. Đọc 16 bytes IV
                var iv = new byte[16];
                int ivRead = 0;
                while (ivRead < 16)
                {
                    int read = await memorySource.ReadAsync(iv, ivRead, 16 - ivRead);
                    if (read == 0) throw new InvalidDataException("Tệp bị cắt cụt, không đủ 16 bytes IV.");
                    ivRead += read;
                }

                // 2. Giải mã AES-256-CBC
                var decryptedCompressedStream = new MemoryStream();
                using (var aes = Aes.Create())
                {
                    aes.Key = _masterKey;
                    aes.IV = iv;

                    using (var decryptor = aes.CreateDecryptor())
                    using (var cryptoStream = new CryptoStream(memorySource, decryptor, CryptoStreamMode.Read, leaveOpen: true))
                    {
                        await cryptoStream.CopyToAsync(decryptedCompressedStream);
                    }
                }

                decryptedCompressedStream.Position = 0;

                // 3. Giải nén GZip để lấy tệp gốc
                var originalStream = new MemoryStream();
                using (var gzipStream = new GZipStream(decryptedCompressedStream, CompressionMode.Decompress, leaveOpen: true))
                {
                    await gzipStream.CopyToAsync(originalStream);
                }

                originalStream.Position = 0;
                if (shouldDisposeMemorySource) memorySource.Dispose();
                return originalStream;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không thể giải mã hoặc giải nén tệp lưu trữ bảo mật. Khóa mã hóa không khớp hoặc tệp bị hỏng.");
                if (shouldDisposeMemorySource) memorySource.Dispose();
                throw new CryptographicException(
                    "Không thể giải mã tệp dữ liệu đã mã hóa. Khóa mã hóa không chính xác hoặc nội dung tệp đã bị hư hỏng / sửa đổi.", ex);
            }
        }

        public bool ValidateMagicBytes(Stream stream, string extension)
        {
            if (stream == null || !stream.CanRead) return false;

            var originalPos = stream.CanSeek ? stream.Position : 0;
            try
            {
                if (stream.CanSeek) stream.Position = 0;

                var buffer = new byte[8];
                int read = stream.Read(buffer, 0, buffer.Length);
                if (read < 4) return false;

                var ext = extension.ToLowerInvariant();
                return ext switch
                {
                    ".pdf" => buffer[0] == 0x25 && buffer[1] == 0x50 && buffer[2] == 0x44 && buffer[3] == 0x46, // %PDF
                    ".png" => buffer[0] == 0x89 && buffer[1] == 0x50 && buffer[2] == 0x4E && buffer[3] == 0x47, // .PNG
                    ".jpg" or ".jpeg" => buffer[0] == 0xFF && buffer[1] == 0xD8 && buffer[2] == 0xFF,
                    ".zip" or ".docx" or ".xlsx" => buffer[0] == 0x50 && buffer[1] == 0x4B && buffer[2] == 0x03 && buffer[3] == 0x04, // PK..
                    ".doc" or ".xls" => buffer[0] == 0xD0 && buffer[1] == 0xCF && buffer[2] == 0x11 && buffer[3] == 0xE0, // OLE2
                    ".rar" => buffer[0] == 0x52 && buffer[1] == 0x61 && buffer[2] == 0x72 && buffer[3] == 0x21, // Rar!
                    _ => true
                };
            }
            finally
            {
                if (stream.CanSeek) stream.Position = originalPos;
            }
        }

        public string ComputeSha256(Stream stream)
        {
            var originalPos = stream.CanSeek ? stream.Position : 0;
            try
            {
                if (stream.CanSeek) stream.Position = 0;
                var hash = SHA256.HashData(stream);
                return Convert.ToHexString(hash).ToLowerInvariant();
            }
            finally
            {
                if (stream.CanSeek) stream.Position = originalPos;
            }
        }
    }
}
