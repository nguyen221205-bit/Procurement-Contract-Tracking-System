using System.ComponentModel.DataAnnotations;

namespace ProcurementSystem.Core.DTOs.Contract
{
    /// <summary>
    /// Dữ liệu tạo hoặc cập nhật mốc thanh toán nghiệm thu.
    /// </summary>
    public class CreateMilestoneRequest
    {
        /// <summary>Tiêu đề hoặc mô tả ngắn của mốc thanh toán.</summary>
        [Required(ErrorMessage = "Tiêu đề mốc thanh toán là bắt buộc.")]
        [MaxLength(300, ErrorMessage = "Tiêu đề không được vượt quá 300 ký tự.")]
        public string Title { get; set; } = string.Empty;

        /// <summary>Ngày đến hạn nghiệm thu hoặc thanh toán.</summary>
        [Required(ErrorMessage = "Ngày đến hạn là bắt buộc.")]
        public DateTime DueDate { get; set; }

        /// <summary>Giá trị thanh toán của mốc.</summary>
        [Range(1, double.MaxValue, ErrorMessage = "Giá trị mốc thanh toán phải lớn hơn 0.")]
        public decimal Amount { get; set; }
    }
}
