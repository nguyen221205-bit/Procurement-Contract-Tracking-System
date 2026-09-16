namespace ProcurementSystem.Core.DTOs.Evaluation
{
    public class SubmissionRankingDto
    {
        public int SubmissionId { get; set; }
        public int BidPackageId { get; set; }
        public int ContractorId { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public string? TaxCode { get; set; }
        public decimal? TotalScore { get; set; }
        public int? Rank { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime SubmittedAt { get; set; }
        public List<EvaluationScoreDto> Scores { get; set; } = new();
    }
}
