using ProcurementSystem.Core.Enums;

namespace ProcurementSystem.Core.DTOs.BidPackage
{
    public class BidPackageSummaryDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public BidPackageType Type { get; set; }
        public decimal Budget { get; set; }
        public DateTime Deadline { get; set; }
        public BidPackageStatus Status { get; set; }
        public string? CreatedByName { get; set; }
        public DateTime CreatedAt { get; set; }
        public int DocumentsCount { get; set; }
        public int SubmissionsCount { get; set; }
    }
}
