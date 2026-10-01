namespace ProcurementSystem.Core.DTOs.BidPackage
{
    public class PackageEvaluatorDto
    {
        public int EvaluatorId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public DateTime AssignedAt { get; set; }
        public int? AssignedBy { get; set; }
        public string? AssignedByName { get; set; }
        public bool HasSubmittedScores { get; set; }
    }
}
