namespace ProcurementSystem.Core.DTOs.User
{
    public class UserDto
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public List<string> Roles { get; set; } = new();
        
        // Thông tin nhà thầu liên kết (nếu có)
        public UserContractorSummary? Contractor { get; set; }
    }

    public class UserContractorSummary
    {
        public int ContractorId { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public string? TaxCode { get; set; }
        public decimal Rating { get; set; }
    }
}
