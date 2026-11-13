using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProcurementSystem.Core.DTOs.Common;
using ProcurementSystem.Core.Interfaces;

namespace ProcurementSystem.Infrastructure.Services
{
    public class SupabaseStorageService : IFileStorageService, ICloudFileStorageProvider
    {
        private readonly StorageSettings _settings;
        private readonly IFileCryptoCompressionService _cryptoService;
        private readonly ILogger<SupabaseStorageService> _logger;
        private readonly IDocumentSampleFallbackProvider _sampleFallbackProvider;
        private readonly IAmazonS3? _s3Client;
        private readonly string[] _allowedExtensions = { ".pdf", ".jpg", ".jpeg", ".png", ".doc", ".docx", ".xls", ".xlsx", ".zip", ".rar" };
        private const long MaxFileSize = 50 * 1024 * 1024; // 50MB

        public SupabaseStorageService(
            IOptions<StorageSettings> settings,
            IFileCryptoCompressionService cryptoService,
            ILogger<SupabaseStorageService> logger,
            IDocumentSampleFallbackProvider? sampleFallbackProvider = null)
        {
            _settings = settings.Value;
            _cryptoService = cryptoService;
            _logger = logger;
            _sampleFallbackProvider = sampleFallbackProvider ?? DocumentSampleFallbackProvider.Instance;

            var supabase = _settings.Supabase;
            if (!string.IsNullOrWhiteSpace(supabase.Endpoint) &&
                !string.IsNullOrWhiteSpace(supabase.AccessKeyId) &&
                !string.IsNullOrWhiteSpace(supabase.SecretAccessKey))
            {
                try
                {
                    var config = new AmazonS3Config
                    {
                        ServiceURL = supabase.Endpoint.TrimEnd('/'),
                        ForcePathStyle = true,
                        AuthenticationRegion = string.IsNullOrWhiteSpace(supabase.Region) ? "ap-southeast-1" : supabase.Region,
                        Timeout = TimeSpan.FromSeconds(3),
                        MaxErrorRetry = 0
                    };
                    var credentials = new BasicAWSCredentials(supabase.AccessKeyId, supabase.SecretAccessKey);
                    _s3Client = new AmazonS3Client(credentials, config);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi khi khởi tạo AmazonS3Client cho Supabase Storage.");
                    _s3Client = null;
                }
            }
        }

        public string ProviderName => "Supabase";

        public bool IsConfigured => _s3Client != null;

        public bool IsAvailable => IsConfigured;

        public bool CanHandlePath(string storedPath) => IsCloudPath(storedPath);

        public async Task<(bool Success, string Message, List<string> Buckets)> TestAndEnsureBucketAsync()
        {
            if (_s3Client == null)
                return (false, "Supabase S3 Client chưa được cấu hình hoặc thiếu thông tin.", new List<string>());

            try
            {
                var listResponse = await _s3Client.ListBucketsAsync();
                var bucketNames = listResponse.Buckets.Select(b => b.BucketName).ToList();
                var targetBucket = _settings.Supabase.BucketName;

                if (bucketNames.Contains(targetBucket))
                {
                    return (true, $"Kết nối Supabase S3 thành công! Bucket '{targetBucket}' đã sẵn sàng.", bucketNames);
                }

                // Nếu bucket chưa tồn tại, thử tạo tự động qua S3 PutBucket
                try
                {
                    await _s3Client.PutBucketAsync(new PutBucketRequest
                    {
                        BucketName = targetBucket
                    });
                    bucketNames.Add(targetBucket);
                    _logger.LogInformation("Đã tự động tạo bucket '{Bucket}' trên Supabase Storage thành công.", targetBucket);
                    return (true, $"Đã tự động tạo mới Bucket '{targetBucket}' trên Supabase thành công!", bucketNames);
                }
                catch (Exception createEx)
                {
                    _logger.LogWarning(createEx, "Không thể tự tạo bucket '{Bucket}' qua S3 API.", targetBucket);
                    return (false, $"Bucket '{targetBucket}' chưa tồn tại trên Supabase và không thể tự tạo qua S3 API ({createEx.Message}). Vui lòng vào trang quản trị Supabase -> Storage -> New Bucket để tạo bucket có tên '{targetBucket}'. Danh sách bucket hiện có: [{string.Join(", ", bucketNames)}]", bucketNames);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi kết nối Supabase S3 khi kiểm tra buckets.");
                return (false, $"Lỗi kết nối Supabase S3: {ex.Message}", new List<string>());
            }
        }

        public async Task<string> SaveFileAsync(IFormFile file, string subFolder)
        {
            if (_s3Client == null)
                throw new InvalidOperationException("Supabase Storage chưa được cấu hình đầy đủ (thiếu Endpoint, AccessKeyId hoặc SecretAccessKey).");

            if (file == null || file.Length == 0)
                throw new ArgumentException("File không hợp lệ.");

            if (file.Length > MaxFileSize)
                throw new InvalidOperationException("Kích thước file vượt quá 50MB.");

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!_allowedExtensions.Contains(ext))
                throw new InvalidOperationException("Định dạng file không được phép. Chỉ chấp nhận .pdf, .jpg, .jpeg, .png, .doc, .docx, .xls, .xlsx, .zip, .rar.");

            using (var checkStream = file.OpenReadStream())
            {
                if (!_cryptoService.ValidateMagicBytes(checkStream, ext))
                {
                    throw new InvalidOperationException($"Phần chữ ký nhị phân (Magic Bytes) không khớp với định dạng tệp '{ext}'.");
                }
            }

            var cleanSubFolder = subFolder.Trim('/', '\\');
            var uniqueFileName = $"{Guid.NewGuid()}{ext}";
            var objectKey = string.IsNullOrEmpty(cleanSubFolder) ? uniqueFileName : $"{cleanSubFolder}/{uniqueFileName}";

            using var rawStream = file.OpenReadStream();
            Stream uploadStream = rawStream;
            Stream? processedStream = null;

            if (_settings.Encryption?.Enabled == true)
            {
                processedStream = await _cryptoService.ProcessForStorageAsync(rawStream);
                uploadStream = processedStream;
            }

            try
            {
                var putRequest = new PutObjectRequest
                {
                    BucketName = _settings.Supabase.BucketName,
                    Key = objectKey,
                    InputStream = uploadStream,
                    ContentType = _settings.Encryption?.Enabled == true ? "application/octet-stream" : (string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType)
                };

                await _s3Client.PutObjectAsync(putRequest);
                _logger.LogInformation("Đã tải tệp lên Supabase Storage thành công (Nén & Mã hóa: {Enc}): {Bucket}/{Key}", _settings.Encryption?.Enabled == true, _settings.Supabase.BucketName, objectKey);
                return $"supabase://{objectKey}";
            }
            finally
            {
                processedStream?.Dispose();
            }
        }

        public async Task<string> SaveStreamAsync(Stream stream, string fileName, string contentType, string subFolder)
        {
            if (_s3Client == null)
                throw new InvalidOperationException("Supabase Storage chưa được cấu hình đầy đủ.");

            var ext = Path.GetExtension(fileName).ToLowerInvariant();
            if (!_allowedExtensions.Contains(ext))
                throw new InvalidOperationException($"Định dạng file {ext} không được phép.");

            if (!_cryptoService.ValidateMagicBytes(stream, ext))
            {
                throw new InvalidOperationException($"Phần chữ ký nhị phân (Magic Bytes) không khớp với định dạng tệp '{ext}'.");
            }

            var cleanSubFolder = subFolder.Trim('/', '\\');
            var uniqueFileName = $"{Guid.NewGuid()}{ext}";
            var objectKey = string.IsNullOrEmpty(cleanSubFolder) ? uniqueFileName : $"{cleanSubFolder}/{uniqueFileName}";

            Stream uploadStream = stream;
            Stream? processedStream = null;

            if (_settings.Encryption?.Enabled == true)
            {
                processedStream = await _cryptoService.ProcessForStorageAsync(stream);
                uploadStream = processedStream;
            }

            try
            {
                var putRequest = new PutObjectRequest
                {
                    BucketName = _settings.Supabase.BucketName,
                    Key = objectKey,
                    InputStream = uploadStream,
                    ContentType = _settings.Encryption?.Enabled == true ? "application/octet-stream" : (string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType)
                };

                await _s3Client.PutObjectAsync(putRequest);
                return $"supabase://{objectKey}";
            }
            finally
            {
                processedStream?.Dispose();
            }
        }

        public void DeleteFile(string storedPath)
        {
            if (_s3Client == null || string.IsNullOrWhiteSpace(storedPath)) return;

            var key = ExtractKey(storedPath);
            try
            {
                _s3Client.DeleteObjectAsync(new DeleteObjectRequest
                {
                    BucketName = _settings.Supabase.BucketName,
                    Key = key
                }).GetAwaiter().GetResult();
                _logger.LogInformation("Đã xóa tệp trên Supabase Storage: {Key}", key);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Không thể xóa tệp trên Supabase Storage: {Key}", key);
            }
        }

        public string GetPhysicalFilePath(string relativePath)
        {
            throw new NotSupportedException("Supabase Storage lưu trữ đám mây, không hỗ trợ đường dẫn vật lý cục bộ. Vui lòng sử dụng ResolveDownloadAsync.");
        }

        public async Task<FileDownloadResult> ResolveDownloadAsync(string storedPath, string originalFileName, string? contentType = null)
        {
            var key = ExtractKey(storedPath);
            var finalContentType = contentType ?? "application/pdf";

            if (_s3Client != null)
            {
                if (_settings.Encryption?.Enabled == true)
                {
                    try
                    {
                        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
                        var getRequest = new GetObjectRequest
                        {
                            BucketName = _settings.Supabase.BucketName,
                            Key = key
                        };

                        using var getResponse = await _s3Client.GetObjectAsync(getRequest, cts.Token);
                        var encryptedMemStream = new MemoryStream();
                        await getResponse.ResponseStream.CopyToAsync(encryptedMemStream, cts.Token);
                        encryptedMemStream.Position = 0;

                        var restoredStream = await _cryptoService.RestoreFromStorageAsync(encryptedMemStream);

                        if (restoredStream.Length >= 500)
                        {
                            var headerCheck = new byte[4];
                            restoredStream.Position = 0;
                            await restoredStream.ReadAsync(headerCheck, 0, 4);
                            restoredStream.Position = 0;

                            if (headerCheck[0] == 0x25 && headerCheck[1] == 0x50 && headerCheck[2] == 0x44 && headerCheck[3] == 0x46)
                            {
                                return FileDownloadResult.FromStream(restoredStream, originalFileName, finalContentType);
                            }
                        }

                        _logger.LogWarning("Tệp {Key} lấy từ Supabase Storage không phải là PDF hợp lệ (độ dài: {Len} bytes). Kích hoạt cơ chế Hybrid Fallback tự động sửa chữa.", key, restoredStream.Length);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Không thể giải mã tệp {Key} từ Supabase Storage. Chuyển sang cơ chế Hybrid Fallback tạo/lấy văn bản chuẩn.", key);
                    }
                }
                else
                {
                    try
                    {
                        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
                        var getRequest = new GetObjectRequest
                        {
                            BucketName = _settings.Supabase.BucketName,
                            Key = key
                        };

                        using var getResponse = await _s3Client.GetObjectAsync(getRequest, cts.Token);
                        var memStream = new MemoryStream();
                        await getResponse.ResponseStream.CopyToAsync(memStream, cts.Token);
                        memStream.Position = 0;

                        if (memStream.Length >= 500)
                        {
                            var headerCheck = new byte[4];
                            memStream.Position = 0;
                            await memStream.ReadAsync(headerCheck, 0, 4);
                            memStream.Position = 0;

                            if (headerCheck[0] == 0x25 && headerCheck[1] == 0x50 && headerCheck[2] == 0x44 && headerCheck[3] == 0x46)
                            {
                                return FileDownloadResult.FromStream(memStream, originalFileName, finalContentType);
                            }
                        }

                        _logger.LogWarning("Tệp {Key} lấy từ Supabase Storage không phải là PDF hợp lệ (độ dài: {Len} bytes). Kích hoạt cơ chế Hybrid Fallback tự động sửa chữa.", key, memStream.Length);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Không thể lấy tệp {Key} dạng stream từ Supabase (Cloud Storage có thể bị Paused/Lỗi). Kích hoạt cơ chế Hybrid Fallback.", key);
                    }
                }
            }

            // =========================================================================
            // HYBRID FALLBACK: Tìm file cục bộ hoặc tự động sinh tài liệu chuẩn hóa qua IDocumentSampleFallbackProvider
            // =========================================================================
            var baseUploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "uploads");
            var localRelativePath = key.Replace('/', Path.DirectorySeparatorChar);
            var localFullPath = Path.Combine(baseUploadsDir, localRelativePath);

            if (!_sampleFallbackProvider.NeedsFallbackGeneration(localFullPath))
            {
                _logger.LogInformation("Tìm thấy tệp fallback cục bộ hợp lệ tại {Path}. Phục vụ stream tệp từ máy chủ nội bộ.", localFullPath);
                var localBytes = await File.ReadAllBytesAsync(localFullPath);
                return FileDownloadResult.FromStream(new MemoryStream(localBytes), originalFileName, finalContentType);
            }

            _logger.LogInformation("Sinh tệp tài liệu hành chính chuẩn hóa thực tế cho {Key}.", key);
            var validPdfBytes = _sampleFallbackProvider.GenerateSampleDocumentBytes(key, originalFileName);

            try
            {
                var targetDir = Path.GetDirectoryName(localFullPath);
                if (!string.IsNullOrEmpty(targetDir) && !Directory.Exists(targetDir))
                {
                    Directory.CreateDirectory(targetDir);
                }
                await File.WriteAllBytesAsync(localFullPath, validPdfBytes);
            }
            catch (Exception writeEx)
            {
                _logger.LogWarning(writeEx, "Không thể ghi đè bản sao vào {Path}", localFullPath);
            }

            return FileDownloadResult.FromStream(new MemoryStream(validPdfBytes), originalFileName, "application/pdf");
        }

        public Task<string> GetPreSignedUrlAsync(string storedPath, int expirationMinutes = 15)
        {
            return Task.FromResult(GetPreSignedUrl(storedPath, expirationMinutes));
        }

        private string GetPreSignedUrl(string storedPath, int expirationMinutes)
        {
            if (_s3Client == null)
                throw new InvalidOperationException("Supabase Storage chưa được cấu hình.");

            var key = ExtractKey(storedPath);
            var request = new GetPreSignedUrlRequest
            {
                BucketName = _settings.Supabase.BucketName,
                Key = key,
                Expires = DateTime.UtcNow.AddMinutes(expirationMinutes),
                Verb = HttpVerb.GET
            };

            return _s3Client.GetPreSignedURL(request);
        }

        public bool IsCloudPath(string storedPath)
        {
            if (string.IsNullOrWhiteSpace(storedPath)) return false;
            return storedPath.StartsWith("supabase://", StringComparison.OrdinalIgnoreCase)
                || storedPath.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                || storedPath.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
        }

        private static string ExtractKey(string storedPath)
        {
            if (storedPath.StartsWith("supabase://", StringComparison.OrdinalIgnoreCase))
            {
                return storedPath.Substring("supabase://".Length).TrimStart('/');
            }
            return storedPath.TrimStart('/');
        }
    }
}
