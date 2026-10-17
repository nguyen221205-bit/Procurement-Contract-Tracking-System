using System.ComponentModel.DataAnnotations;

namespace ProcurementSystem.Core.DTOs.BidPackage
{
    public class ReplaceEvaluatorRequest
    {
        [Required(ErrorMessage = "Vui lòng chỉ định giám khảo cần thay thế (OldEvaluatorId).")]
        public int OldEvaluatorId { get; set; }

        [Required(ErrorMessage = "Vui lòng chỉ định giám khảo mới thay thế (NewEvaluatorId).")]
        public int NewEvaluatorId { get; set; }

        [Required(ErrorMessage = "Lý do thay thế giám khảo không được để trống.")]
        [MaxLength(500, ErrorMessage = "Lý do thay thế tối đa 500 ký tự.")]
        public string Reason { get; set; } = string.Empty;
    }
}
