using System.ComponentModel.DataAnnotations;

namespace ProcurementSystem.Core.DTOs.EvaluatorProposal
{
    /// <summary>
    /// DTO yêu cầu phê duyệt đề xuất và khởi tạo tài khoản Giám khảo (Chỉ Admin)
    /// </summary>
    public class ApproveEvaluatorProposalRequest
    {
        /// <summary>
        /// Mật khẩu khởi tạo tài khoản do Admin đặt (tối thiểu 6 ký tự)
        /// </summary>
        [Required(ErrorMessage = "Mật khẩu khởi tạo tài khoản là bắt buộc.")]
        [MinLength(6, ErrorMessage = "Mật khẩu khởi tạo phải có tối thiểu 6 ký tự.")]
        [MaxLength(100, ErrorMessage = "Mật khẩu không được vượt quá 100 ký tự.")]
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// Ghi chú phê duyệt của Admin (tùy chọn)
        /// </summary>
        [MaxLength(1000, ErrorMessage = "Ghi chú không được vượt quá 1000 ký tự.")]
        public string? AdminNotes { get; set; }
    }
}
