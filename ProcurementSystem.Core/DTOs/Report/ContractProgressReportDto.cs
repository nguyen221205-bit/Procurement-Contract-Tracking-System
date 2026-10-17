namespace ProcurementSystem.Core.DTOs.Report
{
    public class ContractProgressReportDto
    {
        public int TotalMilestones { get; set; }
        public int CompletedMilestones { get; set; }
        public int PendingMilestones { get; set; }
        public int OnTimeMilestones { get; set; }
        public int DelayedMilestones { get; set; }
        public decimal OnTimeRate { get; set; }

        public int ExpiringContractsCount { get; set; }
        public int OverdueContractsCount { get; set; }
        public List<ExpiringContractItemDto> ExpiringContracts { get; set; } = new();
    }

    public class ExpiringContractItemDto
    {
        public int ContractId { get; set; }
        public string ContractNumber { get; set; } = string.Empty;
        public string BidPackageCode { get; set; } = string.Empty;
        public string BidPackageName { get; set; } = string.Empty;
        public string ContractorName { get; set; } = string.Empty;
        public decimal ContractValue { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int DaysRemaining { get; set; }
        public bool IsOverdue { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal TotalDisbursedAmount { get; set; }
        public decimal DisbursementRate { get; set; }
    }
}
