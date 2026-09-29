using Microsoft.AspNetCore.Http;

namespace ProcurementSystem.Core.Interfaces
{
    public interface IFileStorageService
    {
        Task<string> SaveFileAsync(IFormFile file, string subFolder);
        void DeleteFile(string relativePath);
    }
}
