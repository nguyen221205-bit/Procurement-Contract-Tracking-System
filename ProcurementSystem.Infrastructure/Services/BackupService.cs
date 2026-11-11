using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ProcurementSystem.Core.DTOs;
using ProcurementSystem.Core.DTOs.Backup;
using ProcurementSystem.Core.Interfaces;
using ProcurementSystem.Infrastructure.Data;
using ProcurementSystem.Infrastructure.Entities;

namespace ProcurementSystem.Infrastructure.Services
{
    public class BackupService : IBackupService
    {
        private readonly IConfiguration _configuration;
        private readonly AppDbContext _context;
        private readonly ILogger<BackupService> _logger;
        private readonly string _baseBackupDir;
        private readonly string _dbBackupDir;
        private readonly string _docsBackupDir;

        public BackupService(
            IConfiguration configuration,
            AppDbContext context,
            ILogger<BackupService> logger)
        {
            _configuration = configuration;
            _context = context;
            _logger = logger;

            _baseBackupDir = Path.Combine(Directory.GetCurrentDirectory(), "backups");
            _dbBackupDir = Path.Combine(_baseBackupDir, "db");
            _docsBackupDir = Path.Combine(_baseBackupDir, "docs");

            EnsureDirectoriesExist();
        }

        private void EnsureDirectoriesExist()
        {
            if (!Directory.Exists(_baseBackupDir)) Directory.CreateDirectory(_baseBackupDir);
            if (!Directory.Exists(_dbBackupDir)) Directory.CreateDirectory(_dbBackupDir);
            if (!Directory.Exists(_docsBackupDir)) Directory.CreateDirectory(_docsBackupDir);
        }

        public async Task<ApiResponse<BackupOverviewDto>> GetBackupOverviewAsync()
        {
            EnsureDirectoriesExist();

            var dbFiles = Directory.GetFiles(_dbBackupDir, "*.bak")
                .Select(f => CreateBackupItemDto(f, "Database"))
                .OrderByDescending(x => x.CreatedAt)
                .ToList();

            var docFiles = Directory.GetFiles(_docsBackupDir, "*.zip")
                .Select(f => CreateBackupItemDto(f, "DocumentsZip"))
                .OrderByDescending(x => x.CreatedAt)
                .ToList();

            var allFiles = dbFiles.Concat(docFiles).ToList();
            var totalSize = allFiles.Sum(f => f.FileSizeBytes);
            var latestBackup = allFiles.OrderByDescending(f => f.CreatedAt).FirstOrDefault()?.CreatedAt;

            var overview = new BackupOverviewDto
            {
                TotalBackups = allFiles.Count,
                TotalSizeBytes = totalSize,
                TotalSizeFormatted = FormatBytes(totalSize),
                LastBackupAt = latestBackup,
                DatabaseBackups = dbFiles,
                DocumentBackups = docFiles
            };

            return ApiResponse<BackupOverviewDto>.Ok(overview, "Lấy thông tin tổng quan các bản sao lưu thành công.");
        }

        public async Task<ApiResponse<BackupItemDto>> CreateDatabaseBackupAsync(int adminUserId, string? description = null)
        {
            EnsureDirectoriesExist();

            var connStr = _configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrWhiteSpace(connStr))
            {
                return ApiResponse<BackupItemDto>.Fail("Không tìm thấy chuỗi kết nối cơ sở dữ liệu DefaultConnection.", statusCode: 500);
            }

            var builder = new SqlConnectionStringBuilder(connStr);
            var dbName = builder.InitialCatalog;
            if (string.IsNullOrWhiteSpace(dbName))
            {
                return ApiResponse<BackupItemDto>.Fail("Không xác định được tên cơ sở dữ liệu cần sao lưu.", statusCode: 500);
            }

            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            var fileName = $"procurement_db_backup_{timestamp}.bak";
            var backupFilePath = Path.GetFullPath(Path.Combine(_dbBackupDir, fileName));
            var backupName = $"procurement_db Full Backup {timestamp}";

            try
            {
                using var conn = new SqlConnection(connStr);
                await conn.OpenAsync();

                var backupSql = $@"
                    BACKUP DATABASE [{dbName}]
                    TO DISK = @backupFile
                    WITH FORMAT, INIT, CHECKSUM, COMPRESSION, NAME = @backupName;
                ";

                using (var cmd = new SqlCommand(backupSql, conn))
                {
                    cmd.CommandTimeout = 300; // 5 phút
                    cmd.Parameters.AddWithValue("@backupFile", backupFilePath);
                    cmd.Parameters.AddWithValue("@backupName", backupName);
                    await cmd.ExecuteNonQueryAsync();
                }

                // Kiểm định toàn vẹn bản sao lưu bằng RESTORE VERIFYONLY
                var verifySql = "RESTORE VERIFYONLY FROM DISK = @backupFile WITH CHECKSUM;";
                using (var verifyCmd = new SqlCommand(verifySql, conn))
                {
                    verifyCmd.CommandTimeout = 120;
                    verifyCmd.Parameters.AddWithValue("@backupFile", backupFilePath);
                    await verifyCmd.ExecuteNonQueryAsync();
                }

                var itemDto = CreateBackupItemDto(backupFilePath, "Database", description);

                // Ghi nhận AuditLog
                await RecordAuditLogAsync(adminUserId, "DATABASE_BACKUP_CREATED", "Database", null, null, new
                {
                    FileName = fileName,
                    FileSizeBytes = itemDto.FileSizeBytes,
                    Sha256Hash = itemDto.Sha256Hash,
                    Description = description
                });

                _logger.LogInformation("Đã tạo bản sao lưu cơ sở dữ liệu thành công: {FileName} ({Size})", fileName, itemDto.FileSizeFormatted);
                return ApiResponse<BackupItemDto>.Ok(itemDto, "Tạo bản sao lưu cơ sở dữ liệu toàn vẹn thành công.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi sao lưu cơ sở dữ liệu: {Message}", ex.Message);
                if (File.Exists(backupFilePath))
                {
                    try { File.Delete(backupFilePath); } catch { }
                }
                return ApiResponse<BackupItemDto>.Fail($"Sao lưu cơ sở dữ liệu thất bại: {ex.Message}", statusCode: 500);
            }
        }

        public async Task<ApiResponse<BackupItemDto>> CreateDocumentsZipBackupAsync(int adminUserId, string? description = null)
        {
            EnsureDirectoriesExist();

            var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "uploads");
            if (!Directory.Exists(uploadsDir))
            {
                Directory.CreateDirectory(uploadsDir);
            }

            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            var fileName = $"procurement_uploads_backup_{timestamp}.zip";
            var zipFilePath = Path.GetFullPath(Path.Combine(_docsBackupDir, fileName));

            try
            {
                // Nén toàn bộ thư mục uploads thành tệp ZIP
                ZipFile.CreateFromDirectory(uploadsDir, zipFilePath, CompressionLevel.Optimal, false);

                var itemDto = CreateBackupItemDto(zipFilePath, "DocumentsZip", description);

                // Ghi nhận AuditLog
                await RecordAuditLogAsync(adminUserId, "DOCUMENTS_BACKUP_CREATED", "Documents", null, null, new
                {
                    FileName = fileName,
                    FileSizeBytes = itemDto.FileSizeBytes,
                    Sha256Hash = itemDto.Sha256Hash,
                    Description = description
                });

                _logger.LogInformation("Đã tạo bản sao lưu tệp đính kèm thành công: {FileName} ({Size})", fileName, itemDto.FileSizeFormatted);
                return ApiResponse<BackupItemDto>.Ok(itemDto, "Tạo bản sao lưu nén tệp đính kèm thành công.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi nén bản sao lưu tệp đính kèm: {Message}", ex.Message);
                if (File.Exists(zipFilePath))
                {
                    try { File.Delete(zipFilePath); } catch { }
                }
                return ApiResponse<BackupItemDto>.Fail($"Nén bản sao lưu tệp thất bại: {ex.Message}", statusCode: 500);
            }
        }

        public Task<(byte[] FileBytes, string ContentType, string FileName)?> GetBackupFileForDownloadAsync(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName) || fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || fileName.Contains(".."))
            {
                return Task.FromResult<(byte[] FileBytes, string ContentType, string FileName)?>(null);
            }

            string filePath = Path.Combine(_dbBackupDir, fileName);
            string contentType = "application/octet-stream";

            if (File.Exists(filePath))
            {
                contentType = "application/x-sql-backup";
            }
            else
            {
                filePath = Path.Combine(_docsBackupDir, fileName);
                if (File.Exists(filePath))
                {
                    contentType = "application/zip";
                }
                else
                {
                    return Task.FromResult<(byte[] FileBytes, string ContentType, string FileName)?>(null);
                }
            }

            var bytes = File.ReadAllBytes(filePath);
            return Task.FromResult<(byte[] FileBytes, string ContentType, string FileName)?>((bytes, contentType, fileName));
        }

        public async Task<ApiResponse<string>> RestoreDatabaseBackupAsync(int adminUserId, RestoreBackupRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.FileName))
            {
                return ApiResponse<string>.Fail("Vui lòng cung cấp tên tệp sao lưu cần phục hồi.", statusCode: 400);
            }

            if (!string.Equals(request.ConfirmPhrase?.Trim(), "RESTORE_DATABASE_CONFIRM", StringComparison.Ordinal))
            {
                return ApiResponse<string>.Fail("Cụm từ xác nhận không chính xác. Thao tác phục hồi cơ sở dữ liệu đã bị hủy bỏ để đảm bảo an toàn.", statusCode: 400);
            }

            var fileName = Path.GetFileName(request.FileName);
            if (!fileName.EndsWith(".bak", StringComparison.OrdinalIgnoreCase))
            {
                return ApiResponse<string>.Fail("Định dạng tệp sao lưu không hợp lệ. Chỉ chấp nhận tệp có đuôi mở rộng .bak.", statusCode: 400);
            }

            var backupFilePath = Path.GetFullPath(Path.Combine(_dbBackupDir, fileName));
            if (!File.Exists(backupFilePath))
            {
                return ApiResponse<string>.Fail("Không tìm thấy tệp sao lưu yêu cầu trên máy chủ.", statusCode: 404);
            }

            var connStr = _configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrWhiteSpace(connStr))
            {
                return ApiResponse<string>.Fail("Không tìm thấy chuỗi kết nối cơ sở dữ liệu DefaultConnection.", statusCode: 500);
            }

            var builder = new SqlConnectionStringBuilder(connStr);
            var targetDbName = builder.InitialCatalog;
            if (string.IsNullOrWhiteSpace(targetDbName))
            {
                return ApiResponse<string>.Fail("Không xác định được tên cơ sở dữ liệu đích cần phục hồi.", statusCode: 500);
            }

            // Chuyển kết nối tới cơ sở dữ liệu master để không khóa cơ sở dữ liệu đích
            builder.InitialCatalog = "master";
            var masterConnStr = builder.ConnectionString;

            try
            {
                using var conn = new SqlConnection(masterConnStr);
                await conn.OpenAsync();

                // 1. Kiểm tra tính toàn vẹn của bản sao lưu trước khi phục hồi
                var verifySql = "RESTORE VERIFYONLY FROM DISK = @backupFile WITH CHECKSUM;";
                using (var verifyCmd = new SqlCommand(verifySql, conn))
                {
                    verifyCmd.CommandTimeout = 120;
                    verifyCmd.Parameters.AddWithValue("@backupFile", backupFilePath);
                    await verifyCmd.ExecuteNonQueryAsync();
                }

                // 2. Ngắt các phiên kết nối hiện hành, phục hồi cơ sở dữ liệu đè lên bản cũ, mở lại đa kết nối
                var restoreSql = $@"
                    ALTER DATABASE [{targetDbName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                    RESTORE DATABASE [{targetDbName}] FROM DISK = @backupFile WITH REPLACE, RECOVERY;
                    ALTER DATABASE [{targetDbName}] SET MULTI_USER;
                ";

                using (var restoreCmd = new SqlCommand(restoreSql, conn))
                {
                    restoreCmd.CommandTimeout = 300; // 5 phút
                    restoreCmd.Parameters.AddWithValue("@backupFile", backupFilePath);
                    await restoreCmd.ExecuteNonQueryAsync();
                }

                // Ghi nhận AuditLog phục hồi
                await RecordAuditLogAsync(adminUserId, "DATABASE_RESTORED", "Database", null, null, new
                {
                    RestoredFileName = fileName,
                    DatabaseName = targetDbName,
                    RestoredAt = DateTime.UtcNow
                });

                _logger.LogWarning("Admin {AdminUserId} đã phục hồi thành công cơ sở dữ liệu từ tệp: {FileName}", adminUserId, fileName);
                return ApiResponse<string>.Ok(fileName, $"Cơ sở dữ liệu {targetDbName} đã được phục hồi thành công từ bản sao lưu {fileName}.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi trong quá trình phục hồi cơ sở dữ liệu: {Message}", ex.Message);

                // Cố gắng mở lại MULTI_USER nếu bị lỗi giữa chừng
                try
                {
                    using var recoveryConn = new SqlConnection(masterConnStr);
                    await recoveryConn.OpenAsync();
                    using var fixCmd = new SqlCommand($"ALTER DATABASE [{targetDbName}] SET MULTI_USER;", recoveryConn);
                    await fixCmd.ExecuteNonQueryAsync();
                }
                catch { /* Bỏ qua nếu không thể mở lại */ }

                return ApiResponse<string>.Fail($"Phục hồi cơ sở dữ liệu thất bại: {ex.Message}", statusCode: 500);
            }
        }

        public async Task<ApiResponse<string>> DeleteBackupAsync(int adminUserId, string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName) || fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || fileName.Contains(".."))
            {
                return ApiResponse<string>.Fail("Tên tệp không hợp lệ.", statusCode: 400);
            }

            string filePath = Path.Combine(_dbBackupDir, fileName);
            if (!File.Exists(filePath))
            {
                filePath = Path.Combine(_docsBackupDir, fileName);
            }

            if (!File.Exists(filePath))
            {
                return ApiResponse<string>.Fail("Không tìm thấy tệp sao lưu cần xóa.", statusCode: 404);
            }

            try
            {
                File.Delete(filePath);

                // Ghi nhận AuditLog
                await RecordAuditLogAsync(adminUserId, "BACKUP_DELETED", "Backup", null, new { DeletedFileName = fileName }, null);

                return ApiResponse<string>.Ok(fileName, $"Đã xóa tệp sao lưu {fileName} thành công.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xóa tệp sao lưu {FileName}: {Message}", fileName, ex.Message);
                return ApiResponse<string>.Fail($"Xóa tệp sao lưu thất bại: {ex.Message}", statusCode: 500);
            }
        }

        private BackupItemDto CreateBackupItemDto(string filePath, string backupType, string? description = null)
        {
            var fileInfo = new FileInfo(filePath);
            string hash = string.Empty;

            try
            {
                using var fs = File.OpenRead(filePath);
                hash = Convert.ToHexString(SHA256.HashData(fs)).ToLowerInvariant();
            }
            catch { }

            return new BackupItemDto
            {
                FileName = fileInfo.Name,
                BackupType = backupType,
                FileSizeBytes = fileInfo.Length,
                FileSizeFormatted = FormatBytes(fileInfo.Length),
                CreatedAt = fileInfo.CreationTimeUtc,
                Sha256Hash = hash,
                Description = description
            };
        }

        private static string FormatBytes(long bytes)
        {
            if (bytes < 1024) return $"{bytes} B";
            double kb = bytes / 1024.0;
            if (kb < 1024) return $"{kb:F1} KB";
            double mb = kb / 1024.0;
            if (mb < 1024) return $"{mb:F1} MB";
            double gb = mb / 1024.0;
            return $"{gb:F2} GB";
        }

        private async Task RecordAuditLogAsync(int? userId, string action, string entityType, int? entityId, object? oldVals, object? newVals)
        {
            try
            {
                var auditLog = new AuditLog
                {
                    UserId = (userId.HasValue && userId.Value > 0) ? userId : null,
                    Action = action,
                    EntityType = entityType,
                    EntityId = entityId,
                    OldValues = oldVals != null ? JsonSerializer.Serialize(oldVals) : null,
                    NewValues = newVals != null ? JsonSerializer.Serialize(newVals) : null,
                    Timestamp = DateTime.UtcNow
                };

                await _context.AuditLogs.AddAsync(auditLog);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Không thể ghi AuditLog cho thao tác {Action}.", action);
            }
        }
    }
}
