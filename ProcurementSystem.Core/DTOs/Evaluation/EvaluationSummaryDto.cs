namespace ProcurementSystem.Core.DTOs.Evaluation
{
    public class EvaluationSummaryDto
    {
        public int BidPackageId { get; set; }
        public string BidPackageCode { get; set; } = string.Empty;
        public string BidPackageName { get; set; } = string.Empty;
        public decimal Budget { get; set; }
        public string PackageStatus { get; set; } = string.Empty;

        // Thống kê tiêu chí
        public int TotalCriteria { get; set; }
        public decimal TotalWeight { get; set; }

        // Thống kê hồ sơ
        public int TotalSubmissions { get; set; }
        public int EvaluatedSubmissions { get; set; }
        public int PendingSubmissions { get; set; }

        // Thống kê điểm số
        public decimal? HighestScore { get; set; }
        public decimal? LowestScore { get; set; }
        public decimal? AverageScore { get; set; }

        // Thông tin hồ sơ trúng thầu (nếu đã phê duyệt)
        public bool IsFinalized { get; set; }
        public int? WinningSubmissionId { get; set; }
        public int? WinningContractorId { get; set; }
        public string? WinningContractorName { get; set; }
        public string? WinningContractorTaxCode { get; set; }
        public decimal? WinningScore { get; set; }
    }
}
