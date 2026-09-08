using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProcurementSystem.Infrastructure.Entities
{
    public class EvaluationCriteria
    {
        [Key]
        public int Id { get; set; }

        public int BidPackageId { get; set; }

        [Required, MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [Column(TypeName = "decimal(5,2)")]
        public decimal MaxScore { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal Weight { get; set; } = 1;

        // Navigation properties
        [ForeignKey("BidPackageId")]
        public virtual BidPackage BidPackage { get; set; } = null!;
        public virtual ICollection<EvaluationScore> EvaluationScores { get; set; } = new List<EvaluationScore>();
    }
}
