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

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;
        public virtual ICollection<BidSubmission> BidSubmissions { get; set; } = new List<BidSubmission>();
        public virtual ICollection<Contract> Contracts { get; set; } = new List<Contract>();
    }
}
