using System.ComponentModel.DataAnnotations;

namespace ProcurementSystem.Core.DTOs.ProcuringEntity
{
    public class VerifyProcuringEntityRequest
    {
        [Required(ErrorMessage = "Trạng thái phê duyệt là bắt buộc")]
        public bool IsApproved { get; set; }

        [MaxLength(1000, ErrorMessage = "Ghi chú không được vượt quá 1000 ký tự")]
        public string? AdminNotes { get; set; }
    }
}
