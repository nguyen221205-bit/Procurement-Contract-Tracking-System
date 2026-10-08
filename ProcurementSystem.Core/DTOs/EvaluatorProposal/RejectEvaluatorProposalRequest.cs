using System.ComponentModel.DataAnnotations;

namespace ProcurementSystem.Core.DTOs.EvaluatorProposal
{
    /// <summary>
    /// DTO yêu cầu từ chối đề xuất Giám khảo (Chỉ Admin)
    /// </summary>
    public class RejectEvaluatorProposalRequest
    {
        /// <summary>
        /// Lý do từ chối giải trình của Admin
        /// </summary>
        [Required(ErrorMessage = "Lý do từ chối đề xuất là bắt buộc.")]
        [MinLength(5, ErrorMessage = "Lý do từ chối phải có tối thiểu 5 ký tự.")]
        [MaxLength(1000, ErrorMessage = "Lý do từ chối không được vượt quá 1000 ký tự.")]
        public string AdminNotes { get; set; } = string.Empty;
    }
}
