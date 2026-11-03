using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProcurementSystem.Core.DTOs.Common;
using ProcurementSystem.Core.Interfaces;

namespace ProcurementSystem.Infrastructure.Services
{
    /// <summary>
    /// Bộ định tuyến lưu trữ thông minh: Tự động chuyển đổi giữa Local Storage và Supabase Cloud Storage
    /// Đảm bảo tính tương thích ngược cho dữ liệu cũ và an toàn tuyệt đối cho dữ liệu mới.
    /// </summary>
    public class HybridFileStorageService : IFileStorageService
    {
        private readonly FileStorageService _localService;
        private readonly SupabaseStorageService _supabaseService;
        private readonly StorageSettings _settings;
        private readonly ILogger<HybridFileStorageService> _logger;

        public HybridFileStorageService(
            FileStorageService localService,
            SupabaseStorageService supabaseService,
            IOptions<StorageSettings> settings,
            ILogger<HybridFileStorageService> logger)
        {
            _localService = localService;
            _supabaseService = supabaseService;
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task<string> SaveFileAsync(IFormFile file, string subFolder)
        {
            if (IsCloudProviderActive())
            {
                try
                {
                    return await _supabaseService.SaveFileAsync(file, subFolder);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Tải tệp lên Supabase Storage thất bại. Tự động chuyển hướng lưu cục bộ (Local Storage).");
                }
            }

            return await _localService.SaveFileAsync(file, subFolder);
        }

        public void DeleteFile(string storedPath)
        {
            if (string.IsNullOrWhiteSpace(storedPath)) return;

            if (IsCloudPath(storedPath))
            {
                _supabaseService.DeleteFile(storedPath);
            }
            else
            {
                _localService.DeleteFile(storedPath);
            }
        }

        public string GetPhysicalFilePath(string relativePath)
        {
            if (IsCloudPath(relativePath))
            {
                throw new InvalidOperationException("Tệp này hiện được lưu trữ trên Supabase Cloud Storage. Không thể truy cập qua đường dẫn vật lý cục bộ. Vui lòng sử dụng phương thức ResolveDownloadAsync để nhận Pre-signed URL.");
            }

            return _localService.GetPhysicalFilePath(relativePath);
        }

        public async Task<FileDownloadResult> ResolveDownloadAsync(string storedPath, string originalFileName, string? contentType = null)
        {
            if (string.IsNullOrWhiteSpace(storedPath))
                throw new ArgumentException("Đường dẫn tệp không hợp lệ.", nameof(storedPath));

            if (IsCloudPath(storedPath))
            {
                return await _supabaseService.ResolveDownloadAsync(storedPath, originalFileName, contentType);
            }

            return await _localService.ResolveDownloadAsync(storedPath, originalFileName, contentType);
        }

        public async Task<string> GetPreSignedUrlAsync(string storedPath, int expirationMinutes = 15)
        {
            if (!IsCloudPath(storedPath))
            {
                throw new NotSupportedException("Tệp lưu trữ cục bộ không hỗ trợ Pre-signed URL.");
            }

            return await _supabaseService.GetPreSignedUrlAsync(storedPath, expirationMinutes);
        }

        public bool IsCloudPath(string storedPath)
        {
            if (string.IsNullOrWhiteSpace(storedPath)) return false;
            return storedPath.StartsWith("supabase://", StringComparison.OrdinalIgnoreCase)
                || storedPath.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                || storedPath.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
        }

        private bool IsCloudProviderActive()
        {
            return _settings.Provider.Equals("Supabase", StringComparison.OrdinalIgnoreCase)
                && _supabaseService.IsConfigured;
        }
    }
}
