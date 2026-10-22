using System;
using System.Collections.Generic;

namespace ProcurementSystem.Core.DTOs.Evaluation
{
    public class BidEvaluationReportDto
    {
        // 1. Thông tin văn bản hành chính
        public string ReportNumber { get; set; } = string.Empty;
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
        public string Location { get; set; } = "Hà Nội";

        // 2. Thông tin gói thầu
        public int PackageId { get; set; }
        public string PackageCode { get; set; } = string.Empty;
        public string PackageName { get; set; } = string.Empty;
        public decimal Budget { get; set; }
        public string PackageStatus { get; set; } = string.Empty;
        public DateTime Deadline { get; set; }
        public string ProcuringEntityName { get; set; } = string.Empty;
        public string? ProcuringEntityTaxCode { get; set; }
        public string BiddingMethod { get; set; } = "Đấu thầu rộng rãi qua mạng";
        public string BiddingProcedure { get; set; } = "Một giai đoạn một túi hồ sơ";

        // 3. Danh sách Tổ chuyên gia xét thầu
        public List<EvaluationCommitteeMemberDto> CommitteeMembers { get; set; } = new();

        // 4. Danh sách Tiêu chuẩn đánh giá
        public List<EvaluationCriteriaDto> Criteria { get; set; } = new();

        // 5. Thống kê & Bảng xếp hạng HSDT
        public int TotalSubmissions { get; set; }
        public int EvaluatedSubmissions { get; set; }
        public List<EvaluationReportRankingItemDto> Rankings { get; set; } = new();

        // 6. Kết luận & Đề xuất trao thầu
        public bool IsFinalized { get; set; }
        public string? WinningContractorName { get; set; }
        public string? WinningContractorTaxCode { get; set; }
        public decimal? WinningBidPrice { get; set; }
        public decimal? WinningTotalScore { get; set; }
        public string? DecisionReason { get; set; }
        public string ConclusionSummary { get; set; } = string.Empty;
    }

    public class EvaluationCommitteeMemberDto
    {
        public int EvaluatorId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string CommitteeRole { get; set; } = "Thành viên Tổ chuyên gia";
        public bool HasCompletedScoring { get; set; }
    }

    public class EvaluationReportRankingItemDto
    {
        public int SubmissionId { get; set; }
        public int ContractorId { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public string? TaxCode { get; set; }
        public decimal? BidPrice { get; set; }
        public decimal? PriceSavings { get; set; }
        public decimal? PriceSavingsPercent { get; set; }
        public decimal? TotalScore { get; set; }
        public decimal? TechnicalScore { get; set; }
        public decimal? PriceScore { get; set; }
        public int? Rank { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? DisqualificationReason { get; set; }
        public DateTime SubmittedAt { get; set; }
        public List<EvaluationScoreDto> Scores { get; set; } = new();
    }
}
