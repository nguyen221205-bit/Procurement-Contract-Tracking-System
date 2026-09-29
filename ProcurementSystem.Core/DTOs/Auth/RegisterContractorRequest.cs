using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace ProcurementSystem.Core.DTOs.Auth
{
    public class RegisterContractorRequest
    {
        [Required(ErrorMessage = "Họ tên người đại diện là bắt buộc")]
        [MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email là bắt buộc")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
        [MinLength(6, ErrorMessage = "Mật khẩu tối thiểu 6 ký tự")]
        public string Password { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        public string? Phone { get; set; }

        [Required(ErrorMessage = "Tên công ty là bắt buộc")]
        [MaxLength(200)]
        public string CompanyName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mã số thuế là bắt buộc")]
        [MaxLength(20)]
        public string TaxCode { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Address { get; set; }

        /// <summary>
        /// File Giấy phép kinh doanh (PDF, JPG, PNG - Tối đa 10MB)
        /// </summary>
        [Required(ErrorMessage = "Giấy phép kinh doanh là bắt buộc")]
        public IFormFile BusinessLicenseFile { get; set; } = null!;
    }

    public class ContractorRegisterResponse
    {
        public int UserId { get; set; }
        public int ContractorId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string TaxCode { get; set; } = string.Empty;
        public string BusinessLicenseUrl { get; set; } = string.Empty;
        
        // Kết quả thẩm định tự động
        public bool IsTaxCodeVerified { get; set; }
        public string TaxOfficialName { get; set; } = string.Empty;
        public bool IsDigitallySigned { get; set; }
        public string? SignerInfo { get; set; }
        public string VerificationMessage { get; set; } = string.Empty;
    }
}
