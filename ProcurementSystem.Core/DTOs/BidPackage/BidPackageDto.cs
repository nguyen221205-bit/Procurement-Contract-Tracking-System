using ProcurementSystem.Core.Enums;

namespace ProcurementSystem.Core.DTOs.BidPackage
{
    public class BidPackageDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public BidPackageType Type { get; set; }
        public decimal Budget { get; set; }
        public DateTime Deadline { get; set; }
        public BidPackageStatus Status { get; set; }
        public string? Description { get; set; }
        public int CreatedBy { get; set; }
        public string? CreatedByName { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public List<BidDocumentDto> BidDocuments { get; set; } = new();
        public int SubmissionsCount { get; set; }
    }
}
