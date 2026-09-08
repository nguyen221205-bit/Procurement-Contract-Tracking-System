using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ProcurementSystem.Core.Enums;

namespace ProcurementSystem.Infrastructure.Entities
{
    public class Contract
    {
        [Key]
        public int Id { get; set; }

        public int BidPackageId { get; set; }
        public int ContractorId { get; set; }

        [Required, MaxLength(50)]
        public string ContractNumber { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Value { get; set; }

        [MaxLength(3000)]
        public string? Terms { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public ContractStatus Status { get; set; } = ContractStatus.Draft;

        [MaxLength(500)]
        public string? ScannedFilePath { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        [ForeignKey("BidPackageId")]
        public virtual BidPackage BidPackage { get; set; } = null!;

        [ForeignKey("ContractorId")]
        public virtual Contractor Contractor { get; set; } = null!;

        public virtual ICollection<ContractMilestone> Milestones { get; set; } = new List<ContractMilestone>();
        public virtual ICollection<ProgressUpdate> ProgressUpdates { get; set; } = new List<ProgressUpdate>();
        public virtual ICollection<Acceptance> Acceptances { get; set; } = new List<Acceptance>();
    }
}
