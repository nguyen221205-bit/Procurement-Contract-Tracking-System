using System.ComponentModel.DataAnnotations;
using ProcurementSystem.Core.Enums;

namespace ProcurementSystem.Core.DTOs.BidPackage
{
    public class ChangeBidPackageStatusRequest
    {
        [Required(ErrorMessage = "Trạng thái mới là bắt buộc.")]
        public BidPackageStatus NewStatus { get; set; }
    }
}
