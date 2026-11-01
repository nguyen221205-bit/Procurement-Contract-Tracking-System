using System.ComponentModel.DataAnnotations;

namespace ProcurementSystem.Core.DTOs.ProcuringEntity
{
    public class AcceptInvitationRequest
    {
        [Required(ErrorMessage = "Token không được để trống.")]
        public string Token { get; set; } = string.Empty;

        [MaxLength(100, ErrorMessage = "Họ tên tối đa 100 ký tự.")]
        public string? FullName { get; set; }

        [MinLength(6, ErrorMessage = "Mật khẩu tối thiểu 6 ký tự.")]
        public string? Password { get; set; }
    }
}
