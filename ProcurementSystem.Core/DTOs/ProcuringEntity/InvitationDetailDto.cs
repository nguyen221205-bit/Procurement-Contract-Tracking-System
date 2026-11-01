using ProcurementSystem.Core.Enums;

namespace ProcurementSystem.Core.DTOs.ProcuringEntity
{
    public class InvitationDetailDto
    {
        public string Token { get; set; } = string.Empty;
        public int ProcuringEntityId { get; set; }
        public string OrganizationName { get; set; } = string.Empty;
        public string TaxCode { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public ProcuringEntityMemberRole RoleInEntity { get; set; }
        public string RoleInEntityName { get; set; } = string.Empty;
        public string InvitedByName { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public bool IsExpired { get; set; }
        public bool HasExistingAccount { get; set; }
    }
}
