using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProcurementSystem.Infrastructure.Entities
{
    public class ProgressUpdate
    {
        [Key]
        public int Id { get; set; }

        public int ContractId { get; set; }

        public int WeekNumber { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal CompletionPercent { get; set; }

        [MaxLength(2000)]
        public string? Note { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("ContractId")]
        public virtual Contract Contract { get; set; } = null!;
    }
}
