using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace ProcurementSystem.Core.DTOs.Auth
{
    public class RegisterProcuringEntityRequest
    {
        // Thông tin tài khoản người dùng
        [Required(ErrorMessage = "Họ tên người đăng ký / phụ trách là bắt buộc")]
        [MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email công vụ / đăng nhập là bắt buộc")]
        [EmailAddress(ErrorMessage = "Email không đúng định dạng")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
        [MinLength(6, ErrorMessage = "Mật khẩu tối thiểu 6 ký tự")]
        public string Password { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        public string? Phone { get; set; }

        // Thông tin đơn vị mời thầu / Chủ đầu tư
        [Required(ErrorMessage = "Tên cơ quan / đơn vị mời thầu là bắt buộc")]
        [MaxLength(250)]
        public string OrganizationName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Loại hình cơ quan / đơn vị là bắt buộc")]
        [MaxLength(100)]
        public string OrganizationType { get; set; } = string.Empty; // Cơ quan hành chính nhà nước, Đơn vị sự nghiệp công lập, Ban Quản lý Dự án, Doanh nghiệp nhà nước, Khác

        [Required(ErrorMessage = "Mã số thuế là bắt buộc")]
        [MaxLength(20)]
        public string TaxCode { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? BudgetCode { get; set; } // Mã Đơn vị có quan hệ với ngân sách (Mã ĐVQHNS)

        [MaxLength(500)]
        public string? Address { get; set; }

        // Thông tin Người đại diện theo pháp luật / Lãnh đạo phụ trách
        [Required(ErrorMessage = "Họ tên người đại diện pháp luật là bắt buộc")]
        [MaxLength(100)]
        public string RepresentativeName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Chức vụ người đại diện là bắt buộc")]
        [MaxLength(100)]
        public string RepresentativeTitle { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? RepresentativePhone { get; set; }

        /// <summary>
        /// Mã OTP xác thực email công vụ (6 chữ số)
        /// </summary>
        [Required(ErrorMessage = "Mã xác thực OTP là bắt buộc")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "Mã OTP phải gồm đúng 6 chữ số")]
        public string OtpCode { get; set; } = string.Empty;

        /// <summary>
        /// File scan Quyết định thành lập / Giấy phép kinh doanh (PDF, JPG, PNG - Tối đa 15MB)
        /// </summary>
        [Required(ErrorMessage = "Tệp Quyết định thành lập / Giấy phép hoạt động là bắt buộc")]
        public IFormFile EstablishmentDecisionFile { get; set; } = null!;

        /// <summary>
        /// File Quyết định bổ nhiệm người đứng đầu / Giấy ủy quyền (Tùy chọn)
        /// </summary>
        public IFormFile? AppointmentDecisionFile { get; set; }
    }

    public class ProcuringEntityRegisterResponse
    {
        public int UserId { get; set; }
        public int ProcuringEntityId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string OrganizationName { get; set; } = string.Empty;
        public string OrganizationType { get; set; } = string.Empty;
        public string TaxCode { get; set; } = string.Empty;
        public string EstablishmentDecisionFileUrl { get; set; } = string.Empty;
        public string? AppointmentDecisionFileUrl { get; set; }
        public string VerificationStatus { get; set; } = "Pending";
        public string VerificationMessage { get; set; } = string.Empty;
    }
}
