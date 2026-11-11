using ProcurementSystem.Core.DTOs;
using ProcurementSystem.Core.DTOs.Backup;

namespace ProcurementSystem.Core.Interfaces
{
    public interface IBackupService
    {
        Task<ApiResponse<BackupOverviewDto>> GetBackupOverviewAsync();
        Task<ApiResponse<BackupItemDto>> CreateDatabaseBackupAsync(int adminUserId, string? description = null);
        Task<ApiResponse<BackupItemDto>> CreateDocumentsZipBackupAsync(int adminUserId, string? description = null);
        Task<(byte[] FileBytes, string ContentType, string FileName)?> GetBackupFileForDownloadAsync(string fileName);
        Task<ApiResponse<string>> RestoreDatabaseBackupAsync(int adminUserId, RestoreBackupRequest request);
        Task<ApiResponse<string>> DeleteBackupAsync(int adminUserId, string fileName);
    }
}
