using System.ComponentModel.DataAnnotations;

namespace ProcurementSystem.Core.DTOs.Contract
{
    public class TechnicalReviewMilestoneRequest
    {
        [Required(ErrorMessage = "Vui lòng xác nhận kết quả kiểm tra kỹ thuật.")]
        public bool IsPassed { get; set; }

        [MaxLength(1000, ErrorMessage = "Ghi chú tối đa 1000 ký tự.")]
        public string? Note { get; set; }
    }
}
