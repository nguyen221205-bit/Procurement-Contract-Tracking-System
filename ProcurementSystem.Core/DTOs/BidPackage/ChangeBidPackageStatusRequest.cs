using System.ComponentModel.DataAnnotations;
using ProcurementSystem.Core.Enums;

namespace ProcurementSystem.Core.DTOs.BidPackage
{
    public class ChangeBidPackageStatusRequest
    {
        [Required(ErrorMessage = "Trạng thái mới là bắt buộc.")]
        public BidPackageStatus NewStatus { get; set; }

        /// <summary>
        /// Lý do hoặc căn cứ thay đổi trạng thái (bắt buộc khi đóng thầu trước thời hạn)
        /// </summary>
        [MaxLength(1000, ErrorMessage = "Lý do không được vượt quá 1000 ký tự.")]
        public string? Reason { get; set; }

        /// <summary>
        /// Thời hạn nộp thầu mới (bắt buộc khi mở lại gói thầu Closed -> Open nếu thời hạn cũ đã hết)
        /// </summary>
        public DateTime? NewDeadline { get; set; }
    }
}
