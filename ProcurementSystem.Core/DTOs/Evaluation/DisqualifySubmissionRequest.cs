using System.ComponentModel.DataAnnotations;

namespace ProcurementSystem.Core.DTOs.Evaluation
{
    /// <summary>
    /// Yêu cầu loại bỏ hồ sơ dự thầu do không đáp ứng tư cách hợp lệ hoặc vi phạm quy định
    /// </summary>
    public class DisqualifySubmissionRequest
    {
        /// <summary>
        /// Lý do loại bỏ hồ sơ dự thầu (tối thiểu 5 ký tự)
        /// </summary>
        [Required(ErrorMessage = "Lý do loại hồ sơ dự thầu là bắt buộc.")]
        [MinLength(5, ErrorMessage = "Lý do loại hồ sơ dự thầu phải có ít nhất 5 ký tự.")]
        [MaxLength(1000, ErrorMessage = "Lý do loại hồ sơ dự thầu không được vượt quá 1000 ký tự.")]
        public string Reason { get; set; } = string.Empty;
    }
}
