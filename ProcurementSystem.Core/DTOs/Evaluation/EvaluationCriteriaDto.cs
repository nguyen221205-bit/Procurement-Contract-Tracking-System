namespace ProcurementSystem.Core.DTOs.Evaluation
{
    public class EvaluationCriteriaDto
    {
        public int Id { get; set; }
        public int BidPackageId { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal MaxScore { get; set; }
        public decimal Weight { get; set; }

        /// <summary>
        /// Đánh dấu là tiêu chí Giá / Tài chính (hệ thống tự động tính điểm theo công thức luật đấu thầu)
        /// </summary>
        public bool IsPriceCriteria { get; set; }
    }
}
