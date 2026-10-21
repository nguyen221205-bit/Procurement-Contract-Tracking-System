using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ProcurementSystem.Core.Enums;

namespace ProcurementSystem.Infrastructure.Entities
{
    public class ContractGuarantee
    {
        [Key]
        public int Id { get; set; }

        public int ContractId { get; set; }

        [Required, MaxLength(100)]
        public string GuaranteeNumber { get; set; } = string.Empty;

        [Required, MaxLength(200)]
        public string BankName { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal Percentage { get; set; }

        public DateTime EffectiveDate { get; set; }
        public DateTime ExpiryDate { get; set; }

        [MaxLength(500)]
        public string? ScannedDocumentPath { get; set; }

        public GuaranteeStatus Status { get; set; } = GuaranteeStatus.Valid;

        [MaxLength(1000)]
        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        [ForeignKey("ContractId")]
        public virtual Contract Contract { get; set; } = null!;
    }
}
