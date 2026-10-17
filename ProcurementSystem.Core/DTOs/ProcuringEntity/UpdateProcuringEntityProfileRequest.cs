using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace ProcurementSystem.Core.DTOs.ProcuringEntity
{
    public class UpdateProcuringEntityProfileRequest
    {
        [Required(ErrorMessage = "Tên cơ quan / đơn vị mời thầu là bắt buộc")]
        [MaxLength(250, ErrorMessage = "Tên cơ quan không được vượt quá 250 ký tự")]
        public string OrganizationName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Loại hình cơ quan / đơn vị là bắt buộc")]
        [MaxLength(100, ErrorMessage = "Loại hình cơ quan không được vượt quá 100 ký tự")]
        public string OrganizationType { get; set; } = string.Empty;

        [MaxLength(50, ErrorMessage = "Mã ĐVQHNS không được vượt quá 50 ký tự")]
        public string? BudgetCode { get; set; }

        [MaxLength(500, ErrorMessage = "Địa chỉ không được vượt quá 500 ký tự")]
        public string? Address { get; set; }

        [Required(ErrorMessage = "Họ tên người đại diện pháp luật là bắt buộc")]
        [MaxLength(100, ErrorMessage = "Họ tên người đại diện không được vượt quá 100 ký tự")]
        public string RepresentativeName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Chức vụ người đại diện là bắt buộc")]
        [MaxLength(100, ErrorMessage = "Chức vụ không được vượt quá 100 ký tự")]
        public string RepresentativeTitle { get; set; } = string.Empty;

        [MaxLength(20, ErrorMessage = "Số điện thoại không được vượt quá 20 ký tự")]
        public string? RepresentativePhone { get; set; }

        /// <summary>
        /// File scan Quyết định thành lập / Giấy phép hoạt động mới nếu muốn thay thế (PDF, JPG, PNG - Tối đa 15MB)
        /// </summary>
        public IFormFile? EstablishmentDecisionFile { get; set; }

        /// <summary>
        /// File Quyết định bổ nhiệm người đứng đầu / Giấy ủy quyền mới nếu muốn thay thế (Tùy chọn)
        /// </summary>
        public IFormFile? AppointmentDecisionFile { get; set; }
    }
}
