using System.ComponentModel.DataAnnotations;

namespace ProcurementSystem.Core.DTOs.User
{
    public class ResetPasswordRequest
    {
        [MinLength(8, ErrorMessage = "Mật khẩu mới nếu nhập phải có độ dài tối thiểu 8 ký tự.")]
        [MaxLength(100, ErrorMessage = "Mật khẩu không được vượt quá 100 ký tự.")]
        public string? NewPassword { get; set; }
    }

    public class ResetPasswordResponseDto
    {
        public int UserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
        public bool MustChangePassword { get; set; } = true;
        public bool EmailSent { get; set; }
    }
}
