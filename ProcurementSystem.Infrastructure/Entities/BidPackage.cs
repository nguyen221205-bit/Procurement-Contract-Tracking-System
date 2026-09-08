using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ProcurementSystem.Core.Enums;

namespace ProcurementSystem.Infrastructure.Entities
{
    public class BidPackage
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(50)]
        public string Code { get; set; } = string.Empty;

        [Required, MaxLength(300)]
        public string Name { get; set; } = string.Empty;

        public BidPackageType Type { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Budget { get; set; }

        public DateTime Deadline { get; set; }

        public BidPackageStatus Status { get; set; } = BidPackageStatus.Open;

        [MaxLength(2000)]
        public string? Description { get; set; }

        public int CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        [ForeignKey("CreatedBy")]
        public virtual User Creator { get; set; } = null!;
        public virtual ICollection<BidDocument> BidDocuments { get; set; } = new List<BidDocument>();
        public virtual ICollection<BidSubmission> BidSubmissions { get; set; } = new List<BidSubmission>();
        public virtual ICollection<EvaluationCriteria> EvaluationCriteria { get; set; } = new List<EvaluationCriteria>();
        public virtual Contract? Contract { get; set; }
    }
}
