namespace ProcurementSystem.Core.DTOs.ProcuringEntity
{
    public class ProcuringEntityFilterParams
    {
        public string? Keyword { get; set; } // Tìm theo tên đơn vị, tên người đại diện, email
        public string? TaxCode { get; set; }
        public string? OrganizationType { get; set; }
        public string? VerificationStatus { get; set; } // Pending, Approved, Rejected

        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
