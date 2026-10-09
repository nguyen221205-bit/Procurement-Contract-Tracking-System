namespace ProcurementSystem.Core.DTOs.Audit
{
    /// <summary>
    /// Thông tin lịch sử kiểm toán hoạt động hệ thống (Audit Log)
    /// </summary>
    public class AuditLogDto
    {
        public int Id { get; set; }
        public int? UserId { get; set; }
        public string? UserFullName { get; set; }
        public string? UserEmail { get; set; }
        public string Action { get; set; } = string.Empty;
        public string EntityType { get; set; } = string.Empty;
        public int? EntityId { get; set; }
        public string? OldValues { get; set; }
        public string? NewValues { get; set; }
        public DateTime Timestamp { get; set; }
        public string? IpAddress { get; set; }
    }
}
