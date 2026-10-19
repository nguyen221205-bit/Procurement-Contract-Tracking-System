using System.ComponentModel.DataAnnotations;

namespace ProcurementSystem.Core.DTOs.Auth
{
    public class ChangePasswordRequest
    {
        [Required(ErrorMessage = "Mật khẩu hiện tại không được để trống.")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mật khẩu mới không được để trống.")]
        [MinLength(6, ErrorMessage = "Mật khẩu mới phải có ít nhất 6 ký tự.")]
        public string NewPassword { get; set; } = string.Empty;

        private string _confirmNewPassword = string.Empty;

        public string ConfirmNewPassword
        {
            get => _confirmNewPassword;
            set => _confirmNewPassword = value ?? string.Empty;
        }

        public string? ConfirmPassword
        {
            get => _confirmNewPassword;
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    _confirmNewPassword = value;
                }
            }
        }
    }
}
