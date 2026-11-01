using ProcurementSystem.Core.Enums;

namespace ProcurementSystem.Core.DTOs.Contract
{
    public class ContractMilestoneDto
    {
        public int Id { get; set; }
        public int ContractId { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime DueDate { get; set; }
        public decimal Amount { get; set; }
        public MilestoneStatus Status { get; set; }
        public string StatusName { get; set; } = string.Empty;
        public int Version { get; set; } = 1;

        // Acceptance Maker - Checker details
        public int? AcceptanceId { get; set; }
        public AcceptanceStatus? AcceptanceStatus { get; set; }
        public string? AcceptanceStatusName { get; set; }
        public int? TechnicalReviewedBy { get; set; }
        public string? TechnicalReviewerName { get; set; }
        public DateTime? TechnicalReviewedAt { get; set; }
        public string? TechnicalNote { get; set; }
        public bool? IsTechnicalPassed { get; set; }

        // Snapshot nội dung mốc tại thời điểm Maker duyệt
        public int? MilestoneVersion { get; set; }
        public decimal? SnapshotAmount { get; set; }
        public string? SnapshotTitle { get; set; }
        public DateTime? SnapshotDueDate { get; set; }
        public int? AcceptanceRevision { get; set; }
        public string? SnapshotDocumentHash { get; set; }
        public string? SnapshotDocumentPath { get; set; }

        // Thẩm quyền ký duyệt được áp dụng
        public int? SigningAuthorityId { get; set; }
        public string? SigningAuthorityDecisionNumber { get; set; }

        public int? ApprovedBy { get; set; }
        public string? ApproverName { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public string? AcceptanceNote { get; set; }
    }
}
