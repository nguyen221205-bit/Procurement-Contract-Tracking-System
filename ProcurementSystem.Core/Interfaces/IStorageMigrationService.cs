using ProcurementSystem.Core.DTOs;
using ProcurementSystem.Core.DTOs.Common;

namespace ProcurementSystem.Core.Interfaces
{
    public interface IStorageMigrationService
    {
        Task<ApiResponse<StorageSyncReportDto>> SyncLocalFilesToCloudAsync(int adminUserId);
        Task<ApiResponse<string>> RollbackFilePathsAsync(int adminUserId, string? backupTableName = null);
        Task<ApiResponse<StorageStatusDto>> GetStorageStatusAsync();
    }
}
