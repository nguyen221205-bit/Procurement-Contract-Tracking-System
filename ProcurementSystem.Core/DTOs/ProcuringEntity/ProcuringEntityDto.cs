namespace ProcurementSystem.Core.DTOs.ProcuringEntity
{
    public class ProcuringEntityDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? Phone { get; set; }

        public string OrganizationName { get; set; } = string.Empty;
        public string OrganizationType { get; set; } = string.Empty;
        public string TaxCode { get; set; } = string.Empty;
        public string? BudgetCode { get; set; }
        public string? Address { get; set; }

        public string RepresentativeName { get; set; } = string.Empty;
        public string RepresentativeTitle { get; set; } = string.Empty;
        public string? RepresentativePhone { get; set; }

        public string EstablishmentDecisionFile { get; set; } = string.Empty;
        public string? AppointmentDecisionFile { get; set; }

        public string VerificationStatus { get; set; } = "Pending"; // Pending, Approved, Rejected
        public string? AdminNotes { get; set; }

        public int? ReviewedByUserId { get; set; }
        public string? ReviewedByUserName { get; set; }
        public DateTime? ReviewedAt { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
