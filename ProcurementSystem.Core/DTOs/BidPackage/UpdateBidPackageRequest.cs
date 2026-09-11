using System.ComponentModel.DataAnnotations;
using ProcurementSystem.Core.Enums;

namespace ProcurementSystem.Core.DTOs.BidPackage
{
    public class UpdateBidPackageRequest
    {
        [Required(ErrorMessage = "Tên gói thầu là bắt buộc.")]
        [MaxLength(300, ErrorMessage = "Tên gói thầu không được vượt quá 300 ký tự.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Loại gói thầu là bắt buộc.")]
        public BidPackageType Type { get; set; }

        [Required(ErrorMessage = "Dự toán ngân sách là bắt buộc.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Dự toán ngân sách phải lớn hơn 0.")]
        public decimal Budget { get; set; }

        [Required(ErrorMessage = "Thời hạn nộp thầu là bắt buộc.")]
        public DateTime Deadline { get; set; }

        [MaxLength(2000, ErrorMessage = "Mô tả không được vượt quá 2000 ký tự.")]
        public string? Description { get; set; }
    }
}
