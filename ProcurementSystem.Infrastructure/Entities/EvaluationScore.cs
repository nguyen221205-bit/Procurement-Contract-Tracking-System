using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProcurementSystem.Infrastructure.Entities
{
    public class EvaluationScore
    {
        [Key]
        public int Id { get; set; }

        public int BidSubmissionId { get; set; }
        public int CriteriaId { get; set; }
        public int EvaluatorId { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal Score { get; set; }

        [MaxLength(500)]
        public string? Comment { get; set; }

        public DateTime ScoredAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("BidSubmissionId")]
        public virtual BidSubmission BidSubmission { get; set; } = null!;

        [ForeignKey("CriteriaId")]
        public virtual EvaluationCriteria Criteria { get; set; } = null!;

        [ForeignKey("EvaluatorId")]
        public virtual User Evaluator { get; set; } = null!;
    }
}
