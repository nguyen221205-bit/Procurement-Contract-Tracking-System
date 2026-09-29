using System.ComponentModel.DataAnnotations;

namespace ProcurementSystem.Core.DTOs.Contract
{
    /// <summary>
    /// Yêu cầu phê duyệt hoặc từ chối nghiệm thu mốc thanh toán của hợp đồng
    /// </summary>
    public class ApproveMilestoneAcceptanceRequest
    {
        /// <summary>
        /// Kết quả nghiệm thu: true = Phê duyệt (chuyển Completed), false = Từ chối
        /// </summary>
        [Required(ErrorMessage = "Trạng thái phê duyệt không được để trống.")]
        public bool IsApproved { get; set; } = true;

        /// <summary>
        /// Ghi chú, ý kiến thẩm định hoặc lý do từ chối biên bản nghiệm thu
        /// </summary>
        [MaxLength(1000, ErrorMessage = "Ghi chú không được vượt quá 1000 ký tự.")]
        public string? Note { get; set; }
    }
}
