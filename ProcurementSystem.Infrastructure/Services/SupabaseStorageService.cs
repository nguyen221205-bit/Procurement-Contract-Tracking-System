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
    public class SupabaseStorageService : IFileStorageService
    {
        private readonly StorageSettings _settings;
        private readonly IFileCryptoCompressionService _cryptoService;
        private readonly ILogger<SupabaseStorageService> _logger;
        private readonly IAmazonS3? _s3Client;
        private readonly string[] _allowedExtensions = { ".pdf", ".jpg", ".jpeg", ".png", ".doc", ".docx", ".xls", ".xlsx", ".zip", ".rar" };
        private const long MaxFileSize = 50 * 1024 * 1024; // 50MB

        public SupabaseStorageService(
            IOptions<StorageSettings> settings,
            IFileCryptoCompressionService cryptoService,
            ILogger<SupabaseStorageService> logger)
            : this(settings, cryptoService, logger, null)
        {
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
                        AuthenticationRegion = string.IsNullOrWhiteSpace(supabase.Region) ? "ap-southeast-1" : supabase.Region
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

        public SupabaseStorageService(
            IOptions<StorageSettings> settings,
            IFileCryptoCompressionService cryptoService,
            ILogger<SupabaseStorageService> logger,
            IAmazonS3? s3Client)
        {
            _settings = settings.Value;
            _cryptoService = cryptoService;
            _logger = logger;
            _s3Client = s3Client;
        }

        public bool IsConfigured => _s3Client != null;

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

                if (_settings.Encryption?.Enabled == true)
                {
                    putRequest.Metadata.Add("x-amz-meta-encrypted", "true");
                }

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

                if (_settings.Encryption?.Enabled == true)
                {
                    putRequest.Metadata.Add("x-amz-meta-encrypted", "true");
                }

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
            if (_s3Client == null)
                throw new InvalidOperationException("Supabase Storage chưa được cấu hình.");

            var key = ExtractKey(storedPath);
            var finalContentType = contentType ?? "application/octet-stream";

            // R-B02: Đọc file cũ độc lập với cờ _settings.Encryption?.Enabled.
            // Bất kể cờ ghi file mới bật hay tắt: Kéo stream từ S3 về và đưa qua RestoreFromStorageAsync.
            // RestoreFromStorageAsync sẽ tự nhận diện header:
            // - Nếu là tệp mã hóa (header PSEN) -> giải mã AES-256 + GZip bằng MasterKey.
            // - Nếu là tệp plaintext (legacy) -> trả về stream gốc (passthrough).
            try
            {
                var getRequest = new GetObjectRequest
                {
                    BucketName = _settings.Supabase.BucketName,
                    Key = key
                };

                using var getResponse = await _s3Client.GetObjectAsync(getRequest);
                var downloadedMemStream = new MemoryStream();
                await getResponse.ResponseStream.CopyToAsync(downloadedMemStream);
                downloadedMemStream.Position = 0;

                var restoredStream = await _cryptoService.RestoreFromStorageAsync(downloadedMemStream);
                return FileDownloadResult.FromStream(restoredStream, originalFileName, finalContentType);
            }
            catch (System.Security.Cryptography.CryptographicException)
            {
                // Lỗi sai khóa hoặc tệp mã hóa bị hỏng: ném ngoại lệ, tuyệt đối không che giấu lỗi hoặc trả về ciphertext
                throw;
            }
            catch (InvalidOperationException)
            {
                // Lỗi thiếu MasterKey khi gặp tệp mã hóa: ném ngoại lệ, không fallback che giấu
                throw;
            }
            catch (InvalidDataException)
            {
                // Lỗi tệp mã hóa PSEN bị cắt cụt: ném ngoại lệ
                throw;
            }
            catch (AmazonS3Exception ex)
            {
                _logger.LogError(ex, "Lỗi truy xuất tệp từ Supabase Storage: {Key}", key);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không thể tải hoặc xử lý tệp {Key} từ Supabase.", key);
                throw;
            }
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
