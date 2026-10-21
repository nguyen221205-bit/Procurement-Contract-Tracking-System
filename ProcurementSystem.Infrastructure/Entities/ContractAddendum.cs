using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ProcurementSystem.Core.Enums;

namespace ProcurementSystem.Infrastructure.Entities
{
    public class ContractAddendum
    {
        [Key]
        public int Id { get; set; }

        public int ContractId { get; set; }

        [Required, MaxLength(100)]
        public string AddendumNumber { get; set; } = string.Empty;

        [Required, MaxLength(300)]
        public string Title { get; set; } = string.Empty;

        public AddendumType Type { get; set; } = AddendumType.TimelineExtension;

        [Column(TypeName = "decimal(18,2)")]
        public decimal AdditionalValue { get; set; } = 0;

        public DateTime? AdjustedEndDate { get; set; }

        public int ExtensionDays { get; set; } = 0;

        [MaxLength(2000)]
        public string? Reason { get; set; }

        public DateTime SignedDate { get; set; }

        [MaxLength(500)]
        public string? ScannedDocumentPath { get; set; }

        public AddendumStatus Status { get; set; } = AddendumStatus.Approved;

        public int CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int? ApprovedBy { get; set; }
        public DateTime? ApprovedAt { get; set; }

        // Navigation properties
        [ForeignKey("ContractId")]
        public virtual Contract Contract { get; set; } = null!;

        [ForeignKey("CreatedBy")]
        public virtual User Creator { get; set; } = null!;

        [ForeignKey("ApprovedBy")]
        public virtual User? Approver { get; set; }
    }
}
