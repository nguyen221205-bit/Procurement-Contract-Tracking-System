using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProcurementSystem.Core.DTOs.Common;
using ProcurementSystem.Core.Interfaces;

namespace ProcurementSystem.Infrastructure.Services
{
    /// <summary>
    /// Bộ định tuyến lưu trữ thông minh (Strategy & Composite Router):
    /// Tự động điều phối giữa các IFileStorageProvider (Local, Supabase hoặc nhà cung cấp mở rộng khác)
    /// thông qua giao diện trừu tượng (tuân thủ SRP, OCP, LSP, ISP, DIP).
    /// </summary>
    public class HybridFileStorageService : IFileStorageService
    {
        private readonly ILocalFileStorageProvider _localProvider;
        private readonly ICloudFileStorageProvider _cloudProvider;
        private readonly IReadOnlyList<IFileStorageProvider> _providers;
        private readonly StorageSettings _settings;
        private readonly ILogger<HybridFileStorageService> _logger;

        public HybridFileStorageService(
            ILocalFileStorageProvider localProvider,
            ICloudFileStorageProvider cloudProvider,
            IOptions<StorageSettings> settings,
            ILogger<HybridFileStorageService> logger,
            IEnumerable<IFileStorageProvider>? registeredProviders = null)
        {
            _localProvider = localProvider;
            _cloudProvider = cloudProvider;
            _settings = settings.Value;
            _logger = logger;

            var list = new List<IFileStorageProvider>();
            if (registeredProviders != null)
            {
                list.AddRange(registeredProviders);
            }

            if (!list.Contains(cloudProvider))
            {
                list.Add(cloudProvider);
            }

            if (!list.Contains(localProvider))
            {
                list.Add(localProvider);
            }

            _providers = list;
        }

        public async Task<string> SaveFileAsync(IFormFile file, string subFolder)
        {
            var preferredProvider = _providers.FirstOrDefault(p =>
                p.ProviderName.Equals(_settings.Provider, StringComparison.OrdinalIgnoreCase) && p.IsAvailable);

            if (preferredProvider != null && !ReferenceEquals(preferredProvider, _localProvider))
            {
                try
                {
                    return await preferredProvider.SaveFileAsync(file, subFolder);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        ex,
                        "Tải tệp lên nhà cung cấp {Provider} thất bại. Tự động chuyển hướng lưu cục bộ (Local Storage).",
                        preferredProvider.ProviderName);
                }
            }

            return await _localProvider.SaveFileAsync(file, subFolder);
        }

        public void DeleteFile(string storedPath)
        {
            if (string.IsNullOrWhiteSpace(storedPath)) return;

            var handler = ResolveProviderForPath(storedPath);
            handler.DeleteFile(storedPath);
        }

        public string GetPhysicalFilePath(string relativePath)
        {
            if (IsCloudPath(relativePath))
            {
                throw new InvalidOperationException(
                    "Tệp này hiện được lưu trữ trên Supabase Cloud Storage. Không thể truy cập qua đường dẫn vật lý cục bộ. Vui lòng sử dụng phương thức ResolveDownloadAsync để nhận Pre-signed URL.");
            }

            return _localProvider.GetPhysicalFilePath(relativePath);
        }

        public async Task<FileDownloadResult> ResolveDownloadAsync(string storedPath, string originalFileName, string? contentType = null)
        {
            if (string.IsNullOrWhiteSpace(storedPath))
                throw new ArgumentException("Đường dẫn tệp không hợp lệ.", nameof(storedPath));

            var handler = ResolveProviderForPath(storedPath);
            return await handler.ResolveDownloadAsync(storedPath, originalFileName, contentType);
        }

        public async Task<string> GetPreSignedUrlAsync(string storedPath, int expirationMinutes = 15)
        {
            if (IsCloudPath(storedPath))
            {
                return await _cloudProvider.GetPreSignedUrlAsync(storedPath, expirationMinutes);
            }

            var physicalPath = _localProvider.GetPhysicalFilePath(storedPath);
            return new Uri(physicalPath).AbsoluteUri;
        }

        public bool IsCloudPath(string storedPath)
        {
            if (string.IsNullOrWhiteSpace(storedPath)) return false;
            return _cloudProvider.CanHandlePath(storedPath);
        }

        private IFileStorageProvider ResolveProviderForPath(string storedPath)
        {
            var matched = _providers.FirstOrDefault(p => !ReferenceEquals(p, _localProvider) && p.CanHandlePath(storedPath));
            return matched ?? _localProvider;
        }
    }
}
