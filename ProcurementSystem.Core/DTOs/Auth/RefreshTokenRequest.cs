using System.ComponentModel.DataAnnotations;

namespace ProcurementSystem.Core.DTOs.Auth
{
    public class RefreshTokenRequest
    {
        [Required(ErrorMessage = "Token là bắt buộc")]
        public string Token { get; set; } = string.Empty;

        [Required(ErrorMessage = "RefreshToken là bắt buộc")]
        public string RefreshToken { get; set; } = string.Empty;
    }
}
