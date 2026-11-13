using Microsoft.AspNetCore.Http;
using ProcurementSystem.Core.DTOs.Common;

namespace ProcurementSystem.Core.Interfaces
{
    /// <summary>
    /// Giao diện chiến lược lưu trữ tệp tin trừu tượng (Strategy Pattern - tuân thủ OCP, LSP, ISP, DIP).
    /// Cho phép mở rộng các nhà cung cấp lưu trữ (Local, Supabase S3, AWS S3, Azure Blob) mà không phụ thuộc lớp cụ thể.
    /// </summary>
    public interface IFileStorageProvider
    {
        /// <summary>Tên định danh nhà cung cấp (ví dụ: "Local", "Supabase").</summary>
        string ProviderName { get; }

        /// <summary>Nhà cung cấp hiện đã cấu hình đầy đủ và sẵn sàng xử lý hay chưa.</summary>
        bool IsAvailable { get; }

        /// <summary>Kiểm tra nhà cung cấp này có chịu trách nhiệm xử lý đường dẫn lưu trữ đã cho hay không.</summary>
        bool CanHandlePath(string storedPath);

        Task<string> SaveFileAsync(IFormFile file, string subFolder);

        void DeleteFile(string storedPath);

        Task<FileDownloadResult> ResolveDownloadAsync(string storedPath, string originalFileName, string? contentType = null);
    }

    /// <summary>
    /// Giao diện chuyên biệt dành cho lưu trữ đĩa cục bộ (ISP - tách biệt phương thức đường dẫn vật lý).
    /// </summary>
    public interface ILocalFileStorageProvider : IFileStorageProvider
    {
        string GetPhysicalFilePath(string relativePath);
    }

    /// <summary>
    /// Giao diện chuyên biệt dành cho lưu trữ đám mây (ISP - tách biệt phương thức S3/Pre-signed URL/Stream).
    /// </summary>
    public interface ICloudFileStorageProvider : IFileStorageProvider
    {
        bool IsConfigured { get; }

        Task<(bool Success, string Message, List<string> Buckets)> TestAndEnsureBucketAsync();

        Task<string> SaveStreamAsync(Stream stream, string fileName, string contentType, string subFolder);

        Task<string> GetPreSignedUrlAsync(string storedPath, int expirationMinutes = 15);
    }

    /// <summary>
    /// Giao diện chuyên trách khôi phục / sinh tài liệu PDF hành chính mẫu khi tệp môi trường Demo bị thiếu (SRP).
    /// Tách biệt hoàn toàn trách nhiệm tạo dữ liệu PDF mẫu ra khỏi dịch vụ I/O lưu trữ tệp.
    /// </summary>
    public interface IDocumentSampleFallbackProvider
    {
        bool NeedsFallbackGeneration(string physicalPath);

        byte[] GenerateSampleDocumentBytes(string pathOrKey, string originalFileName);

        void EnsureLocalSamplePdfIfNeeded(string physicalPath, string originalFileName);
    }
}
