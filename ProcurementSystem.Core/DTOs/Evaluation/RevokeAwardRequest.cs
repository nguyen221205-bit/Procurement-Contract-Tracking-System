using System.ComponentModel.DataAnnotations;

namespace ProcurementSystem.Core.DTOs.Evaluation
{
    /// <summary>
    /// Dữ liệu yêu cầu thu hồi quyết định phê duyệt kết quả trúng thầu (Revoke Award).
    /// </summary>
    public class RevokeAwardRequest
    {
        /// <summary>
        /// Lý do hoặc căn cứ pháp lý thu hồi quyết định trao thầu (Bắt buộc).
        /// </summary>
        [Required(ErrorMessage = "Lý do thu hồi kết quả trao thầu là bắt buộc.")]
        [MinLength(5, ErrorMessage = "Lý do thu hồi phải có ít nhất 5 ký tự.")]
        [MaxLength(1000, ErrorMessage = "Lý do thu hồi không được vượt quá 1000 ký tự.")]
        public string Reason { get; set; } = string.Empty;
    }
}
