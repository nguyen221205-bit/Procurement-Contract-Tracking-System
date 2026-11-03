namespace ProcurementSystem.Core.DTOs.Common
{
    public class StorageSyncReportDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int TotalFilesDiscovered { get; set; }
        public int UploadedFilesCount { get; set; }
        public int DatabaseRecordsUpdated { get; set; }
        public int FailedFilesCount { get; set; }
        public string BackupTableName { get; set; } = string.Empty;
        public List<string> Details { get; set; } = new();
    }

    public class StorageStatusDto
    {
        public string ActiveProvider { get; set; } = "Local";
        public bool IsSupabaseConfigured { get; set; }
        public string SupabaseEndpoint { get; set; } = string.Empty;
        public string SupabaseBucket { get; set; } = string.Empty;
        public long LocalUploadsSizeBytes { get; set; }
        public int LocalFilesCount { get; set; }
    }
}
