namespace ProcurementSystem.Core.DTOs.Evaluation
{
    public class EvaluationScoreDto
    {
        public int Id { get; set; }
        public int BidSubmissionId { get; set; }
        public int CriteriaId { get; set; }
        public string CriteriaName { get; set; } = string.Empty;
        public decimal MaxScore { get; set; }
        public decimal Weight { get; set; }
        public int EvaluatorId { get; set; }
        public string? EvaluatorName { get; set; }
        public decimal Score { get; set; }
        public string? Comment { get; set; }
        public DateTime ScoredAt { get; set; }
    }
}
