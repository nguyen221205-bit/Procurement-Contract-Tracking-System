namespace ProcurementSystem.Infrastructure.Utilities
{
    /// <summary>
    /// Tiện ích phân giải kiểu nội dung MIME (Content-Type) tập trung dựa trên bảng tra cứu (Lookup Table - tuân thủ SRP & OCP).
    /// Loại bỏ hoàn toàn các khối switch-case phân giải phần mở rộng tệp trùng lặp rải rác ở các Service.
    /// </summary>
    public static class ContentTypeResolver
    {
        private static readonly IReadOnlyDictionary<string, string> MimeMappings =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [".pdf"] = "application/pdf",
                [".doc"] = "application/msword",
                [".docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                [".xls"] = "application/vnd.ms-excel",
                [".xlsx"] = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                [".csv"] = "text/csv",
                [".png"] = "image/png",
                [".jpg"] = "image/jpeg",
                [".jpeg"] = "image/jpeg",
                [".gif"] = "image/gif",
                [".webp"] = "image/webp",
                [".zip"] = "application/zip",
                [".rar"] = "application/x-rar-compressed",
                [".7z"] = "application/x-7z-compressed",
                [".txt"] = "text/plain"
            };

        /// <summary>
        /// Phân giải Content-Type chuẩn từ tên tệp hoặc đường dẫn lưu trữ.
        /// </summary>
        public static string ResolveByFileName(string? fileNameOrPath, string defaultContentType = "application/octet-stream")
        {
            if (string.IsNullOrWhiteSpace(fileNameOrPath))
            {
                return defaultContentType;
            }

            var ext = Path.GetExtension(fileNameOrPath);
            if (string.IsNullOrWhiteSpace(ext))
            {
                return defaultContentType;
            }

            return MimeMappings.TryGetValue(ext, out var contentType)
                ? contentType
                : defaultContentType;
        }
    }
}
