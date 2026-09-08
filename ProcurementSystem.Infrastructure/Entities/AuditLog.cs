using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProcurementSystem.Infrastructure.Entities
{
    public class AuditLog
    {
        [Key]
        public int Id { get; set; }

        public int? UserId { get; set; }

        [Required, MaxLength(100)]
        public string Action { get; set; } = string.Empty; // CREATE, UPDATE, DELETE

        [Required, MaxLength(100)]
        public string EntityType { get; set; } = string.Empty; // BidPackage, Contract, etc.

        public int? EntityId { get; set; }

        [Column(TypeName = "text")]
        public string? OldValues { get; set; } // JSON

        [Column(TypeName = "text")]
        public string? NewValues { get; set; } // JSON

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        [MaxLength(50)]
        public string? IpAddress { get; set; }

        // Navigation properties
        [ForeignKey("UserId")]
        public virtual User? User { get; set; }
    }
}
