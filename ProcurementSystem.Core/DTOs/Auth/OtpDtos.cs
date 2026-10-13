using System.ComponentModel.DataAnnotations;

namespace ProcurementSystem.Core.DTOs.Auth
{
    public class SendOtpRequest
    {
        [Required(ErrorMessage = "Email là bắt buộc")]
        [EmailAddress(ErrorMessage = "Địa chỉ email không đúng định dạng")]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Mục đích gửi OTP: RegisterContractor, RegisterProcuringEntity, Register
        /// </summary>
        public string Purpose { get; set; } = "Register";
    }

    public class SendOtpResponse
    {
        public string Email { get; set; } = string.Empty;
        public int CooldownSeconds { get; set; } = 60;
        public int ExpiryMinutes { get; set; } = 5;

        /// <summary>
        /// Mã OTP trả về trong môi trường Development nhằm phục vụ kiểm thử nhanh
        /// </summary>
        public string? DevOtpCode { get; set; }
    }

    public class VerifyOtpRequest
    {
        [Required(ErrorMessage = "Email là bắt buộc")]
        [EmailAddress(ErrorMessage = "Địa chỉ email không đúng định dạng")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mã OTP là bắt buộc")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "Mã OTP phải gồm đúng 6 chữ số")]
        public string OtpCode { get; set; } = string.Empty;

        public string Purpose { get; set; } = "Register";
    }
}
