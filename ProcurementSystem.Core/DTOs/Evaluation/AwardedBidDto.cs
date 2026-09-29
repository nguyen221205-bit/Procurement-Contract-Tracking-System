namespace ProcurementSystem.Core.DTOs.Evaluation
{
    public class AwardedBidDto
    {
        // Thông tin gói thầu
        public int BidPackageId { get; set; }
        public string PackageCode { get; set; } = string.Empty;
        public string PackageName { get; set; } = string.Empty;
        public decimal Budget { get; set; }
        public string PackageStatus { get; set; } = string.Empty;

        // Thông tin nhà thầu trúng thầu
        public int ContractorId { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public string? TaxCode { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }

        // Thông tin hồ sơ trúng thầu
        public int SubmissionId { get; set; }
        public decimal? TotalScore { get; set; }
        public int? Rank { get; set; }
        public DateTime AwardedAt { get; set; }

        // Trạng thái hợp đồng liên thông
        public bool HasContract { get; set; }
        public int? ExistingContractId { get; set; }
        public string? ExistingContractNumber { get; set; }
        public string? ContractStatus { get; set; }
        public bool IsReadyForContract { get; set; }
    }
}
