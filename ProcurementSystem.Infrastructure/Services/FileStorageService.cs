using Microsoft.AspNetCore.Http;
using ProcurementSystem.Core.Interfaces;

namespace ProcurementSystem.Infrastructure.Services
{
    public class FileStorageService : IFileStorageService
    {
        private readonly string _baseUploadPath;
        private readonly string[] _allowedExtensions = { ".pdf", ".jpg", ".jpeg", ".png", ".doc", ".docx", ".xls", ".xlsx", ".zip", ".rar" };
        private const long MaxFileSize = 50 * 1024 * 1024; // 50MB

        public FileStorageService()
        {
            _baseUploadPath = Path.Combine(Directory.GetCurrentDirectory(), "uploads");
        }

        public async Task<string> SaveFileAsync(IFormFile file, string subFolder)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File không hợp lệ.");

            if (file.Length > MaxFileSize)
                throw new InvalidOperationException("Kích thước file vượt quá 50MB.");

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!_allowedExtensions.Contains(ext))
                throw new InvalidOperationException("Định dạng file không được phép. Chỉ chấp nhận .pdf, .jpg, .jpeg, .png, .doc, .docx, .xls, .xlsx, .zip, .rar.");

            var folderPath = Path.Combine(_baseUploadPath, subFolder);
            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            var uniqueFileName = $"{Guid.NewGuid()}{ext}";
            var fullPath = Path.Combine(folderPath, uniqueFileName);

            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return $"/uploads/{subFolder}/{uniqueFileName}";
        }

        public void DeleteFile(string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath)) return;
            var fullPath = Path.Combine(Directory.GetCurrentDirectory(), relativePath.TrimStart('/'));
            if (File.Exists(fullPath)) File.Delete(fullPath);
        }

        public string GetPhysicalFilePath(string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
                throw new ArgumentException("Đường dẫn tệp không hợp lệ.", nameof(relativePath));

            if (Path.IsPathRooted(relativePath))
            {
                var fullRooted = Path.GetFullPath(relativePath);
                var basePathRoot = Path.GetFullPath(Directory.GetCurrentDirectory());
                var tempPathRoot = Path.GetFullPath(Path.GetTempPath());

                if (fullRooted.StartsWith(basePathRoot, StringComparison.OrdinalIgnoreCase) ||
                    fullRooted.StartsWith(tempPathRoot, StringComparison.OrdinalIgnoreCase))
                {
                    return fullRooted;
                }
            }

            var cleaned = relativePath.TrimStart('/', '\\');
            var fullPath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), cleaned));
            var basePath = Path.GetFullPath(Directory.GetCurrentDirectory());

            if (!fullPath.StartsWith(basePath, StringComparison.OrdinalIgnoreCase))
            {
                throw new System.Security.SecurityException("Phát hiện hành vi truy cập tệp bất hợp pháp (Path Traversal).");
            }

            return fullPath;
        }
    
        public Task<ProcurementSystem.Core.DTOs.Common.FileDownloadResult> ResolveDownloadAsync(string storedPath, string originalFileName, string? contentType = null)
        {
            var physicalPath = GetPhysicalFilePath(storedPath);
            return Task.FromResult(ProcurementSystem.Core.DTOs.Common.FileDownloadResult.FromPhysicalPath(
                physicalPath,
                originalFileName,
                contentType ?? "application/octet-stream"));
        }

        public Task<string> GetPreSignedUrlAsync(string storedPath, int expirationMinutes = 15)
        {
            throw new NotSupportedException("Local File Storage không hỗ trợ Pre-signed URL.");
        }

        public bool IsCloudPath(string storedPath)
        {
            if (string.IsNullOrWhiteSpace(storedPath)) return false;
            return storedPath.StartsWith("supabase://", StringComparison.OrdinalIgnoreCase)
                || storedPath.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                || storedPath.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
        }
    }
}
