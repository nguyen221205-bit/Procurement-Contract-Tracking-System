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
        private readonly byte[] _masterKey;

        public FileCryptoCompressionService(
            IOptions<StorageSettings> settings,
            ILogger<FileCryptoCompressionService> logger)
        {
            _settings = settings.Value;
            _logger = logger;

            var keyString = _settings.Encryption?.MasterKey;
            if (!string.IsNullOrWhiteSpace(keyString))
            {
                try
                {
                    _masterKey = Convert.FromBase64String(keyString);
                    if (_masterKey.Length != 32)
                    {
                        throw new ArgumentException("MasterKey phải có độ dài đúng 256 bits (32 bytes).");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "MasterKey không hợp lệ, đang sử dụng fallback key dẫn xuất.");
                    _masterKey = DeriveKeyFromSeed(keyString);
                }
            }
            else
            {
                // Fallback deterministic 256-bit key để đảm bảo hệ thống không bị crash nếu chưa cấu hình
                _masterKey = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes("ProcurementSystem_Default_AES256_MasterKey_2026"));
            }
        }

        private static readonly byte[] EncryptedHeader = new byte[] { 0x50, 0x53, 0x45, 0x4E }; // "PSEN" (Procurement System Encrypted)

        private static byte[] DeriveKeyFromSeed(string seed)
        {
            return SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(seed));
        }

        public async Task<Stream> ProcessForStorageAsync(Stream inputStream)
        {
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
                _logger.LogWarning(ex, "Không thể giải mã/giải nén tệp lưu trữ. Đang trả về dữ liệu nguyên bản dạng fallback.");
                var fallbackStream = new MemoryStream();
                memorySource.Position = 0;
                await memorySource.CopyToAsync(fallbackStream);
                fallbackStream.Position = 0;
                if (shouldDisposeMemorySource) memorySource.Dispose();
                return fallbackStream;
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
                    _ => true // Định dạng khác cho phép nếu trong danh sách mở rộng hợp lệ
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
