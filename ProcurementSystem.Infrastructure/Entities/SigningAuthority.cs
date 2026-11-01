using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ProcurementSystem.Core.Enums;

namespace ProcurementSystem.Infrastructure.Entities
{
    public class SigningAuthority
    {
        [Key]
        public int Id { get; set; }

        public int ProcuringEntityId { get; set; }
        public int UserId { get; set; }

        public SigningAuthorityType AuthorityType { get; set; } = SigningAuthorityType.DisbursementApproval;

        [Required, MaxLength(100)]
        public string Scope { get; set; } = "EntityWide"; // "EntityWide" hoặc "Contract:{Id}"

        public DateTime ValidFrom { get; set; }
        public DateTime ValidTo { get; set; }

        public SigningAuthorityStatus Status { get; set; } = SigningAuthorityStatus.PendingVerification;

        [Required, MaxLength(100)]
        public string DecisionNumber { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? DocumentUrl { get; set; }

        [MaxLength(500)]
        public string LegalBasis { get; set; } = string.Empty;

        public int RequestedByUserId { get; set; }
        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

        public int? VerifiedByUserId { get; set; }
        public DateTime? VerifiedAt { get; set; }

        [MaxLength(1000)]
        public string? VerificationNotes { get; set; }

        public int? RevokedByUserId { get; set; }
        public DateTime? RevokedAt { get; set; }

        [MaxLength(500)]
        public string? RevocationReason { get; set; }

        [Timestamp]
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        // Navigation
        [ForeignKey("ProcuringEntityId")]
        public virtual ProcuringEntity ProcuringEntity { get; set; } = null!;

        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;

        [ForeignKey("RequestedByUserId")]
        public virtual User RequestedByUser { get; set; } = null!;

        [ForeignKey("VerifiedByUserId")]
        public virtual User? VerifiedByUser { get; set; }

        [ForeignKey("RevokedByUserId")]
        public virtual User? RevokedByUser { get; set; }
    }
}
