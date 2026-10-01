using System.ComponentModel.DataAnnotations;

namespace ProcurementSystem.Core.DTOs.BidPackage
{
    public class AssignEvaluatorRequest
    {
        [Required(ErrorMessage = "Mã giám khảo (EvaluatorId) là bắt buộc.")]
        public int EvaluatorId { get; set; }
    }
}
