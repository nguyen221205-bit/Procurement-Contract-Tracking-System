using ProcurementSystem.Core.Enums;

namespace ProcurementSystem.Core.DTOs.Contract
{
    public class ContractDto
    {
        public int Id { get; set; }
        public int BidPackageId { get; set; }
        public string BidPackageCode { get; set; } = string.Empty;
        public string BidPackageName { get; set; } = string.Empty;

        public int ContractorId { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public string? TaxCode { get; set; }

        public string ContractNumber { get; set; } = string.Empty;
        public decimal Value { get; set; }
        public string? Terms { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public ContractStatus Status { get; set; }
        public string StatusName { get; set; } = string.Empty;

        public string? ScannedFilePath { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public List<ContractMilestoneDto> Milestones { get; set; } = new();
    }
}
