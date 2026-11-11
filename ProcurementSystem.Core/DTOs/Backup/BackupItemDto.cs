namespace ProcurementSystem.Core.DTOs.Backup
{
    public class BackupItemDto
    {
        public string FileName { get; set; } = string.Empty;
        public string BackupType { get; set; } = string.Empty; // "Database" | "DocumentsZip"
        public long FileSizeBytes { get; set; }
        public string FileSizeFormatted { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string Sha256Hash { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    public class CreateBackupRequest
    {
        public string? Description { get; set; }
        public string BackupType { get; set; } = "Database"; // "Database" | "DocumentsZip"
    }

    public class RestoreBackupRequest
    {
        public string FileName { get; set; } = string.Empty;
        public string ConfirmPhrase { get; set; } = string.Empty; // Phải là "RESTORE_DATABASE_CONFIRM"
    }

    public class BackupOverviewDto
    {
        public int TotalBackups { get; set; }
        public long TotalSizeBytes { get; set; }
        public string TotalSizeFormatted { get; set; } = string.Empty;
        public DateTime? LastBackupAt { get; set; }
        public List<BackupItemDto> DatabaseBackups { get; set; } = new();
        public List<BackupItemDto> DocumentBackups { get; set; } = new();
    }
}
