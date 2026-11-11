using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using ProcurementSystem.API.Controllers;
using ProcurementSystem.Core.DTOs.Backup;
using ProcurementSystem.Infrastructure.Data;
using ProcurementSystem.Infrastructure.Entities;
using ProcurementSystem.Infrastructure.Services;
using Xunit;

namespace ProcurementSystem.Tests
{
    public class BackupAndRestoreTests : IDisposable
    {
        private readonly List<string> _tempFilesToCleanup = new();
        private readonly string _backupsDbDir;
        private readonly string _backupsDocsDir;

        public BackupAndRestoreTests()
        {
            var baseDir = Path.Combine(Directory.GetCurrentDirectory(), "backups");
            _backupsDbDir = Path.Combine(baseDir, "db");
            _backupsDocsDir = Path.Combine(baseDir, "docs");

            if (!Directory.Exists(_backupsDbDir)) Directory.CreateDirectory(_backupsDbDir);
            if (!Directory.Exists(_backupsDocsDir)) Directory.CreateDirectory(_backupsDocsDir);
        }

        public void Dispose()
        {
            foreach (var path in _tempFilesToCleanup)
            {
                try
                {
                    if (File.Exists(path)) File.Delete(path);
                }
                catch { }
            }
        }

        private static IConfiguration CreateMockConfiguration(string? connectionString = null)
        {
            var myConfiguration = new Dictionary<string, string?>
            {
                {"ConnectionStrings:DefaultConnection", connectionString ?? "Server=localhost;Database=procurement_db;User Id=sa;Password=123;TrustServerCertificate=True;"}
            };

            return new ConfigurationBuilder()
                .AddInMemoryCollection(myConfiguration)
                .Build();
        }

        private static async Task SeedAdminUserAsync(AppDbContext dbContext)
        {
            var adminUser = new User
            {
                Id = 1,
                FullName = "System Admin",
                Email = "admin@system.gov.vn",
                PasswordHash = "dummyhash"
            };
            dbContext.Users.Add(adminUser);
            await dbContext.SaveChangesAsync();
        }

        [Fact]
        public async Task BackupService_GetBackupOverviewAsync_ReturnsValidOverviewStructure()
        {
            // Arrange
            using var connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();
            using var dbContext = new TestDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options);
            await dbContext.Database.EnsureCreatedAsync();

            var config = CreateMockConfiguration();
            var service = new BackupService(config, dbContext, NullLogger<BackupService>.Instance);

            // Act
            var res = await service.GetBackupOverviewAsync();

            // Assert
            Assert.NotNull(res);
            Assert.True(res.Success);
            Assert.NotNull(res.Data);
            Assert.NotNull(res.Data.DatabaseBackups);
            Assert.NotNull(res.Data.DocumentBackups);
        }

        [Fact]
        public async Task BackupService_CreateDocumentsZipBackupAsync_CreatesValidZipAndAuditLog()
        {
            // Arrange
            var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "uploads");
            if (!Directory.Exists(uploadsDir)) Directory.CreateDirectory(uploadsDir);
            var sampleFile = Path.Combine(uploadsDir, $"test_doc_{Guid.NewGuid():N}.txt");
            await File.WriteAllTextAsync(sampleFile, "SAMPLE DOCUMENT CONTENT FOR BACKUP TEST");
            _tempFilesToCleanup.Add(sampleFile);

            using var connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();
            using var dbContext = new TestDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options);
            await dbContext.Database.EnsureCreatedAsync();
            await SeedAdminUserAsync(dbContext);

            var config = CreateMockConfiguration();
            var service = new BackupService(config, dbContext, NullLogger<BackupService>.Instance);

            // Act
            var res = await service.CreateDocumentsZipBackupAsync(adminUserId: 1, description: "Unit test zip backup");

            // Assert
            Assert.NotNull(res);
            Assert.True(res.Success);
            Assert.NotNull(res.Data);
            Assert.EndsWith(".zip", res.Data.FileName);
            Assert.True(res.Data.FileSizeBytes > 0);
            Assert.NotEmpty(res.Data.Sha256Hash);

            var createdZipPath = Path.Combine(_backupsDocsDir, res.Data.FileName);
            _tempFilesToCleanup.Add(createdZipPath);
            Assert.True(File.Exists(createdZipPath));

            // Verify AuditLog written
            var auditLog = await dbContext.AuditLogs.FirstOrDefaultAsync(a => a.Action == "DOCUMENTS_BACKUP_CREATED");
            Assert.NotNull(auditLog);
            Assert.Equal("Documents", auditLog.EntityType);
        }

        [Fact]
        public async Task BackupService_GetBackupFileForDownloadAsync_PreventsPathTraversal()
        {
            // Arrange
            using var connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();
            using var dbContext = new TestDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options);
            await dbContext.Database.EnsureCreatedAsync();

            var config = CreateMockConfiguration();
            var service = new BackupService(config, dbContext, NullLogger<BackupService>.Instance);

            // Act
            var resultTraversal1 = await service.GetBackupFileForDownloadAsync("../../secret.txt");
            var resultTraversal2 = await service.GetBackupFileForDownloadAsync("..\\appsettings.json");
            var resultNonExistent = await service.GetBackupFileForDownloadAsync("non_existent_file.bak");

            // Assert
            Assert.Null(resultTraversal1);
            Assert.Null(resultTraversal2);
            Assert.Null(resultNonExistent);
        }

        [Fact]
        public async Task BackupService_RestoreDatabaseBackupAsync_RequiresExactConfirmPhrase()
        {
            // Arrange
            using var connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();
            using var dbContext = new TestDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options);
            await dbContext.Database.EnsureCreatedAsync();

            var config = CreateMockConfiguration();
            var service = new BackupService(config, dbContext, NullLogger<BackupService>.Instance);

            // Act 1: Invalid phrase
            var resInvalidPhrase = await service.RestoreDatabaseBackupAsync(1, new RestoreBackupRequest
            {
                FileName = "test.bak",
                ConfirmPhrase = "WRONG_PHRASE"
            });

            // Act 2: Non-bak extension
            var resNonBak = await service.RestoreDatabaseBackupAsync(1, new RestoreBackupRequest
            {
                FileName = "test.zip",
                ConfirmPhrase = "RESTORE_DATABASE_CONFIRM"
            });

            // Act 3: Missing file
            var resMissing = await service.RestoreDatabaseBackupAsync(1, new RestoreBackupRequest
            {
                FileName = "missing_backup_20261110.bak",
                ConfirmPhrase = "RESTORE_DATABASE_CONFIRM"
            });

            // Assert
            Assert.False(resInvalidPhrase.Success);
            Assert.Equal(400, resInvalidPhrase.StatusCode);
            Assert.Contains("Cụm từ xác nhận không chính xác", resInvalidPhrase.Message);

            Assert.False(resNonBak.Success);
            Assert.Equal(400, resNonBak.StatusCode);
            Assert.Contains(".bak", resNonBak.Message);

            Assert.False(resMissing.Success);
            Assert.Equal(404, resMissing.StatusCode);
        }

        [Fact]
        public async Task BackupsController_EndToEndMockContext_AdminAllowed()
        {
            // Arrange
            using var connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();
            using var dbContext = new TestDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options);
            await dbContext.Database.EnsureCreatedAsync();

            var config = CreateMockConfiguration();
            var service = new BackupService(config, dbContext, NullLogger<BackupService>.Instance);
            var controller = new BackupsController(service);

            // Setup Admin User Claims
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, "1"),
                        new Claim(ClaimTypes.Name, "admin"),
                        new Claim(ClaimTypes.Role, "Admin")
                    }, "TestAuth"))
                }
            };

            // Act
            var actionResult = await controller.GetBackupsOverview();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task BackupService_DeleteBackupAsync_SafelyDeletesAndWritesAuditLog()
        {
            // Arrange
            var testFileName = $"test_delete_{Guid.NewGuid():N}.bak";
            var testFilePath = Path.Combine(_backupsDbDir, testFileName);
            await File.WriteAllTextAsync(testFilePath, "DUMMY TEST BAK FILE CONTENT");
            _tempFilesToCleanup.Add(testFilePath);

            using var connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();
            using var dbContext = new TestDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options);
            await dbContext.Database.EnsureCreatedAsync();
            await SeedAdminUserAsync(dbContext);

            var config = CreateMockConfiguration();
            var service = new BackupService(config, dbContext, NullLogger<BackupService>.Instance);

            // Act
            var res = await service.DeleteBackupAsync(adminUserId: 1, testFileName);

            // Assert
            Assert.True(res.Success);
            Assert.False(File.Exists(testFilePath));

            // Verify AuditLog
            var auditLog = await dbContext.AuditLogs.FirstOrDefaultAsync(a => a.Action == "BACKUP_DELETED");
            Assert.NotNull(auditLog);
            Assert.Equal("Backup", auditLog.EntityType);
        }
    }
}
