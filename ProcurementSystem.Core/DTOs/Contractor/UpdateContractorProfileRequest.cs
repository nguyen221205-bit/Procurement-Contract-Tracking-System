using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace ProcurementSystem.Core.DTOs.Contractor
{
    public class UpdateContractorProfileRequest
    {
        [Required(ErrorMessage = "Tên công ty là bắt buộc.")]
        [MaxLength(200, ErrorMessage = "Tên công ty không được vượt quá 200 ký tự.")]
        public string CompanyName { get; set; } = string.Empty;

        [MaxLength(500, ErrorMessage = "Địa chỉ không được vượt quá 500 ký tự.")]
        public string? Address { get; set; }

        /// <summary>
        /// File Giấy phép kinh doanh mới nếu muốn thay thế (PDF, JPG, PNG - Tối đa 10MB)
        /// </summary>
        public IFormFile? BusinessLicenseFile { get; set; }
    }
}
