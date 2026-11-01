using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ProcurementSystem.Core.Enums;

namespace ProcurementSystem.Infrastructure.Entities
{
    public class Acceptance
    {
        [Key]
        public int Id { get; set; }

        public int ContractId { get; set; }
        public int? MilestoneId { get; set; }
        public int? ApprovedBy { get; set; }

        public DateTime? ApprovedAt { get; set; }

        public AcceptanceStatus Status { get; set; } = AcceptanceStatus.Pending;

        [MaxLength(1000)]
        public string? Note { get; set; }

        // Maker - Checker fields
        public int? TechnicalReviewedBy { get; set; }
        public DateTime? TechnicalReviewedAt { get; set; }
        [MaxLength(1000)]
        public string? TechnicalNote { get; set; }
        public bool? IsTechnicalPassed { get; set; }

        // Snapshot nội dung mốc tại thời điểm Maker xác nhận kỹ thuật
        public int MilestoneVersion { get; set; } = 1;

        [Column(TypeName = "decimal(18,2)")]
        public decimal SnapshotAmount { get; set; }

        [MaxLength(300)]
        public string SnapshotTitle { get; set; } = string.Empty;

        public DateTime? SnapshotDueDate { get; set; }

        public int Revision { get; set; } = 1;

        [MaxLength(64)]
        public string? SnapshotDocumentHash { get; set; }

        [MaxLength(500)]
        public string? SnapshotDocumentPath { get; set; }

        [Timestamp]
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        // Thẩm quyền ký duyệt được áp dụng
        public int? SigningAuthorityId { get; set; }

        // Navigation properties
        [ForeignKey("ContractId")]
        public virtual Contract Contract { get; set; } = null!;

        [ForeignKey("MilestoneId")]
        public virtual ContractMilestone? Milestone { get; set; }

        [ForeignKey("ApprovedBy")]
        public virtual User? Approver { get; set; }

        [ForeignKey("TechnicalReviewedBy")]
        public virtual User? TechnicalReviewer { get; set; }

        [ForeignKey("SigningAuthorityId")]
        public virtual SigningAuthority? AppliedSigningAuthority { get; set; }
    }
}
