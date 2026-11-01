using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ProcurementSystem.Core.Enums;

namespace ProcurementSystem.Infrastructure.Entities
{
    public class ProcuringEntityInvitation
    {
        [Key]
        public int Id { get; set; }

        public int ProcuringEntityId { get; set; }

        [Required, MaxLength(150)]
        public string Email { get; set; } = string.Empty;

        public ProcuringEntityMemberRole RoleInEntity { get; set; } = ProcuringEntityMemberRole.Officer;

        [Required, MaxLength(128)]
        public string TokenHash { get; set; } = string.Empty; // SHA-256 hash của raw token

        public int InvitedByUserId { get; set; }

        public DateTime ExpiresAt { get; set; }

        public InvitationStatus Status { get; set; } = InvitationStatus.Pending;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? AcceptedAt { get; set; }

        public int? AcceptedByUserId { get; set; }

        // Navigation properties
        [ForeignKey("ProcuringEntityId")]
        public virtual ProcuringEntity ProcuringEntity { get; set; } = null!;

        [ForeignKey("InvitedByUserId")]
        public virtual User InvitedByUser { get; set; } = null!;

        [ForeignKey("AcceptedByUserId")]
        public virtual User? AcceptedByUser { get; set; }
    }
}
