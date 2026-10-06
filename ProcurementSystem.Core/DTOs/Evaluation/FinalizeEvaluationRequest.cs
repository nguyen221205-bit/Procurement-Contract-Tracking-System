using System.ComponentModel.DataAnnotations;

namespace ProcurementSystem.Core.DTOs.Evaluation
{
    public class FinalizeEvaluationRequest
    {
        [Required(ErrorMessage = "Mã hồ sơ dự thầu trúng thầu là bắt buộc.")]
        public int SelectedSubmissionId { get; set; }

        /// <summary>
        /// Căn cứ / lý do phê duyệt lựa chọn nhà thầu (Bắt buộc nếu chọn hồ sơ xếp hạng > 1)
        /// </summary>
        [MaxLength(1000, ErrorMessage = "Lý do phê duyệt không được vượt quá 1000 ký tự.")]
        public string? DecisionReason { get; set; }
    }
}
