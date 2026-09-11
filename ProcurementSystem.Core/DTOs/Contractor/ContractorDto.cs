namespace ProcurementSystem.Core.DTOs.Contractor
{
    public class ContractorDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public string? TaxCode { get; set; }
        public string? Address { get; set; }
        public string? BusinessLicenseFile { get; set; }
        public decimal Rating { get; set; }
        public DateTime CreatedAt { get; set; }

        // Thông tin người đại diện (từ User)
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
    }
}
