using System.Text;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using ProcurementSystem.Core.DTOs.Common;
using ProcurementSystem.Infrastructure.Data;
using ProcurementSystem.Infrastructure.Services;
using Xunit;

namespace ProcurementSystem.Tests
{
    public class HybridStorageAndAdminTests : IDisposable
    {
        private const string ValidMasterKey = "Yj794nLSArXvXh23bxXrCi40ZK4a0/M5lQLBxl8cge0=";
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
                    MasterKey = ValidMasterKey
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

        #region R-B01 & R-B02 New Unit Tests

        [Fact]
        public async Task StorageSettings_WhenProviderLocal_AndNoMasterKey_ServicesResolveAndWorkProperly()
        {
            // R-B01: Khi Provider = "Local" và không có MasterKey (Encryption.Enabled = false, MasterKey = "")
            var settings = Options.Create(new StorageSettings
            {
                Provider = "Local",
                Encryption = new StorageEncryptionConfig
                {
                    Enabled = false,
                    MasterKey = ""
                }
            });

            var crypto = new FileCryptoCompressionService(settings, NullLogger<FileCryptoCompressionService>.Instance);
            var local = new FileStorageService();
            var supabase = new SupabaseStorageService(settings, crypto, NullLogger<SupabaseStorageService>.Instance);
            var hybrid = new HybridFileStorageService(local, supabase, settings, NullLogger<HybridFileStorageService>.Instance);

            // Hoạt động bình thường qua DI mà không throw
            Assert.NotNull(hybrid);
            var file = CreateMockFormFile("di_test.pdf", "%PDF-1.4 DI Content Without Key");
            var storedPath = await hybrid.SaveFileAsync(file, "test_di");

            Assert.NotNull(storedPath);
            Assert.False(hybrid.IsCloudPath(storedPath));

            var physicalPath = local.GetPhysicalFilePath(storedPath);
            Assert.True(File.Exists(physicalPath));
            _tempFilesToCleanup.Add(physicalPath);
        }

        [Fact]
        public void StorageSettings_WhenProviderLocal_AndEncryptionEnabledWithoutMasterKey_ServicesResolveProperly()
        {
            // R-B01: Khi Provider = "Local", nếu Encryption:Enabled vô tình bật nhưng MasterKey rỗng
            var settings = Options.Create(new StorageSettings
            {
                Provider = "Local",
                Encryption = new StorageEncryptionConfig
                {
                    Enabled = true,
                    MasterKey = ""
                }
            });

            var crypto = new FileCryptoCompressionService(settings, NullLogger<FileCryptoCompressionService>.Instance);
            var local = new FileStorageService();
            var supabase = new SupabaseStorageService(settings, crypto, NullLogger<SupabaseStorageService>.Instance);
            var hybrid = new HybridFileStorageService(local, supabase, settings, NullLogger<HybridFileStorageService>.Instance);

            // Khởi tạo thành công, không ném ngoại lệ
            Assert.NotNull(crypto);
            Assert.NotNull(hybrid);
        }

        [Fact]
        public async Task SupabaseStorageService_ResolveDownloadAsync_WhenEncryptionDisabled_ButFileIsEncrypted_DecryptsSuccessfully()
        {
            // R-B02: Tệp cũ trên Supabase đã được mã hóa (header PSEN).
            // Cấu hình hiện tại: Encryption.Enabled = false (để ghi file mới dạng plaintext), nhưng MasterKey vẫn được cung cấp.
            var settings = Options.Create(new StorageSettings
            {
                Provider = "Supabase",
                Supabase = new SupabaseStorageConfig
                {
                    BucketName = "procurement-documents"
                },
                Encryption = new StorageEncryptionConfig
                {
                    Enabled = false, // TẮT mã hóa chiều GHI
                    MasterKey = ValidMasterKey // Nhưng có khóa để giải mã chiều ĐỌC file cũ
                }
            });

            var originalContent = "%PDF-1.4 Nội dung tài liệu đấu thầu bảo mật cũ trên Supabase Cloud";
            using var rawStream = new MemoryStream(Encoding.UTF8.GetBytes(originalContent));
            using var encryptedStream = await _cryptoService.ProcessForStorageAsync(rawStream);
            var encryptedBytes = ((MemoryStream)encryptedStream).ToArray();

            // Mock IAmazonS3 trả về ciphertext stream của tệp PSEN
            var mockS3 = new Mock<IAmazonS3>();
            mockS3.Setup(s => s.GetObjectAsync(It.Is<GetObjectRequest>(r => r.Key == "contracts/old_encrypted.pdf"), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(new GetObjectResponse
                  {
                      ResponseStream = new MemoryStream(encryptedBytes)
                  });

            var supabaseService = new SupabaseStorageService(
                settings,
                _cryptoService,
                NullLogger<SupabaseStorageService>.Instance,
                mockS3.Object);

            // Act: Gọi ResolveDownloadAsync
            using var downloadResult = await supabaseService.ResolveDownloadAsync("supabase://contracts/old_encrypted.pdf", "old_encrypted.pdf");

            // Assert: Hệ thống tự động nhận diện header PSEN và giải mã thành công thành plaintext stream!
            Assert.NotNull(downloadResult);
            Assert.False(downloadResult.IsRedirect);
            Assert.NotNull(downloadResult.FileStream);

            using var reader = new StreamReader(downloadResult.FileStream, Encoding.UTF8);
            var decryptedContent = await reader.ReadToEndAsync();
            Assert.Equal(originalContent, decryptedContent);
        }

        [Fact]
        public async Task SupabaseStorageService_ResolveDownloadAsync_WhenFileIsEncrypted_AndMasterKeyMissing_ThrowsInvalidOperationException()
        {
            // R-B02: Tệp trên Supabase có header PSEN nhưng hệ thống không có MasterKey
            var settings = Options.Create(new StorageSettings
            {
                Provider = "Supabase",
                Supabase = new SupabaseStorageConfig
                {
                    BucketName = "procurement-documents"
                },
                Encryption = new StorageEncryptionConfig
                {
                    Enabled = false,
                    MasterKey = "" // Thiếu MasterKey
                }
            });

            var originalContent = "%PDF-1.4 Dữ liệu mẫu cần giải mã";
            using var rawStream = new MemoryStream(Encoding.UTF8.GetBytes(originalContent));
            using var encryptedStream = await _cryptoService.ProcessForStorageAsync(rawStream);
            var encryptedBytes = ((MemoryStream)encryptedStream).ToArray();

            var cryptoWithoutKey = new FileCryptoCompressionService(settings, NullLogger<FileCryptoCompressionService>.Instance);

            var mockS3 = new Mock<IAmazonS3>();
            mockS3.Setup(s => s.GetObjectAsync(It.IsAny<GetObjectRequest>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(new GetObjectResponse
                  {
                      ResponseStream = new MemoryStream(encryptedBytes)
                  });

            var supabaseService = new SupabaseStorageService(
                settings,
                cryptoWithoutKey,
                NullLogger<SupabaseStorageService>.Instance,
                mockS3.Object);

            // Act & Assert: Phải ném InvalidOperationException, TUYỆT ĐỐI KHÔNG redirect Pre-signed URL
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                supabaseService.ResolveDownloadAsync("supabase://contracts/enc_doc.pdf", "enc_doc.pdf"));

            Assert.Contains("MasterKey", ex.Message);
        }

        #endregion

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

        [Fact]
        public async Task DocumentSampleFallbackProvider_NeedsFallbackGeneration_IdentifiesEmptyStubAndInvalidPdfs()
        {
            var provider = DocumentSampleFallbackProvider.Instance;

            // 1. File không tồn tại
            var nonExistentPath = Path.Combine(Path.GetTempPath(), $"not_exist_{Guid.NewGuid():N}.pdf");
            Assert.True(provider.NeedsFallbackGeneration(nonExistentPath));

            // 2. File rỗng (0 bytes)
            var emptyPath = Path.Combine(Path.GetTempPath(), $"empty_{Guid.NewGuid():N}.pdf");
            await File.WriteAllBytesAsync(emptyPath, Array.Empty<byte>());
            _tempFilesToCleanup.Add(emptyPath);
            Assert.True(provider.NeedsFallbackGeneration(emptyPath));

            // 3. File stub notice HTML (chứa Project Paused)
            var stubHtmlPath = Path.Combine(Path.GetTempPath(), $"stub_{Guid.NewGuid():N}.pdf");
            await File.WriteAllTextAsync(stubHtmlPath, "<html><body>Project Paused</body></html>");
            _tempFilesToCleanup.Add(stubHtmlPath);
            Assert.True(provider.NeedsFallbackGeneration(stubHtmlPath));

            // 4. File PDF hợp lệ (bắt đầu bằng %PDF và có dung lượng > 20 bytes)
            var validPdfPath = Path.Combine(Path.GetTempPath(), $"valid_{Guid.NewGuid():N}.pdf");
            await File.WriteAllBytesAsync(validPdfPath, Encoding.ASCII.GetBytes("%PDF-1.4\n1 0 obj\n<< /Type /Catalog >>\nendobj\n%%EOF"));
            _tempFilesToCleanup.Add(validPdfPath);
            Assert.False(provider.NeedsFallbackGeneration(validPdfPath));

            // 5. File non-PDF hợp lệ (ví dụ ảnh .png 120 bytes dữ liệu hợp lệ)
            var validPngPath = Path.Combine(Path.GetTempPath(), $"valid_{Guid.NewGuid():N}.png");
            var dummyPngBytes = new byte[120];
            new Random(42).NextBytes(dummyPngBytes);
            await File.WriteAllBytesAsync(validPngPath, dummyPngBytes);
            _tempFilesToCleanup.Add(validPngPath);
            Assert.False(provider.NeedsFallbackGeneration(validPngPath));
        }

        [Fact]
        public async Task SupabaseStorageService_ResolveDownloadAsync_WhenLocalCopyIsCorruptOrEmpty_ReplacesWithValidFallback()
        {
            var settings = Options.Create(new StorageSettings
            {
                Provider = "Supabase",
                Supabase = new SupabaseStorageConfig
                {
                    BucketName = "procurement-documents"
                }
            });

            var supabaseService = new SupabaseStorageService(
                settings,
                _cryptoService,
                NullLogger<SupabaseStorageService>.Instance);

            var key = $"contracts/test_corrupt_{Guid.NewGuid():N}.pdf";
            var baseUploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "uploads");
            var localRelativePath = key.Replace('/', Path.DirectorySeparatorChar);
            var localFullPath = Path.Combine(baseUploadsDir, localRelativePath);

            var dir = Path.GetDirectoryName(localFullPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            // Ghi file rỗng vào bản sao local
            await File.WriteAllBytesAsync(localFullPath, Array.Empty<byte>());
            _tempFilesToCleanup.Add(localFullPath);

            // Act: Khi tải, service phát hiện bản sao local rỗng -> tự động sinh tài liệu chuẩn và ghi đè
            var result = await supabaseService.ResolveDownloadAsync($"supabase://{key}", "test_doc.pdf");

            // Assert: Kết quả trả về phải là PDF hợp lệ, không phải 0 bytes
            Assert.NotNull(result);
            Assert.True(result.FileStream.Length > 500);

            var header = new byte[4];
            result.FileStream.Position = 0;
            _ = await result.FileStream.ReadAsync(header, 0, 4);
            Assert.Equal((byte)'%', header[0]);
            Assert.Equal((byte)'P', header[1]);
            Assert.Equal((byte)'D', header[2]);
            Assert.Equal((byte)'F', header[3]);
        }
    }
}
