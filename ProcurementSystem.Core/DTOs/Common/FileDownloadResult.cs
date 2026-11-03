namespace ProcurementSystem.Core.DTOs.Common
{
    /// <summary>
    /// Kết quả giải quyết tải tệp an toàn (hỗ trợ Local Physical File, Cloud Decrypted Stream, và Pre-signed URL Redirect)
    /// </summary>
    public class FileDownloadResult : IDisposable
    {
        public bool IsRedirect { get; set; }
        public string? RedirectUrl { get; set; }
        public string? PhysicalPath { get; set; }
        public Stream? FileStream { get; set; }
        public bool IsStream => FileStream != null;
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = "application/octet-stream";

        public static FileDownloadResult FromRedirect(string redirectUrl, string fileName, string contentType = "application/octet-stream")
        {
            return new FileDownloadResult
            {
                IsRedirect = true,
                RedirectUrl = redirectUrl,
                FileName = fileName,
                ContentType = contentType
            };
        }

        public static FileDownloadResult FromPhysicalPath(string physicalPath, string fileName, string contentType = "application/octet-stream")
        {
            return new FileDownloadResult
            {
                IsRedirect = false,
                PhysicalPath = physicalPath,
                FileName = fileName,
                ContentType = contentType
            };
        }

        public static FileDownloadResult FromStream(Stream stream, string fileName, string contentType = "application/octet-stream")
        {
            return new FileDownloadResult
            {
                IsRedirect = false,
                FileStream = stream,
                FileName = fileName,
                ContentType = contentType
            };
        }

        public void Dispose()
        {
            FileStream?.Dispose();
        }
    }
}
