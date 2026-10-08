using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProcurementSystem.Infrastructure.Entities
{
    public class EvaluatorProposal
    {
        [Key]
        public int Id { get; set; }

        public int ProposerUserId { get; set; } // Người gửi đề xuất (Bên mời thầu - Procurement)

        [Required, MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required, MaxLength(150)]
        public string Email { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? Phone { get; set; }

        [MaxLength(200)]
        public string? Specialization { get; set; } // Chuyên môn: Kỹ thuật CNTT, Xây dựng, Tài chính, Pháp lý...

        [MaxLength(250)]
        public string? Workplace { get; set; } // Cơ quan / Đơn vị công tác / Chức vụ

        public int? ExperienceYears { get; set; } // Số năm kinh nghiệm công tác

        [MaxLength(200)]
        public string? CertificateName { get; set; } // Tên chứng chỉ chuyên môn / Chứng chỉ nghiệp vụ đấu thầu

        [MaxLength(500)]
        public string? CertificateFile { get; set; } // Đường dẫn lưu file chứng chỉ

        [MaxLength(1000)]
        public string? Notes { get; set; } // Lý do đề xuất của Bên mời thầu

        [MaxLength(50)]
        public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected

        [MaxLength(1000)]
        public string? AdminNotes { get; set; } // Ghi chú hoặc lý do từ chối của Admin

        public int? ReviewedByUserId { get; set; } // Admin đã xử lý
        public DateTime? ReviewedAt { get; set; }

        public int? CreatedUserId { get; set; } // Tài khoản User được tạo ra sau khi duyệt

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("ProposerUserId")]
        public virtual User ProposerUser { get; set; } = null!;

        [ForeignKey("ReviewedByUserId")]
        public virtual User? ReviewedByUser { get; set; }

        [ForeignKey("CreatedUserId")]
        public virtual User? CreatedUser { get; set; }
    }
}
