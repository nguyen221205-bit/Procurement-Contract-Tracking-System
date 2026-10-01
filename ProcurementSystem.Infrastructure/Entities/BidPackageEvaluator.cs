using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProcurementSystem.Infrastructure.Entities
{
    public class BidPackageEvaluator
    {
        public int BidPackageId { get; set; }
        public int EvaluatorId { get; set; }
        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
        public int? AssignedBy { get; set; }

        // Navigation properties
        [ForeignKey("BidPackageId")]
        public virtual BidPackage BidPackage { get; set; } = null!;

        [ForeignKey("EvaluatorId")]
        public virtual User Evaluator { get; set; } = null!;

        [ForeignKey("AssignedBy")]
        public virtual User? Assigner { get; set; }
    }
}
