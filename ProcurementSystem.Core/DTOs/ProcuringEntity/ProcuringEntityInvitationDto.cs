using ProcurementSystem.Core.Enums;

namespace ProcurementSystem.Core.DTOs.ProcuringEntity
{
    public class ProcuringEntityInvitationDto
    {
        public int Id { get; set; }
        public int ProcuringEntityId { get; set; }
        public string OrganizationName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public ProcuringEntityMemberRole RoleInEntity { get; set; }
        public string RoleInEntityName { get; set; } = string.Empty;
        public int InvitedByUserId { get; set; }
        public string InvitedByName { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public InvitationStatus Status { get; set; }
        public string StatusName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? AcceptedAt { get; set; }
        public int? AcceptedByUserId { get; set; }
        public string? AcceptedByName { get; set; }
        public bool IsExpired => DateTime.UtcNow > ExpiresAt;
    }
}
