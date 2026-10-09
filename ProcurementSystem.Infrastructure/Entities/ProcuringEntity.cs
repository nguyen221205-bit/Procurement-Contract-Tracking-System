using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProcurementSystem.Infrastructure.Entities
{
    public class ProcuringEntity
    {
        [Key]
        public int Id { get; set; }

        public int UserId { get; set; }

        [Required, MaxLength(250)]
        public string OrganizationName { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string OrganizationType { get; set; } = string.Empty; // CoQuanHanhChinh, DonViSuNghiep, BanQLDA, DoanhNghiep, Khac

        [Required, MaxLength(20)]
        public string TaxCode { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? BudgetCode { get; set; } // Mã Đơn vị có quan hệ với ngân sách (Mã ĐVQHNS)

        [MaxLength(500)]
        public string? Address { get; set; }

        [Required, MaxLength(100)]
        public string RepresentativeName { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string RepresentativeTitle { get; set; } = string.Empty; // Giám đốc, Trưởng ban, v.v.

        [MaxLength(20)]
        public string? RepresentativePhone { get; set; }

        [Required, MaxLength(500)]
        public string EstablishmentDecisionFile { get; set; } = string.Empty; // File scan Quyết định thành lập / ĐKKD

        [MaxLength(500)]
        public string? AppointmentDecisionFile { get; set; } // File Quyết định bổ nhiệm (nếu có)

        [MaxLength(50)]
        public string VerificationStatus { get; set; } = "Pending"; // Pending, Approved, Rejected

        [MaxLength(1000)]
        public string? AdminNotes { get; set; }

        public int? ReviewedByUserId { get; set; }
        public DateTime? ReviewedAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;

        [ForeignKey("ReviewedByUserId")]
        public virtual User? ReviewedByUser { get; set; }
    }
}
