using ProcurementSystem.Core.Enums;

namespace ProcurementSystem.Core.DTOs.SigningAuthority
{
    public class SigningAuthorityDto
    {
        public int Id { get; set; }
        public int ProcuringEntityId { get; set; }
        public string ProcuringEntityName { get; set; } = string.Empty;
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public SigningAuthorityType AuthorityType { get; set; }
        public string AuthorityTypeName { get; set; } = string.Empty;
        public string Scope { get; set; } = "EntityWide";
        public DateTime ValidFrom { get; set; }
        public DateTime ValidTo { get; set; }
        public SigningAuthorityStatus Status { get; set; }
        public string StatusName { get; set; } = string.Empty;
        public string DecisionNumber { get; set; } = string.Empty;
        public string? DocumentUrl { get; set; }
        public string LegalBasis { get; set; } = string.Empty;
        public int RequestedByUserId { get; set; }
        public string RequestedByName { get; set; } = string.Empty;
        public DateTime RequestedAt { get; set; }
        public int? VerifiedByUserId { get; set; }
        public string? VerifiedByName { get; set; }
        public DateTime? VerifiedAt { get; set; }
        public string? VerificationNotes { get; set; }
        public int? RevokedByUserId { get; set; }
        public string? RevokedByName { get; set; }
        public DateTime? RevokedAt { get; set; }
        public string? RevocationReason { get; set; }

        public bool IsCurrentlyValid =>
            Status == SigningAuthorityStatus.Active &&
            DateTime.UtcNow >= ValidFrom &&
            DateTime.UtcNow <= ValidTo;
    }
}
