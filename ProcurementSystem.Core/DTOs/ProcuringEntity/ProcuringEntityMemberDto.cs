using ProcurementSystem.Core.Enums;

namespace ProcurementSystem.Core.DTOs.ProcuringEntity
{
    public class ProcuringEntityMemberDto
    {
        public int Id { get; set; }
        public int ProcuringEntityId { get; set; }
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public ProcuringEntityMemberRole RoleInEntity { get; set; }
        public string RoleInEntityName { get; set; } = string.Empty;
        public MemberStatus Status { get; set; }
        public string StatusName { get; set; } = string.Empty;
        public DateTime JoinedAt { get; set; }
        public int? InvitedByUserId { get; set; }
        public string? InvitedByName { get; set; }
        public DateTime? RevokedAt { get; set; }
        public int? RevokedByUserId { get; set; }
        public string? RevokedByName { get; set; }
    }
}
