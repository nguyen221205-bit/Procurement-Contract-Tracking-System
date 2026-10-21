using System.ComponentModel.DataAnnotations;

namespace ProcurementSystem.Core.DTOs.Contract
{
    public class ContractorEvaluationDto
    {
        public int Id { get; set; }
        public int ContractId { get; set; }
        public string ContractNumber { get; set; } = string.Empty;
        public string? BidPackageName { get; set; }
        public int ContractorId { get; set; }
        public string ContractorName { get; set; } = string.Empty;
        public int EvaluatorUserId { get; set; }
        public string EvaluatorName { get; set; } = string.Empty;
        public decimal QualityScore { get; set; }
        public decimal ScheduleScore { get; set; }
        public decimal ComplianceScore { get; set; }
        public decimal TotalScore { get; set; }
        public string RatingGrade { get; set; } = "B";
        public string? Comments { get; set; }
        public DateTime EvaluatedAt { get; set; }
    }

    public class CreateContractorEvaluationRequest
    {
        [Range(0, 40, ErrorMessage = "Điểm chất lượng công trình / sản phẩm phải từ 0 đến 40")]
        public decimal QualityScore { get; set; }

        [Range(0, 40, ErrorMessage = "Điểm tiến độ thực hiện phải từ 0 đến 40")]
        public decimal ScheduleScore { get; set; }

        [Range(0, 20, ErrorMessage = "Điểm an toàn & bảo hành phải từ 0 đến 20")]
        public decimal ComplianceScore { get; set; }

        [MaxLength(2000)]
        public string? Comments { get; set; }
    }

    public class ContractorCreditProfileDto
    {
        public int ContractorId { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public string? TaxCode { get; set; }
        public decimal Rating { get; set; }
        public string RatingGrade => Rating switch
        {
            >= 85 => "A",
            >= 70 => "B",
            >= 50 => "C",
            _ => "D"
        };
        public string RatingGradeName => Rating switch
        {
            >= 85 => "Hạng A - Xuất sắc",
            >= 70 => "Hạng B - Đạt yêu cầu",
            >= 50 => "Hạng C - Cần cải thiện",
            _ => "Hạng D - Cảnh báo rủi ro"
        };
        public int TotalContractsCount { get; set; }
        public int CompletedContractsCount { get; set; }
        public int TerminatedContractsCount { get; set; }
        public decimal AverageQualityScore { get; set; }
        public decimal AverageScheduleScore { get; set; }
        public decimal AverageComplianceScore { get; set; }
        public List<ContractorEvaluationDto> Evaluations { get; set; } = new List<ContractorEvaluationDto>();
    }
}
