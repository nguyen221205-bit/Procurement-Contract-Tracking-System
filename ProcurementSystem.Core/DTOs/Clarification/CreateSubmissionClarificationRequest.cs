using System.ComponentModel.DataAnnotations;

namespace ProcurementSystem.Core.DTOs.Clarification
{
    /// <summary>
    /// DTO yêu cầu làm rõ / giải trình Hồ sơ dự thầu (HSDT) do Tổ chuyên gia hoặc Bên mời thầu gửi
    /// </summary>
    public class CreateSubmissionClarificationRequest
    {
        [Required(ErrorMessage = "Vui lòng chọn hồ sơ dự thầu cần làm rõ")]
        public int BidSubmissionId { get; set; }

        [Required(ErrorMessage = "Tiêu đề yêu cầu làm rõ không được để trống")]
        [MaxLength(250)]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Nội dung yêu cầu giải trình không được để trống")]
        [MaxLength(3000)]
        public string QuestionContent { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? QuestionAttachmentUrl { get; set; }

        [Required(ErrorMessage = "Vui lòng chỉ định thời hạn giải trình")]
        public DateTime Deadline { get; set; }
    }
}
