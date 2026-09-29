namespace ProcurementSystem.Core.DTOs.BidSubmission
{
    public class BidSubmissionSummaryDto
    {
        public int Id { get; set; }
        public int BidPackageId { get; set; }
        public string BidPackageCode { get; set; } = string.Empty;
        public string BidPackageName { get; set; } = string.Empty;

        public int ContractorId { get; set; }
        public string CompanyName { get; set; } = string.Empty;

        public DateTime SubmittedAt { get; set; }
        public decimal? TotalScore { get; set; }
        public int? Rank { get; set; }
        public string Status { get; set; } = string.Empty;
        public int FileCount { get; set; }
    }
}
