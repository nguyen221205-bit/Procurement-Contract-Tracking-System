using System.ComponentModel.DataAnnotations;

namespace ProcurementSystem.Core.DTOs.Evaluation
{
    public class CriteriaScoreItem
    {
        [Required(ErrorMessage = "Mã tiêu chí là bắt buộc.")]
        public int CriteriaId { get; set; }

        [Required(ErrorMessage = "Điểm số là bắt buộc.")]
        [Range(0, 1000.00, ErrorMessage = "Điểm số không được là số âm.")]
        public decimal Score { get; set; }

        [MaxLength(500, ErrorMessage = "Nhận xét không được vượt quá 500 ký tự.")]
        public string? Comment { get; set; }
    }
}
