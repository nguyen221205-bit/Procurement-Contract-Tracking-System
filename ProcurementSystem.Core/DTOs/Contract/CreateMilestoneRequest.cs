using System.ComponentModel.DataAnnotations;

namespace ProcurementSystem.Core.DTOs.Contract
{
    public class CreateMilestoneRequest
    {
        [Required(ErrorMessage = "Tiêu đề mốc thanh toán là bắt buộc.")]
        [MaxLength(300, ErrorMessage = "Tiêu đề không được vượt quá 300 ký tự.")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Ngày đến hạn là bắt buộc.")]
        public DateTime DueDate { get; set; }

        [Range(1, double.MaxValue, ErrorMessage = "Giá trị mốc thanh toán phải lớn hơn 0.")]
        public decimal Amount { get; set; }
    }
}
