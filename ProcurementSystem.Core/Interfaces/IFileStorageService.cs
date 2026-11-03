using Microsoft.AspNetCore.Http;
using ProcurementSystem.Core.DTOs.Common;

namespace ProcurementSystem.Core.Interfaces
{
    public interface IFileStorageService
    {
        Task<string> SaveFileAsync(IFormFile file, string subFolder);
        void DeleteFile(string storedPath);
        string GetPhysicalFilePath(string relativePath);

        // Hỗ trợ Cloud Storage & Pre-signed URL
        Task<FileDownloadResult> ResolveDownloadAsync(string storedPath, string originalFileName, string? contentType = null);
        Task<string> GetPreSignedUrlAsync(string storedPath, int expirationMinutes = 15);
        bool IsCloudPath(string storedPath);
    }
}

