using System.ComponentModel.DataAnnotations;

namespace ProcurementSystem.Core.DTOs.BidPackage;

public class PackageEntityOptionDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class ReviewPackageOwnershipRequest
{
    [Range(1, int.MaxValue)] public int ProcuringEntityId { get; set; }
    public int? ExpectedProcuringEntityId { get; set; }
    [Required, MaxLength(1000)] public string EvidenceReference { get; set; } = string.Empty;
    [Required, MaxLength(2000)] public string Note { get; set; } = string.Empty;
}
