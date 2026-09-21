using System.ComponentModel.DataAnnotations;

namespace ProcurementSystem.Core.DTOs.Contract
{
    public class ApproveMilestoneAcceptanceRequest
    {
        [Required(ErrorMessage = "Trạng thái phê duyệt không được để trống.")]
        public bool IsApproved { get; set; } = true;

        [MaxLength(1000, ErrorMessage = "Ghi chú không được vượt quá 1000 ký tự.")]
        public string? Note { get; set; }
    }
}
