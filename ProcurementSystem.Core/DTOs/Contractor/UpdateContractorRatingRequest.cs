using System.ComponentModel.DataAnnotations;

namespace ProcurementSystem.Core.DTOs.Contractor
{
    public class UpdateContractorRatingRequest
    {
        [Required(ErrorMessage = "Điểm đánh giá là bắt buộc.")]
        [Range(0.0, 5.0, ErrorMessage = "Điểm đánh giá phải nằm trong khoảng từ 0.0 đến 5.0 sao.")]
        public decimal Rating { get; set; }
    }
}
