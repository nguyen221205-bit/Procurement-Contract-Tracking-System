using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ProcurementSystem.Core.Enums;

namespace ProcurementSystem.Infrastructure.Entities
{
    /// <summary>
    /// Bản ghi Làm rõ Hồ sơ mời thầu (HSMT) hoặc Hồ sơ dự thầu (HSDT)
    /// </summary>
    public class BidClarification
    {
        [Key]
        public int Id { get; set; }

        public int BidPackageId { get; set; }

        public int? BidSubmissionId { get; set; } // Null nếu làm rõ HSMT; có ID nếu làm rõ HSDT cụ thể

        public ClarificationType Type { get; set; } // 0: HSMT (Công khai), 1: HSDT (Riêng tư)

        [Required, MaxLength(250)]
        public string Title { get; set; } = string.Empty;

        [Required, MaxLength(3000)]
        public string QuestionContent { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? QuestionAttachmentUrl { get; set; }

        public int AskedByUserId { get; set; }

        public DateTime AskedAt { get; set; } = DateTime.UtcNow;

        public DateTime? Deadline { get; set; } // Hạn chót giải trình (áp dụng cho làm rõ HSDT)

        [MaxLength(3000)]
        public string? AnswerContent { get; set; }

        [MaxLength(500)]
        public string? AnswerAttachmentUrl { get; set; }

        public int? AnsweredByUserId { get; set; }

        public DateTime? AnsweredAt { get; set; }

        public ClarificationStatus Status { get; set; } = ClarificationStatus.Pending;

        // Navigation properties
        [ForeignKey("BidPackageId")]
        public virtual BidPackage BidPackage { get; set; } = null!;

        [ForeignKey("BidSubmissionId")]
        public virtual BidSubmission? BidSubmission { get; set; }

        [ForeignKey("AskedByUserId")]
        public virtual User AskedByUser { get; set; } = null!;

        [ForeignKey("AnsweredByUserId")]
        public virtual User? AnsweredByUser { get; set; }
    }
}
