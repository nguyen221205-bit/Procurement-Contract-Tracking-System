using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ProcurementSystem.Core.Enums;

namespace ProcurementSystem.Infrastructure.Entities
{
    public class BidSubmission
    {
        [Key]
        public int Id { get; set; }

        public int BidPackageId { get; set; }
        public int ContractorId { get; set; }

        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

        [Column(TypeName = "decimal(10,2)")]
        public decimal? TotalScore { get; set; }

        public int? Rank { get; set; }

        [MaxLength(50)]
        public string Status { get; set; } = "Submitted"; // Submitted, Evaluated, Selected, Rejected

        // Navigation properties
        [ForeignKey("BidPackageId")]
        public virtual BidPackage BidPackage { get; set; } = null!;

        [ForeignKey("ContractorId")]
        public virtual Contractor Contractor { get; set; } = null!;

        public virtual ICollection<SubmissionFile> SubmissionFiles { get; set; } = new List<SubmissionFile>();
        public virtual ICollection<EvaluationScore> EvaluationScores { get; set; } = new List<EvaluationScore>();
    }
}
