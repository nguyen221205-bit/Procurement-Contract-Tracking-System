using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProcurementSystem.Core.DTOs;
using ProcurementSystem.Core.DTOs.Common;
using ProcurementSystem.Core.Interfaces;
using ProcurementSystem.Infrastructure.Data;
using ProcurementSystem.Infrastructure.Entities;
using ProcurementSystem.Infrastructure.Utilities;

namespace ProcurementSystem.Infrastructure.Services
{
    public class StorageMigrationService : IStorageMigrationService
    {
        private readonly AppDbContext _context;
        private readonly ICloudFileStorageProvider _supabaseService;
        private readonly StorageSettings _settings;
        private readonly ILogger<StorageMigrationService> _logger;
        private readonly string _baseUploadPath;

        public StorageMigrationService(
            AppDbContext context,
            ICloudFileStorageProvider supabaseService,
            IOptions<StorageSettings> settings,
            ILogger<StorageMigrationService> logger)
        {
            _context = context;
            _supabaseService = supabaseService;
            _settings = settings.Value;
            _logger = logger;
            _baseUploadPath = Path.Combine(Directory.GetCurrentDirectory(), "uploads");
        }

        public Task<ApiResponse<StorageStatusDto>> GetStorageStatusAsync()
        {
            var status = new StorageStatusDto
            {
                ActiveProvider = _settings.Provider,
                IsSupabaseConfigured = _supabaseService.IsConfigured,
                SupabaseEndpoint = _settings.Supabase.Endpoint,
                SupabaseBucket = _settings.Supabase.BucketName
            };

            if (Directory.Exists(_baseUploadPath))
            {
                var files = Directory.GetFiles(_baseUploadPath, "*.*", SearchOption.AllDirectories);
                status.LocalFilesCount = files.Length;
                status.LocalUploadsSizeBytes = files.Sum(f => new FileInfo(f).Length);
            }

            return Task.FromResult(ApiResponse<StorageStatusDto>.Ok(status));
        }

        public async Task<ApiResponse<StorageSyncReportDto>> SyncLocalFilesToCloudAsync(int adminUserId)
        {
            var report = new StorageSyncReportDto();

            if (!_supabaseService.IsConfigured)
            {
                return ApiResponse<StorageSyncReportDto>.Fail(
                    "Supabase Storage chưa được cấu hình. Vui lòng cập nhật StorageSettings:Supabase trong appsettings.json trước khi đồng bộ.");
            }

            if (!Directory.Exists(_baseUploadPath))
            {
                return ApiResponse<StorageSyncReportDto>.Fail("Thư mục uploads cục bộ không tồn tại.");
            }

            var allFiles = Directory.GetFiles(_baseUploadPath, "*.*", SearchOption.AllDirectories)
                .Where(f => !f.EndsWith(".gitkeep", StringComparison.OrdinalIgnoreCase))
                .ToList();

            report.TotalFilesDiscovered = allFiles.Count;
            if (report.TotalFilesDiscovered == 0)
            {
                report.Success = true;
                report.Message = "Không có tệp cục bộ nào cần đồng bộ lên Cloud.";
                return ApiResponse<StorageSyncReportDto>.Ok(report);
            }

            // 1. Tự động tạo bảng sao lưu CSDL an toàn
            var backupTableName = $"_Backup_FilePaths_{DateTime.UtcNow:yyyyMMdd_HHmmss}";
            report.BackupTableName = backupTableName;

            try
            {
                var createBackupTableSql = $@"
                    CREATE TABLE [{backupTableName}] (
                        Id INT IDENTITY(1,1) PRIMARY KEY,
                        TableName NVARCHAR(100) NOT NULL,
                        RecordId INT NOT NULL,
                        ColumnName NVARCHAR(100) NOT NULL,
                        OriginalPath NVARCHAR(500) NOT NULL,
                        CreatedAt DATETIME2 DEFAULT SYSUTCDATETIME()
                    );";
                await _context.Database.ExecuteSqlRawAsync(createBackupTableSql);

                var backupInsertsSql = $@"
                    INSERT INTO [{backupTableName}] (TableName, RecordId, ColumnName, OriginalPath)
                    SELECT 'BidDocuments', Id, 'FilePath', FilePath FROM BidDocuments WHERE FilePath IS NOT NULL AND FilePath LIKE '/uploads/%';

                    INSERT INTO [{backupTableName}] (TableName, RecordId, ColumnName, OriginalPath)
                    SELECT 'SubmissionFiles', Id, 'FilePath', FilePath FROM SubmissionFiles WHERE FilePath IS NOT NULL AND FilePath LIKE '/uploads/%';

                    INSERT INTO [{backupTableName}] (TableName, RecordId, ColumnName, OriginalPath)
                    SELECT 'Contracts', Id, 'ScannedFilePath', ScannedFilePath FROM Contracts WHERE ScannedFilePath IS NOT NULL AND ScannedFilePath LIKE '/uploads/%';

                    INSERT INTO [{backupTableName}] (TableName, RecordId, ColumnName, OriginalPath)
                    SELECT 'ContractGuarantees', Id, 'ScannedDocumentPath', ScannedDocumentPath FROM ContractGuarantees WHERE ScannedDocumentPath IS NOT NULL AND ScannedDocumentPath LIKE '/uploads/%';

                    INSERT INTO [{backupTableName}] (TableName, RecordId, ColumnName, OriginalPath)
                    SELECT 'ContractAddenda', Id, 'ScannedDocumentPath', ScannedDocumentPath FROM ContractAddenda WHERE ScannedDocumentPath IS NOT NULL AND ScannedDocumentPath LIKE '/uploads/%';

                    INSERT INTO [{backupTableName}] (TableName, RecordId, ColumnName, OriginalPath)
                    SELECT 'Contractors', Id, 'BusinessLicenseFile', BusinessLicenseFile FROM Contractors WHERE BusinessLicenseFile IS NOT NULL AND BusinessLicenseFile LIKE '/uploads/%';

                    INSERT INTO [{backupTableName}] (TableName, RecordId, ColumnName, OriginalPath)
                    SELECT 'ProcuringEntities', Id, 'EstablishmentDecisionFile', EstablishmentDecisionFile FROM ProcuringEntities WHERE EstablishmentDecisionFile IS NOT NULL AND EstablishmentDecisionFile LIKE '/uploads/%';

                    INSERT INTO [{backupTableName}] (TableName, RecordId, ColumnName, OriginalPath)
                    SELECT 'ProcuringEntities', Id, 'AppointmentDecisionFile', AppointmentDecisionFile FROM ProcuringEntities WHERE AppointmentDecisionFile IS NOT NULL AND AppointmentDecisionFile LIKE '/uploads/%';

                    INSERT INTO [{backupTableName}] (TableName, RecordId, ColumnName, OriginalPath)
                    SELECT 'EvaluatorProposals', Id, 'CertificateFile', CertificateFile FROM EvaluatorProposals WHERE CertificateFile IS NOT NULL AND CertificateFile LIKE '/uploads/%';

                    INSERT INTO [{backupTableName}] (TableName, RecordId, ColumnName, OriginalPath)
                    SELECT 'BidClarifications', Id, 'QuestionAttachmentUrl', QuestionAttachmentUrl FROM BidClarifications WHERE QuestionAttachmentUrl IS NOT NULL AND QuestionAttachmentUrl LIKE '/uploads/%';

                    INSERT INTO [{backupTableName}] (TableName, RecordId, ColumnName, OriginalPath)
                    SELECT 'BidClarifications', Id, 'AnswerAttachmentUrl', AnswerAttachmentUrl FROM BidClarifications WHERE AnswerAttachmentUrl IS NOT NULL AND AnswerAttachmentUrl LIKE '/uploads/%';
                ";
                await _context.Database.ExecuteSqlRawAsync(backupInsertsSql);
                report.Details.Add($"Đã tạo bảng sao lưu CSDL an toàn: {backupTableName}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo bảng sao lưu CSDL trước khi đồng bộ tệp.");
                return ApiResponse<StorageSyncReportDto>.Fail($"Không thể tạo bảng sao lưu CSDL: {ex.Message}");
            }

            // 2. Kiểm tra kết nối Supabase S3 và đảm bảo Bucket tồn tại
            var (bucketOk, bucketMsg, bucketList) = await _supabaseService.TestAndEnsureBucketAsync();
            if (!bucketOk)
            {
                _logger.LogError("Không thể đồng bộ vì Bucket Supabase không hợp lệ: {Msg}", bucketMsg);
                return ApiResponse<StorageSyncReportDto>.Fail(bucketMsg);
            }
            report.Details.Add(bucketMsg);

            // 3. Duyệt qua từng tệp và tải lên Supabase Storage
            foreach (var filePath in allFiles)
            {
                try
                {
                    var relativePath = Path.GetRelativePath(Directory.GetCurrentDirectory(), filePath).Replace('\\', '/');
                    if (!relativePath.StartsWith('/')) relativePath = "/" + relativePath;

                    var localRelative = Path.GetRelativePath(_baseUploadPath, filePath).Replace('\\', '/');
                    var parts = localRelative.Split('/');
                    var subFolder = parts.Length > 1 ? string.Join('/', parts.Take(parts.Length - 1)) : string.Empty;
                    var fileName = parts.Last();
                    var contentType = ContentTypeResolver.ResolveByFileName(fileName);

                    using var stream = File.OpenRead(filePath);
                    var cloudPath = await _supabaseService.SaveStreamAsync(stream, fileName, contentType, subFolder);

                    // 3. Cập nhật đường dẫn trong CSDL
                    var updateTables = new[]
                    {
                        ("BidDocuments", "FilePath"),
                        ("SubmissionFiles", "FilePath"),
                        ("Contracts", "ScannedFilePath"),
                        ("ContractGuarantees", "ScannedDocumentPath"),
                        ("ContractAddenda", "ScannedDocumentPath"),
                        ("Contractors", "BusinessLicenseFile"),
                        ("ProcuringEntities", "EstablishmentDecisionFile"),
                        ("ProcuringEntities", "AppointmentDecisionFile"),
                        ("EvaluatorProposals", "CertificateFile"),
                        ("BidClarifications", "QuestionAttachmentUrl"),
                        ("BidClarifications", "AnswerAttachmentUrl")
                    };

                    foreach (var (tableName, colName) in updateTables)
                    {
                        var updateSql = string.Format("UPDATE [{0}] SET [{1}] = {{0}} WHERE [{1}] = {{1}}", tableName, colName);
                        var rows = await _context.Database.ExecuteSqlRawAsync(updateSql, cloudPath, relativePath);
                        report.DatabaseRecordsUpdated += rows;
                    }

                    report.UploadedFilesCount++;
                    report.Details.Add($"[OK] {relativePath} -> {cloudPath}");
                }
                catch (Exception ex)
                {
                    report.FailedFilesCount++;
                    report.Details.Add($"[FAILED] {filePath}: {ex.Message}");
                    _logger.LogError(ex, "Lỗi khi đồng bộ tệp {FilePath} lên Supabase.", filePath);
                }
            }

            // Ghi Audit Log
            try
            {
                var auditLog = new AuditLog
                {
                    UserId = adminUserId,
                    Action = "STORAGE_SYNC_TO_CLOUD",
                    EntityType = "Storage",
                    EntityId = 0,
                    IpAddress = "127.0.0.1",
                    NewValues = $"Total: {report.TotalFilesDiscovered}, Uploaded: {report.UploadedFilesCount}, DB Records: {report.DatabaseRecordsUpdated}, BackupTable: {backupTableName}"
                };
                await _context.AuditLogs.AddAsync(auditLog);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Không thể ghi AuditLog cho thao tác STORAGE_SYNC_TO_CLOUD.");
            }

            report.Success = report.FailedFilesCount == 0;
            report.Message = report.Success
                ? $"Đồng bộ hoàn tất {report.UploadedFilesCount}/{report.TotalFilesDiscovered} tệp lên Supabase Storage. Đã cập nhật {report.DatabaseRecordsUpdated} bản ghi CSDL."
                : $"Đồng bộ hoàn tất với {report.FailedFilesCount} tệp gặp sự cố. Bảng sao lưu an toàn: {backupTableName}";

            return ApiResponse<StorageSyncReportDto>.Ok(report, report.Message);
        }

        public async Task<ApiResponse<string>> RollbackFilePathsAsync(int adminUserId, string? backupTableName = null)
        {
            try
            {
                // Nếu không truyền tên bảng, tìm bảng backup mới nhất
                if (string.IsNullOrWhiteSpace(backupTableName))
                {
                    var sqlFindLatest = @"
                        SELECT TOP 1 TABLE_NAME 
                        FROM INFORMATION_SCHEMA.TABLES 
                        WHERE TABLE_NAME LIKE '_Backup_FilePaths_%' 
                        ORDER BY TABLE_NAME DESC";

                    using var command = _context.Database.GetDbConnection().CreateCommand();
                    command.CommandText = sqlFindLatest;
                    await _context.Database.OpenConnectionAsync();

                    var result = await command.ExecuteScalarAsync();
                    if (result == null || result == DBNull.Value)
                    {
                        return ApiResponse<string>.Fail("Không tìm thấy bảng sao lưu CSDL nào để thực hiện rollback.");
                    }
                    backupTableName = result.ToString();
                }

                if (string.IsNullOrWhiteSpace(backupTableName))
                {
                    return ApiResponse<string>.Fail("Tên bảng sao lưu không hợp lệ.");
                }

                // Thực hiện câu lệnh SQL Rollback từ bảng backup
                var rollbackSql = $@"
                    UPDATE b SET b.FilePath = bk.OriginalPath
                    FROM BidDocuments b INNER JOIN [{backupTableName}] bk ON bk.TableName = 'BidDocuments' AND bk.RecordId = b.Id;

                    UPDATE b SET b.FilePath = bk.OriginalPath
                    FROM SubmissionFiles b INNER JOIN [{backupTableName}] bk ON bk.TableName = 'SubmissionFiles' AND bk.RecordId = b.Id;

                    UPDATE c SET c.ScannedFilePath = bk.OriginalPath
                    FROM Contracts c INNER JOIN [{backupTableName}] bk ON bk.TableName = 'Contracts' AND bk.RecordId = c.Id;

                    UPDATE g SET g.ScannedDocumentPath = bk.OriginalPath
                    FROM ContractGuarantees g INNER JOIN [{backupTableName}] bk ON bk.TableName = 'ContractGuarantees' AND bk.RecordId = g.Id;

                    UPDATE a SET a.ScannedDocumentPath = bk.OriginalPath
                    FROM ContractAddenda a INNER JOIN [{backupTableName}] bk ON bk.TableName = 'ContractAddenda' AND bk.RecordId = a.Id;

                    UPDATE ct SET ct.BusinessLicenseFile = bk.OriginalPath
                    FROM Contractors ct INNER JOIN [{backupTableName}] bk ON bk.TableName = 'Contractors' AND bk.RecordId = ct.Id;

                    UPDATE pe SET pe.EstablishmentDecisionFile = bk.OriginalPath
                    FROM ProcuringEntities pe INNER JOIN [{backupTableName}] bk ON bk.TableName = 'ProcuringEntities' AND bk.ColumnName = 'EstablishmentDecisionFile' AND bk.RecordId = pe.Id;

                    UPDATE pe SET pe.AppointmentDecisionFile = bk.OriginalPath
                    FROM ProcuringEntities pe INNER JOIN [{backupTableName}] bk ON bk.TableName = 'ProcuringEntities' AND bk.ColumnName = 'AppointmentDecisionFile' AND bk.RecordId = pe.Id;

                    UPDATE ep SET ep.CertificateFile = bk.OriginalPath
                    FROM EvaluatorProposals ep INNER JOIN [{backupTableName}] bk ON bk.TableName = 'EvaluatorProposals' AND bk.RecordId = ep.Id;

                    UPDATE cl SET cl.QuestionAttachmentUrl = bk.OriginalPath
                    FROM BidClarifications cl INNER JOIN [{backupTableName}] bk ON bk.TableName = 'BidClarifications' AND bk.ColumnName = 'QuestionAttachmentUrl' AND bk.RecordId = cl.Id;

                    UPDATE cl SET cl.AnswerAttachmentUrl = bk.OriginalPath
                    FROM BidClarifications cl INNER JOIN [{backupTableName}] bk ON bk.TableName = 'BidClarifications' AND bk.ColumnName = 'AnswerAttachmentUrl' AND bk.RecordId = cl.Id;
                ";

                var affectedRows = await _context.Database.ExecuteSqlRawAsync(rollbackSql);

                // Ghi AuditLog
                var auditLog = new AuditLog
                {
                    UserId = adminUserId,
                    Action = "STORAGE_ROLLBACK_TO_LOCAL",
                    EntityType = "Storage",
                    EntityId = 0,
                    IpAddress = "127.0.0.1",
                    NewValues = $"Restored from backup table: {backupTableName}, AffectedRows: {affectedRows}"
                };
                await _context.AuditLogs.AddAsync(auditLog);
                await _context.SaveChangesAsync();

                return ApiResponse<string>.Ok($"Đã hoàn tác (rollback) thành công đường dẫn tệp về trạng thái cục bộ từ bảng {backupTableName}. Số bản ghi khôi phục: {affectedRows}.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi thực hiện rollback đường dẫn tệp.");
                return ApiResponse<string>.Fail($"Lỗi khi thực hiện rollback: {ex.Message}");
            }
        }
    }
}
