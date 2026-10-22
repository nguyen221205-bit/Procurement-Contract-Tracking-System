using System.ComponentModel.DataAnnotations;

namespace ProcurementSystem.Core.DTOs.BidPackage
{
    /// <summary>
    /// Request của Admin khi xem xét phê duyệt hoặc từ chối yêu cầu thay đổi trạng thái gói thầu
    /// </summary>
    public class ReviewPackageStatusApprovalRequest
    {
        [Required]
        public bool IsApproved { get; set; } // true = Approve, false = Reject

        [MaxLength(1000)]
        public string? AdminComment { get; set; } // Ý kiến thẩm định hoặc lý do từ chối
    }
}
