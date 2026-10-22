using System.ComponentModel.DataAnnotations;

namespace ProcurementSystem.Core.DTOs.Clarification
{
    /// <summary>
    /// DTO yêu cầu làm rõ Hồ sơ mời thầu (HSMT) do Nhà thầu gửi
    /// </summary>
    public class CreatePackageClarificationRequest
    {
        [Required(ErrorMessage = "Tiêu đề yêu cầu làm rõ không được để trống")]
        [MaxLength(250)]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Nội dung thắc mắc không được để trống")]
        [MaxLength(3000)]
        public string QuestionContent { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? QuestionAttachmentUrl { get; set; }
    }
}
