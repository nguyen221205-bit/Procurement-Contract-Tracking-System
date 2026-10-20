using ProcurementSystem.Core.Enums;

namespace ProcurementSystem.Core.DTOs.BidPackage
{
    public class PackageStatusApprovalRequestDto
    {
        public int Id { get; set; }
        public int BidPackageId { get; set; }
        public string BidPackageCode { get; set; } = string.Empty;
        public string BidPackageName { get; set; } = string.Empty;
        public int RequestedByUserId { get; set; }
        public string RequestedByUserName { get; set; } = string.Empty;
        public BidPackageStatus CurrentStatus { get; set; }
        public string CurrentStatusName { get; set; } = string.Empty;
        public BidPackageStatus TargetStatus { get; set; }
        public string TargetStatusName { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public DateTime? NewDeadline { get; set; }
        public ApprovalStatus Status { get; set; }
        public string StatusName { get; set; } = string.Empty;
        public string? AdminComment { get; set; }
        public int? ReviewedByUserId { get; set; }
        public string? ReviewedByUserName { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
