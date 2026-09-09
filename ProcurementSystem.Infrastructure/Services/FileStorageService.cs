using Microsoft.AspNetCore.Http;
using ProcurementSystem.Core.Interfaces;

namespace ProcurementSystem.Infrastructure.Services
{
    public class FileStorageService : IFileStorageService
    {
        private readonly string _baseUploadPath;
        private readonly string[] _allowedExtensions = { ".pdf", ".jpg", ".jpeg", ".png" };
        private const long MaxFileSize = 10 * 1024 * 1024; // 10MB

        public FileStorageService()
        {
            _baseUploadPath = Path.Combine(Directory.GetCurrentDirectory(), "uploads");
        }

        public async Task<string> SaveFileAsync(IFormFile file, string subFolder)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File không hợp lệ.");

            if (file.Length > MaxFileSize)
                throw new InvalidOperationException("Kích thước file vượt quá 10MB.");

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!_allowedExtensions.Contains(ext))
                throw new InvalidOperationException("Định dạng file không được phép. Chỉ chấp nhận .pdf, .jpg, .jpeg, .png.");

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
    }
}
