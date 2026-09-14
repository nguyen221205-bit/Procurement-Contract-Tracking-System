using ProcurementSystem.Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace ProcurementSystem.Core.DTOs.Contract
{
    public class ChangeContractStatusRequest
    {
        [Required(ErrorMessage = "Trạng thái mới là bắt buộc.")]
        public ContractStatus NewStatus { get; set; }

        [MaxLength(500)]
        public string? Note { get; set; }
    }
}
