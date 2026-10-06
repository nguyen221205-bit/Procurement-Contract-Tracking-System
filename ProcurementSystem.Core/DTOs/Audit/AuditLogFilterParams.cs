namespace ProcurementSystem.Core.DTOs.Audit
{
    /// <summary>
    /// Bộ lọc tra cứu lịch sử kiểm toán Audit Log
    /// </summary>
    public class AuditLogFilterParams
    {
        private const int MaxPageSize = 100;
        private int _pageSize = 20;

        public int PageIndex { get; set; } = 1;

        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = value > MaxPageSize ? MaxPageSize : (value < 1 ? 20 : value);
        }

        public string? Action { get; set; }
        public string? EntityType { get; set; }
        public int? EntityId { get; set; }
        public int? UserId { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }
}
