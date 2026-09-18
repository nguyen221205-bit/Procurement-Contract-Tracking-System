namespace ProcurementSystem.Core.DTOs.Report
{
    public class ProcurementDashboardDto
    {
        // ==== 1. THỐNG KÊ GÓI THẦU ====
        public int TotalPackages { get; set; }
        public int OpenPackages { get; set; }
        public int ClosedPackages { get; set; }
        public int EvaluatingPackages { get; set; }
        public int ContractedPackages { get; set; }

        // ==== 2. THỐNG KÊ HỒ SƠ & DOANH NGHIỆP ====
        public int TotalSubmissions { get; set; }
        public int EvaluatedSubmissions { get; set; }
        public int SelectedSubmissions { get; set; }
        public int TotalContractors { get; set; }

        // ==== 3. THỐNG KÊ TÀI CHÍNH & HIỆU QUẢ ĐẤU THẦU ====
        public decimal TotalEstimatedBudget { get; set; }
        public decimal TotalContractValue { get; set; }
        public decimal TotalSavings { get; set; }
        public decimal SavingsRate { get; set; }

        // ==== 4. TIẾN ĐỘ THỰC HIỆN HỢP ĐỒNG & GIẢI NGÂN ====
        public int TotalContracts { get; set; }
        public int DraftContracts { get; set; }
        public int ActiveContracts { get; set; }
        public int CompletedContracts { get; set; }
        public int TerminatedContracts { get; set; }

        // ==== 5. TIẾN ĐỘ MỐC THANH TOÁN (MILESTONES) ====
        public int TotalMilestones { get; set; }
        public int CompletedMilestones { get; set; }
        public int PendingMilestones { get; set; }
        public decimal TotalDisbursedAmount { get; set; }
        public decimal TotalRemainingAmount { get; set; }
        public decimal DisbursementRate { get; set; }

        // ==== 6. DANH SÁCH HỢP ĐỒNG GẦN ĐÂY ====
        public List<RecentContractItemDto> RecentContracts { get; set; } = new();
    }

    public class RecentContractItemDto
    {
        public int ContractId { get; set; }
        public string ContractNumber { get; set; } = string.Empty;
        public string BidPackageCode { get; set; } = string.Empty;
        public string BidPackageName { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public decimal PackageBudget { get; set; }
        public decimal ContractValue { get; set; }
        public decimal Savings { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}
