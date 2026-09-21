namespace ProcurementSystem.Core.DTOs.Contract
{
    public class ProgressUpdateDto
    {
        public int Id { get; set; }
        public int ContractId { get; set; }
        public int WeekNumber { get; set; }
        public decimal CompletionPercent { get; set; }
        public string? Note { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
