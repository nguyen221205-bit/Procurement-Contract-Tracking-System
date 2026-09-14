using ProcurementSystem.Core.Enums;

namespace ProcurementSystem.Core.DTOs.Contract
{
    public class ContractMilestoneDto
    {
        public int Id { get; set; }
        public int ContractId { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime DueDate { get; set; }
        public decimal Amount { get; set; }
        public MilestoneStatus Status { get; set; }
        public string StatusName { get; set; } = string.Empty;
    }
}
