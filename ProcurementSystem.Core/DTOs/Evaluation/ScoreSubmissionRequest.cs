using System.ComponentModel.DataAnnotations;

namespace ProcurementSystem.Core.DTOs.Evaluation
{
    public class ScoreSubmissionRequest
    {
        [Required(ErrorMessage = "Danh sách điểm chấm không được để trống.")]
        [MinLength(1, ErrorMessage = "Phiếu chấm điểm phải có ít nhất một tiêu chí đánh giá.")]
        public List<CriteriaScoreItem> Scores { get; set; } = new();
    }
}
