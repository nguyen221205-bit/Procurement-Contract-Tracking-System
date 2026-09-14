using ProcurementSystem.Core.Enums;

namespace ProcurementSystem.Core.DTOs.Contract
{
    public class ContractSummaryDto
    {
        public int Id { get; set; }
        public int BidPackageId { get; set; }
        public string BidPackageCode { get; set; } = string.Empty;
        public int ContractorId { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public string ContractNumber { get; set; } = string.Empty;
        public decimal Value { get; set; }
        public ContractStatus Status { get; set; }
        public string StatusName { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int MilestoneCount { get; set; }
    }
}
