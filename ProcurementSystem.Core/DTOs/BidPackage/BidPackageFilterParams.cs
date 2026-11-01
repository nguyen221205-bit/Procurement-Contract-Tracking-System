using System.ComponentModel.DataAnnotations;
using ProcurementSystem.Core.Enums;

namespace ProcurementSystem.Core.DTOs.BidPackage
{
    public class BidPackageFilterParams
    {
        public string? Search { get; set; }
        [EnumDataType(typeof(BidPackageType))]
        public BidPackageType? Type { get; set; }
        [EnumDataType(typeof(BidPackageStatus))]
        public BidPackageStatus? Status { get; set; }
        public decimal? MinBudget { get; set; }
        public decimal? MaxBudget { get; set; }
        public DateTime? FromDeadline { get; set; }
        public DateTime? ToDeadline { get; set; }
        public int? EvaluatorId { get; set; }
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
