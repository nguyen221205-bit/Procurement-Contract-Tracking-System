using System.ComponentModel.DataAnnotations;

namespace ProcurementSystem.Core.DTOs.Contract
{
    public class CreateProgressUpdateRequest
    {
        [Required(ErrorMessage = "Số tuần báo cáo không được để trống.")]
        [Range(1, 100, ErrorMessage = "Số tuần phải từ 1 đến 100.")]
        public int WeekNumber { get; set; }

        [Required(ErrorMessage = "Phần trăm hoàn thành không được để trống.")]
        [Range(0, 100, ErrorMessage = "Phần trăm hoàn thành phải từ 0% đến 100%.")]
        public decimal CompletionPercent { get; set; }

        [MaxLength(2000, ErrorMessage = "Ghi chú không được vượt quá 2000 ký tự.")]
        public string? Note { get; set; }
    }
}
