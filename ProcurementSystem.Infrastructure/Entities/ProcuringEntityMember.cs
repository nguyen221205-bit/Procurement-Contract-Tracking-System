using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ProcurementSystem.Core.Enums;

namespace ProcurementSystem.Infrastructure.Entities
{
    public class ProcuringEntityMember
    {
        [Key]
        public int Id { get; set; }

        public int ProcuringEntityId { get; set; }

        public int UserId { get; set; }

        public ProcuringEntityMemberRole RoleInEntity { get; set; } = ProcuringEntityMemberRole.Officer;

        public MemberStatus Status { get; set; } = MemberStatus.Active;

        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

        public int? InvitedByUserId { get; set; }

        public DateTime? RevokedAt { get; set; }

        public int? RevokedByUserId { get; set; }

        [Timestamp]
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        // Navigation properties
        [ForeignKey("ProcuringEntityId")]
        public virtual ProcuringEntity ProcuringEntity { get; set; } = null!;

        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;

        [ForeignKey("InvitedByUserId")]
        public virtual User? InvitedByUser { get; set; }

        [ForeignKey("RevokedByUserId")]
        public virtual User? RevokedByUser { get; set; }
    }
}
