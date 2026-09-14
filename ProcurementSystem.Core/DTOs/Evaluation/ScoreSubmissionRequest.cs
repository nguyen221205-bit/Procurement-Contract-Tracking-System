using System.ComponentModel.DataAnnotations;

namespace ProcurementSystem.Core.DTOs.Evaluation
{
    public class ScoreSubmissionRequest
    {
        [Required(ErrorMessage = "Danh sách điểm chấm không được để trống.")]
        public List<CriteriaScoreItem> Scores { get; set; } = new();
    }
}
