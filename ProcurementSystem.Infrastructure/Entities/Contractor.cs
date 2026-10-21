using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProcurementSystem.Infrastructure.Entities
{
    public class Contractor
    {
        [Key]
        public int Id { get; set; }

        public int UserId { get; set; }

        [Required, MaxLength(200)]
        public string CompanyName { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? TaxCode { get; set; }

        [MaxLength(500)]
        public string? Address { get; set; }

        [MaxLength(500)]
        public string? BusinessLicenseFile { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal Rating { get; set; } = 0;

        [MaxLength(50)]
        public string VerificationStatus { get; set; } = "Approved"; // Pending, Approved, Rejected

        // Thông tin Chữ ký số điện tử (USB Token / HSM PKCS#7 qua iText7)
        public bool IsDigitalSigned { get; set; } = false;

        [MaxLength(250)]
        public string? DigitalSignerName { get; set; }

        public DateTime? DigitalSignDate { get; set; }

        [MaxLength(500)]
        public string? DigitalSignMessage { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;
        public virtual ICollection<BidSubmission> BidSubmissions { get; set; } = new List<BidSubmission>();
        public virtual ICollection<Contract> Contracts { get; set; } = new List<Contract>();
        public virtual ICollection<ContractorEvaluation> Evaluations { get; set; } = new List<ContractorEvaluation>();
    }
}
