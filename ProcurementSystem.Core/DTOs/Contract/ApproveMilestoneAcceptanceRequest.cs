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

        /// <summary>
        /// Mã định danh biên bản nghiệm thu kỹ thuật mà Người ký duyệt đã xem xét
        /// </summary>
        [Required(ErrorMessage = "Mã định danh biên bản nghiệm thu kỹ thuật (AcceptanceId) là bắt buộc.")]
        [Range(1, int.MaxValue, ErrorMessage = "AcceptanceId phải lớn hơn 0.")]
        public int AcceptanceId { get; set; }

        /// <summary>
        /// Số lần thẩm tra kỹ thuật (Revision) mà Người ký duyệt đã xem xét
        /// </summary>
        [Required(ErrorMessage = "Số lần thẩm tra kỹ thuật kỳ vọng (ExpectedRevision) là bắt buộc.")]
        [Range(1, int.MaxValue, ErrorMessage = "ExpectedRevision phải lớn hơn 0.")]
        public int ExpectedRevision { get; set; }

        /// <summary>
        /// Phiên bản nội dung mốc mà Người ký duyệt đã thẩm tra (bắt buộc, chống race condition và tráo đổi nội dung sau thẩm tra)
        /// </summary>
        [Required(ErrorMessage = "Phiên bản mốc thanh toán nghiệm thu (ExpectedVersion) là bắt buộc.")]
        [Range(1, int.MaxValue, ErrorMessage = "Phiên bản mốc thanh toán phải lớn hơn 0.")]
        public int ExpectedVersion { get; set; }
    }
}
