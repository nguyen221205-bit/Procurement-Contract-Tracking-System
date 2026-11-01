namespace ProcurementSystem.Core.DTOs.Contract;

public class ContractListSummaryDto
{
    public int TotalContracts { get; set; }
    public int ActiveContracts { get; set; }
    public int DraftContracts { get; set; }
    public int CompletedContracts { get; set; }
    public decimal TotalValue { get; set; }
    public decimal TotalDisbursedAmount { get; set; }
    public decimal DisbursementRate { get; set; }
}
