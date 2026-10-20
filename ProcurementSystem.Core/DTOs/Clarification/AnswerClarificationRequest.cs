using System.ComponentModel.DataAnnotations;

namespace ProcurementSystem.Core.DTOs.Clarification
{
    /// <summary>
    /// DTO phản hồi câu hỏi HSMT (từ Bên mời thầu) hoặc văn bản giải trình HSDT (từ Nhà thầu)
    /// </summary>
    public class AnswerClarificationRequest
    {
        [Required(ErrorMessage = "Nội dung phản hồi / giải trình không được để trống")]
        [MaxLength(3000)]
        public string AnswerContent { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? AnswerAttachmentUrl { get; set; }
    }
}
