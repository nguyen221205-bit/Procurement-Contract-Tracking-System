namespace ProcurementSystem.Core.DTOs.Report
{
    public class ContractorPerformanceReportDto
    {
        public int TotalContractors { get; set; }
        public int ActiveBiddingContractors { get; set; }
        public List<ContractorPerformanceItemDto> Contractors { get; set; } = new();
    }

    public class ContractorPerformanceItemDto
    {
        public int ContractorId { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public string TaxCode { get; set; } = string.Empty;
        public decimal Rating { get; set; }
        public string VerificationStatus { get; set; } = string.Empty;

        public int TotalSubmissions { get; set; }
        public int WonSubmissions { get; set; }
        public int LostSubmissions { get; set; }
        public int DisqualifiedSubmissions { get; set; }
        public decimal WinRate { get; set; }
        public decimal TotalAwardedValue { get; set; }
    }
}
