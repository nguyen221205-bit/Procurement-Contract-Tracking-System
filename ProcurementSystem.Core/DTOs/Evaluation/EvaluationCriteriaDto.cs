namespace ProcurementSystem.Core.DTOs.Evaluation
{
    public class EvaluationCriteriaDto
    {
        public int Id { get; set; }
        public int BidPackageId { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal MaxScore { get; set; }
        public decimal Weight { get; set; }
    }
}
