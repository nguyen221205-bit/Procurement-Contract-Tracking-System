namespace ProcurementSystem.Core.DTOs.Common
{
    public class StorageSettings
    {
        public const string SectionName = "StorageSettings";

        /// <summary>
        /// Nhà cung cấp lưu trữ: "Local" hoặc "Supabase"
        /// </summary>
        public string Provider { get; set; } = "Local";

        /// <summary>
        /// Cấu hình kết nối Supabase S3
        /// </summary>
        public SupabaseStorageConfig Supabase { get; set; } = new();

        /// <summary>
        /// Cấu hình Nén và Mã hóa bảo mật tài liệu khi lưu trên Cloud
        /// </summary>
        public StorageEncryptionConfig Encryption { get; set; } = new();
    }

    public class StorageEncryptionConfig
    {
        /// <summary>
        /// Bật/Tắt tính năng nén GZip và mã hóa AES-256 trước khi đẩy lên Cloud
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Khóa mã hóa chính 256-bit (32 bytes Base64)
        /// </summary>
        public string MasterKey { get; set; } = string.Empty;
    }

    public class SupabaseStorageConfig
    {
        /// <summary>
        /// Endpoint S3 của Supabase, định dạng: https://{project-ref}.storage.supabase.co/v1/s3
        /// </summary>
        public string Endpoint { get; set; } = string.Empty;

        /// <summary>
        /// Tên Private Bucket lưu trữ tài liệu
        /// </summary>
        public string BucketName { get; set; } = "procurement-documents";

        /// <summary>
        /// S3 Access Key ID (lấy từ Supabase Project Settings -> Storage -> S3 Access Keys)
        /// </summary>
        public string AccessKeyId { get; set; } = string.Empty;

        /// <summary>
        /// S3 Secret Access Key
        /// </summary>
        public string SecretAccessKey { get; set; } = string.Empty;

        /// <summary>
        /// Khu vực (Region) của Project Supabase (mặc định ap-southeast-1)
        /// </summary>
        public string Region { get; set; } = "ap-southeast-1";
    }
}
