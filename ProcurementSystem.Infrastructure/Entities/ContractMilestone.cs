using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ProcurementSystem.Core.Enums;

namespace ProcurementSystem.Infrastructure.Entities
{
    public class ContractMilestone
    {
        [Key]
        public int Id { get; set; }

        public int ContractId { get; set; }

        [Required, MaxLength(300)]
        public string Title { get; set; } = string.Empty;

        public DateTime DueDate { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        public MilestoneStatus Status { get; set; } = MilestoneStatus.Pending;

        // Navigation properties
        [ForeignKey("ContractId")]
        public virtual Contract Contract { get; set; } = null!;
        public virtual ICollection<Acceptance> Acceptances { get; set; } = new List<Acceptance>();
    }
}
