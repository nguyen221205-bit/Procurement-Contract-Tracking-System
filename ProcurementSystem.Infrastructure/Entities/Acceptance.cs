using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ProcurementSystem.Core.Enums;

namespace ProcurementSystem.Infrastructure.Entities
{
    public class Acceptance
    {
        [Key]
        public int Id { get; set; }

        public int ContractId { get; set; }
        public int? MilestoneId { get; set; }
        public int ApprovedBy { get; set; }

        public DateTime? ApprovedAt { get; set; }

        public AcceptanceStatus Status { get; set; } = AcceptanceStatus.Pending;

        [MaxLength(1000)]
        public string? Note { get; set; }

        // Navigation properties
        [ForeignKey("ContractId")]
        public virtual Contract Contract { get; set; } = null!;

        [ForeignKey("MilestoneId")]
        public virtual ContractMilestone? Milestone { get; set; }

        [ForeignKey("ApprovedBy")]
        public virtual User Approver { get; set; } = null!;
    }
}
