using ProcurementSystem.Core.Enums;

namespace ProcurementSystem.Core.DTOs.Report
{
    public class ProcurementSummaryReportDto
    {
        public int? FilteredYear { get; set; }
        public BidPackageType? FilteredType { get; set; }
        public string? FilteredTypeName { get; set; }

        public int TotalPackages { get; set; }
        public decimal TotalBudget { get; set; }
        public decimal TotalContractValue { get; set; }
        public decimal TotalSavings { get; set; }
        public decimal SavingsRate { get; set; }

        public List<PackageTypeStatDto> TypeBreakdowns { get; set; } = new();
        public List<PackageStatusStatDto> StatusBreakdowns { get; set; } = new();
    }

    public class PackageTypeStatDto
    {
        public BidPackageType Type { get; set; }
        public string TypeName { get; set; } = string.Empty;
        public int PackageCount { get; set; }
        public decimal TotalBudget { get; set; }
        public decimal TotalContractValue { get; set; }
        public decimal TotalSavings { get; set; }
        public decimal SavingsRate { get; set; }
    }

    public class PackageStatusStatDto
    {
        public BidPackageStatus Status { get; set; }
        public string StatusName { get; set; } = string.Empty;
        public int PackageCount { get; set; }
        public decimal Percentage { get; set; }
    }
}
