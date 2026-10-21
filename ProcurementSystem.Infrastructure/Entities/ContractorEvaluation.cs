using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProcurementSystem.Infrastructure.Entities
{
    public class ContractorEvaluation
    {
        [Key]
        public int Id { get; set; }

        public int ContractId { get; set; }
        public int ContractorId { get; set; }
        public int EvaluatorUserId { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal QualityScore { get; set; } // 0 - 40 điểm

        [Column(TypeName = "decimal(5,2)")]
        public decimal ScheduleScore { get; set; } // 0 - 40 điểm

        [Column(TypeName = "decimal(5,2)")]
        public decimal ComplianceScore { get; set; } // 0 - 20 điểm

        [Column(TypeName = "decimal(5,2)")]
        public decimal TotalScore { get; set; } // 0 - 100 điểm

        [Required, MaxLength(10)]
        public string RatingGrade { get; set; } = "B"; // A, B, C, D

        [MaxLength(2000)]
        public string? Comments { get; set; }

        public DateTime EvaluatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("ContractId")]
        public virtual Contract Contract { get; set; } = null!;

        [ForeignKey("ContractorId")]
        public virtual Contractor Contractor { get; set; } = null!;

        [ForeignKey("EvaluatorUserId")]
        public virtual User EvaluatorUser { get; set; } = null!;
    }
}
