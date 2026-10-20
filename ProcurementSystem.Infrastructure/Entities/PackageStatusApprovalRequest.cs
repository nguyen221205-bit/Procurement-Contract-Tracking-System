using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ProcurementSystem.Core.Enums;

namespace ProcurementSystem.Infrastructure.Entities
{
    /// <summary>
    /// Bản ghi yêu cầu phê duyệt thay đổi trạng thái gói thầu (Đóng thầu sớm, Mở lại/Gia hạn, Hủy thầu)
    /// </summary>
    public class PackageStatusApprovalRequest
    {
        [Key]
        public int Id { get; set; }

        public int BidPackageId { get; set; }

        public int RequestedByUserId { get; set; } // Người gửi yêu cầu (Bên mời thầu - Procurement)

        public BidPackageStatus CurrentStatus { get; set; }

        public BidPackageStatus TargetStatus { get; set; }

        [Required, MaxLength(1000)]
        public string Reason { get; set; } = string.Empty; // Lý do / Căn cứ phê duyệt

        public DateTime? NewDeadline { get; set; } // Hạn nộp mới nếu mở lại / gia hạn thầu

        public ApprovalStatus Status { get; set; } = ApprovalStatus.Pending; // Pending, Approved, Rejected

        [MaxLength(1000)]
        public string? AdminComment { get; set; } // Nhận xét hoặc lý do từ chối của Chủ đầu tư / Admin

        public int? ReviewedByUserId { get; set; } // Admin phê duyệt

        public DateTime? ReviewedAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("BidPackageId")]
        public virtual BidPackage BidPackage { get; set; } = null!;

        [ForeignKey("RequestedByUserId")]
        public virtual User RequestedByUser { get; set; } = null!;

        [ForeignKey("ReviewedByUserId")]
        public virtual User? ReviewedByUser { get; set; }
    }
}
