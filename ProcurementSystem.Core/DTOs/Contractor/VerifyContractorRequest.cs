using System.ComponentModel.DataAnnotations;

namespace ProcurementSystem.Core.DTOs.Contractor
{
    /// <summary>
    /// DTO yêu cầu phê duyệt hoặc từ chối hồ sơ năng lực nhà thầu
    /// </summary>
    public class VerifyContractorRequest
    {
        /// <summary>
        /// True: Phê duyệt (Approved), False: Từ chối (Rejected)
        /// </summary>
        [Required]
        public bool IsApproved { get; set; }

        /// <summary>
        /// Ghi chú hoặc lý do phê duyệt / từ chối
        /// </summary>
        [MaxLength(500)]
        public string? Notes { get; set; }
    }
}
