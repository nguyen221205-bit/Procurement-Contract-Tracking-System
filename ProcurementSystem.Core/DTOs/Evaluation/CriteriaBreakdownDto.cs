namespace ProcurementSystem.Core.DTOs.Evaluation;

public class CriteriaBreakdownDto
{
    public int CriteriaId { get; set; }
    public bool IsPriceCriteria { get; set; }
    public decimal? AverageScore { get; set; }
    public decimal? NormalizedScore { get; set; }
    public decimal? WeightedScore { get; set; }
}

public class EvaluatorScoreSummaryDto
{
    public int EvaluatorId { get; set; }
    public decimal ManualWeightedScore { get; set; }
    public decimal ManualMaxWeight { get; set; }
    public bool HasCompletedScoring { get; set; }
}
