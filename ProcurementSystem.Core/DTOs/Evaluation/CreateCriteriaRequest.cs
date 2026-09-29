using System.ComponentModel.DataAnnotations;

namespace ProcurementSystem.Core.DTOs.Evaluation
{
    public class CreateCriteriaRequest
    {
        [Required(ErrorMessage = "Tên tiêu chí không được để trống.")]
        [MaxLength(200, ErrorMessage = "Tên tiêu chí không được vượt quá 200 ký tự.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Thang điểm tối đa là bắt buộc.")]
        [Range(0.01, 1000.00, ErrorMessage = "Thang điểm tối đa phải lớn hơn 0.")]
        public decimal MaxScore { get; set; } = 100;

        [Required(ErrorMessage = "Trọng số là bắt buộc.")]
        [Range(0.01, 100.00, ErrorMessage = "Trọng số phải lớn hơn 0.")]
        public decimal Weight { get; set; } = 1;
    }
}
