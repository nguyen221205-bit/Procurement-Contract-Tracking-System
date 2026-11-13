using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ProcurementSystem.Core.DTOs.Common;
using ProcurementSystem.Infrastructure.Data;
using ProcurementSystem.Infrastructure.Services;
using Xunit;

namespace ProcurementSystem.Tests
{
    public class HybridStorageAndAdminTests : IDisposable
    {
        private readonly List<string> _tempFilesToCleanup = new();
        private readonly FileStorageService _localService;
        private readonly FileCryptoCompressionService _cryptoService;

        public HybridStorageAndAdminTests()
        {
            _localService = new FileStorageService();
            var cryptoOptions = Options.Create(new StorageSettings
            {
                Encryption = new StorageEncryptionConfig
                {
                    Enabled = true,
                    MasterKey = "Yj794nLSArXvXh23bxXrCi40ZK4a0/M5lQLBxl8cge0="
                }
            });
            _cryptoService = new FileCryptoCompressionService(cryptoOptions, NullLogger<FileCryptoCompressionService>.Instance);
        }

        public void Dispose()
        {
            foreach (var path in _tempFilesToCleanup)
            {
                try
                {
                    if (File.Exists(path))
                    {
                        File.Delete(path);
                    }
                }
                catch
                {
                    // Ignore cleanup exceptions
                }
            }
        }

        private static IFormFile CreateMockFormFile(string fileName, string content)
        {
            var bytes = Encoding.UTF8.GetBytes(content);
            var stream = new MemoryStream(bytes);
            return new FormFile(stream, 0, bytes.Length, "file", fileName)
            {
                Headers = new HeaderDictionary(),
                ContentType = "application/pdf"
            };
        }

        [Fact]
        public async Task SaveFileAsync_WhenProviderIsLocal_SavesToLocalDiskAndReturnsRelativePath()
        {
            // Arrange
            var settings = Options.Create(new StorageSettings { Provider = "Local" });
            var supabaseService = new SupabaseStorageService(settings, _cryptoService, NullLogger<SupabaseStorageService>.Instance);
            var hybridService = new HybridFileStorageService(
                _localService,
                supabaseService,
                settings,
                NullLogger<HybridFileStorageService>.Instance);

            var file = CreateMockFormFile("test_contract_document.pdf", "%PDF-1.4 Mock Contract Content");

            // Act
            var storedPath = await hybridService.SaveFileAsync(file, "test_hybrid");

            // Assert
            Assert.NotNull(storedPath);
            Assert.False(hybridService.IsCloudPath(storedPath));
            Assert.Contains("test_hybrid", storedPath);

            var physicalPath = _localService.GetPhysicalFilePath(storedPath);
            Assert.True(File.Exists(physicalPath));
            _tempFilesToCleanup.Add(physicalPath);
        }

        [Fact]
        public async Task SaveFileAsync_WhenProviderIsSupabase_ButNotConfigured_FallsBackToLocalDisk()
        {
            // Arrange: Cấu hình chọn Provider là Supabase nhưng cố tình để trống Endpoint / S3 keys
            var settings = Options.Create(new StorageSettings
            {
                Provider = "Supabase",
                Supabase = new SupabaseStorageConfig
                {
                    Endpoint = "",
                    AccessKeyId = "",
                    SecretAccessKey = ""
                }
            });
            var supabaseService = new SupabaseStorageService(settings, _cryptoService, NullLogger<SupabaseStorageService>.Instance);
            var hybridService = new HybridFileStorageService(
                _localService,
                supabaseService,
                settings,
                NullLogger<HybridFileStorageService>.Instance);

            var file = CreateMockFormFile("fallback_acceptance.pdf", "%PDF-1.4 Mock Acceptance Data");

            // Act: Lưu tệp trong điều kiện Supabase chưa cấu hình
            var storedPath = await hybridService.SaveFileAsync(file, "test_fallback");

            // Assert: Hệ thống tự động fallback về Local Storage, không bị ném ngoại lệ hay crash
            Assert.NotNull(storedPath);
            Assert.False(hybridService.IsCloudPath(storedPath));

            var physicalPath = _localService.GetPhysicalFilePath(storedPath);
            Assert.True(File.Exists(physicalPath));
            _tempFilesToCleanup.Add(physicalPath);
        }

        [Theory]
        [InlineData("supabase://procurement-documents/contracts/doc.pdf", true)]
        [InlineData("https://project.storage.supabase.co/v1/s3/doc.pdf", true)]
        [InlineData("http://project.storage.supabase.co/v1/s3/doc.pdf", true)]
        [InlineData("uploads/contracts/doc.pdf", false)]
        [InlineData("uploads\\test\\doc.pdf", false)]
        [InlineData("", false)]
        [InlineData(null, false)]
        public void IsCloudPath_ShouldCorrectlyIdentifyProtocols(string? storedPath, bool expectedIsCloud)
        {
            // Arrange
            var settings = Options.Create(new StorageSettings { Provider = "Local" });
            var supabaseService = new SupabaseStorageService(settings, _cryptoService, NullLogger<SupabaseStorageService>.Instance);
            var hybridService = new HybridFileStorageService(
                _localService,
                supabaseService,
                settings,
                NullLogger<HybridFileStorageService>.Instance);

            // Act
            var actual = hybridService.IsCloudPath(storedPath!);

            // Assert
            Assert.Equal(expectedIsCloud, actual);
        }

        [Fact]
        public void GetPhysicalFilePath_WhenCloudPath_ThrowsInvalidOperationException()
        {
            // Arrange
            var settings = Options.Create(new StorageSettings { Provider = "Local" });
            var supabaseService = new SupabaseStorageService(settings, _cryptoService, NullLogger<SupabaseStorageService>.Instance);
            var hybridService = new HybridFileStorageService(
                _localService,
                supabaseService,
                settings,
                NullLogger<HybridFileStorageService>.Instance);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() =>
                hybridService.GetPhysicalFilePath("supabase://bucket/contracts/file.pdf"));
        }

        [Fact]
        public async Task ResolveDownloadAsync_WhenLocalPath_ResolvesToPhysicalFilePath()
        {
            // Arrange
            var settings = Options.Create(new StorageSettings { Provider = "Local" });
            var supabaseService = new SupabaseStorageService(settings, _cryptoService, NullLogger<SupabaseStorageService>.Instance);
            var hybridService = new HybridFileStorageService(
                _localService,
                supabaseService,
                settings,
                NullLogger<HybridFileStorageService>.Instance);

            var file = CreateMockFormFile("resolve_test.pdf", "%PDF-1.4 Content To Resolve");
            var storedPath = await hybridService.SaveFileAsync(file, "test_resolve");
            var physicalPath = _localService.GetPhysicalFilePath(storedPath);
            _tempFilesToCleanup.Add(physicalPath);

            // Act
            using var downloadResult = await hybridService.ResolveDownloadAsync(storedPath, "resolve_test.pdf");

            // Assert
            Assert.NotNull(downloadResult);
            Assert.False(downloadResult.IsRedirect);
            Assert.Equal(physicalPath, downloadResult.PhysicalPath);
        }

        [Fact]
        public async Task SupabaseStorageService_TestAndEnsureBucketAsync_WhenUnconfigured_ReturnsSafeFailure()
        {
            // Arrange
            var settings = Options.Create(new StorageSettings
            {
                Provider = "Local",
                Supabase = new SupabaseStorageConfig
                {
                    Endpoint = "",
                    AccessKeyId = "",
                    SecretAccessKey = ""
                }
            });
            var supabaseService = new SupabaseStorageService(settings, _cryptoService, NullLogger<SupabaseStorageService>.Instance);

            // Act
            var (success, message, buckets) = await supabaseService.TestAndEnsureBucketAsync();

            // Assert
            Assert.False(success);
            Assert.Contains("chưa được cấu hình", message);
            Assert.Empty(buckets);
        }

        [Fact]
        public async Task StorageMigrationService_GetStorageStatusAsync_ReturnsValidLocalMetrics()
        {
            // Arrange
            var settings = Options.Create(new StorageSettings
            {
                Provider = "Local",
                Supabase = new SupabaseStorageConfig
                {
                    BucketName = "procurement-documents"
                }
            });
            var supabaseService = new SupabaseStorageService(settings, _cryptoService, NullLogger<SupabaseStorageService>.Instance);
            
            using var connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();
            using var dbContext = new TestDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options);
            await dbContext.Database.EnsureCreatedAsync();

            var migrationService = new StorageMigrationService(
                dbContext,
                supabaseService,
                settings,
                NullLogger<StorageMigrationService>.Instance);

            // Act
            var response = await migrationService.GetStorageStatusAsync();

            // Assert
            Assert.NotNull(response);
            Assert.True(response.Success);
            Assert.Equal("Local", response.Data?.ActiveProvider);
            Assert.False(response.Data?.IsSupabaseConfigured);
            Assert.Equal("procurement-documents", response.Data?.SupabaseBucket);
            Assert.True(response.Data?.LocalFilesCount >= 0);
            Assert.True(response.Data?.LocalUploadsSizeBytes >= 0);
        }

        [Fact]
        public async Task StorageMigrationService_SyncLocalFilesToCloudAsync_WhenNotConfigured_ReturnsSafeError()
        {
            // Arrange
            var settings = Options.Create(new StorageSettings
            {
                Provider = "Local",
                Supabase = new SupabaseStorageConfig
                {
                    Endpoint = "",
                    AccessKeyId = "",
                    SecretAccessKey = ""
                }
            });
            var supabaseService = new SupabaseStorageService(settings, _cryptoService, NullLogger<SupabaseStorageService>.Instance);
            
            using var connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();
            using var dbContext = new TestDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options);
            await dbContext.Database.EnsureCreatedAsync();

            var migrationService = new StorageMigrationService(
                dbContext,
                supabaseService,
                settings,
                NullLogger<StorageMigrationService>.Instance);

            // Act
            var response = await migrationService.SyncLocalFilesToCloudAsync(adminUserId: 1);

            // Assert
            Assert.NotNull(response);
            Assert.False(response.Success);
            Assert.Contains("chưa được cấu hình", response.Message);
        }

        [Fact]
        public async Task GetPreSignedUrlAsync_WhenLocalPath_ReturnsValidFileUriWithoutThrowing()
        {
            // Arrange (LSP: FileStorageService & HybridFileStorageService must not throw NotSupportedException on local paths)
            var settings = Options.Create(new StorageSettings { Provider = "Local" });
            var supabaseService = new SupabaseStorageService(settings, _cryptoService, NullLogger<SupabaseStorageService>.Instance);
            var hybridService = new HybridFileStorageService(
                _localService,
                supabaseService,
                settings,
                NullLogger<HybridFileStorageService>.Instance);

            var localRelativePath = "uploads/contracts/lsp_check.pdf";

            // Act
            var localUri = await _localService.GetPreSignedUrlAsync(localRelativePath, 15);
            var hybridUri = await hybridService.GetPreSignedUrlAsync(localRelativePath, 15);

            // Assert
            Assert.StartsWith("file://", localUri, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(localUri, hybridUri);
        }

        [Fact]
        public void DocumentSampleFallbackProvider_GeneratesValidPdfDocumentByCategory()
        {
            // Arrange (SRP: Sample generation isolated in IDocumentSampleFallbackProvider)
            var sampleProvider = DocumentSampleFallbackProvider.Instance;

            // Act
            var contractPdf = sampleProvider.GenerateSampleDocumentBytes("contracts/hd-001.pdf", "hd-001.pdf");
            var guaranteePdf = sampleProvider.GenerateSampleDocumentBytes("guarantees/bl-001.pdf", "bl-001.pdf");

            // Assert: PDF header magic bytes 0x25 0x50 0x44 0x46
            Assert.True(contractPdf.Length > 500);
            Assert.Equal(0x25, contractPdf[0]);
            Assert.Equal(0x50, contractPdf[1]);
            Assert.Equal(0x44, contractPdf[2]);
            Assert.Equal(0x46, contractPdf[3]);

            Assert.True(guaranteePdf.Length > 500);
            Assert.Equal(0x25, guaranteePdf[0]);
        }
    }
}
