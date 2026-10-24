using ProcurementSystem.Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace ProcurementSystem.Core.DTOs.Contract
{
    public class ChangeContractStatusRequest
    {
        [Required(ErrorMessage = "Trạng thái mới là bắt buộc.")]
        public ContractStatus NewStatus { get; set; }

        [MaxLength(1000)]
        public string? Note { get; set; }

        /// <summary>
        /// Phân loại căn cứ pháp lý khi chấm dứt hợp đồng:
        /// ContractorBreach, ForceMajeure, MutualAgreement, OwnerDecision
        /// </summary>
        [MaxLength(100)]
        public string? TerminationReasonCategory { get; set; }

        /// <summary>
        /// Số hiệu Văn bản / Quyết định / Biên bản chấm dứt hợp đồng làm căn cứ pháp lý.
        /// </summary>
        [MaxLength(100)]
        public string? DecisionNumber { get; set; }
    }
}
