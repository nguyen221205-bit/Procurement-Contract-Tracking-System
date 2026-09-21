using ProcurementSystem.Core.Enums;

namespace ProcurementSystem.Core.DTOs.Contract
{
    public class MilestoneAcceptanceDto
    {
        public int Id { get; set; }
        public int ContractId { get; set; }
        public int? MilestoneId { get; set; }
        public int ApprovedBy { get; set; }
        public string ApproverName { get; set; } = string.Empty;
        public DateTime? ApprovedAt { get; set; }
        public AcceptanceStatus Status { get; set; }
        public string StatusName { get; set; } = string.Empty;
        public string? Note { get; set; }
        public MilestoneStatus MilestoneStatus { get; set; }
        public string MilestoneStatusName { get; set; } = string.Empty;
    }
}
