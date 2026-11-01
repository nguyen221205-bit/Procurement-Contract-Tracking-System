using ProcurementSystem.Core.Enums;

namespace ProcurementSystem.Core.DTOs.Contract
{
    /// <summary>
    /// Kết quả phê duyệt hoặc từ chối nghiệm thu một mốc thanh toán.
    /// </summary>
    public class MilestoneAcceptanceDto
    {
        /// <summary>Mã bản ghi nghiệm thu.</summary>
        public int Id { get; set; }
        /// <summary>Mã hợp đồng được nghiệm thu.</summary>
        public int ContractId { get; set; }
        /// <summary>Mã mốc thanh toán được nghiệm thu.</summary>
        public int? MilestoneId { get; set; }

        // Maker fields (Xác nhận kỹ thuật)
        /// <summary>Mã cán bộ xác nhận kỹ thuật (Maker).</summary>
        public int? TechnicalReviewedBy { get; set; }
        /// <summary>Tên cán bộ xác nhận kỹ thuật.</summary>
        public string? TechnicalReviewerName { get; set; }
        /// <summary>Thời điểm xác nhận kỹ thuật.</summary>
        public DateTime? TechnicalReviewedAt { get; set; }
        /// <summary>Ghi chú kết quả kiểm tra kỹ thuật.</summary>
        public string? TechnicalNote { get; set; }
        /// <summary>Kỹ thuật đạt hay không đạt.</summary>
        public bool? IsTechnicalPassed { get; set; }

        // Snapshot nội dung mốc tại thời điểm Maker duyệt
        public int MilestoneVersion { get; set; } = 1;
        public decimal SnapshotAmount { get; set; }
        public string SnapshotTitle { get; set; } = string.Empty;
        public DateTime? SnapshotDueDate { get; set; }
        public int Revision { get; set; } = 1;
        public string? SnapshotDocumentHash { get; set; }
        public string? SnapshotDocumentPath { get; set; }

        // Thẩm quyền ký duyệt được áp dụng
        public int? SigningAuthorityId { get; set; }
        public string? SigningAuthorityDecisionNumber { get; set; }

        // Checker fields (Phê duyệt giải ngân)
        /// <summary>Mã người phê duyệt nghiệm thu (Checker).</summary>
        public int? ApprovedBy { get; set; }
        /// <summary>Tên người phê duyệt nghiệm thu.</summary>
        public string? ApproverName { get; set; }
        /// <summary>Thời điểm phê duyệt hoặc từ chối.</summary>
        public DateTime? ApprovedAt { get; set; }
        /// <summary>Trạng thái nghiệm thu dạng enum.</summary>
        public AcceptanceStatus Status { get; set; }
        /// <summary>Tên trạng thái nghiệm thu.</summary>
        public string StatusName { get; set; } = string.Empty;
        /// <summary>Ghi chú của người phê duyệt.</summary>
        public string? Note { get; set; }
        /// <summary>Trạng thái hiện tại của mốc thanh toán.</summary>
        public MilestoneStatus MilestoneStatus { get; set; }
        /// <summary>Tên trạng thái hiện tại của mốc thanh toán.</summary>
        public string MilestoneStatusName { get; set; } = string.Empty;
        /// <summary>Trạng thái hợp đồng sau khi xử lý nghiệm thu.</summary>
        public ContractStatus ContractStatus { get; set; }
        /// <summary>Tên trạng thái hợp đồng sau khi xử lý nghiệm thu.</summary>
        public string ContractStatusName { get; set; } = string.Empty;
        /// <summary>Tổng tiền đã giải ngân của hợp đồng.</summary>
        public decimal TotalDisbursedAmount { get; set; }
        /// <summary>Số tiền còn lại chưa giải ngân của hợp đồng.</summary>
        public decimal TotalRemainingAmount { get; set; }
    }
}
