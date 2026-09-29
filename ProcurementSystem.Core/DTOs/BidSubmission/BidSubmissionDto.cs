namespace ProcurementSystem.Core.DTOs.BidSubmission
{
    /// <summary>
    /// Thông tin chi tiết hồ sơ dự thầu và các file đính kèm.
    /// </summary>
    public class BidSubmissionDto
    {
        /// <summary>Mã hồ sơ dự thầu.</summary>
        public int Id { get; set; }
        /// <summary>Mã gói thầu.</summary>
        public int BidPackageId { get; set; }
        /// <summary>Mã định danh gói thầu.</summary>
        public string BidPackageCode { get; set; } = string.Empty;
        /// <summary>Tên gói thầu.</summary>
        public string BidPackageName { get; set; } = string.Empty;

        /// <summary>Mã nhà thầu nộp hồ sơ.</summary>
        public int ContractorId { get; set; }
        /// <summary>Tên công ty nhà thầu.</summary>
        public string CompanyName { get; set; } = string.Empty;
        /// <summary>Mã số thuế nhà thầu.</summary>
        public string? TaxCode { get; set; }

        /// <summary>Thời điểm nộp hồ sơ.</summary>
        public DateTime SubmittedAt { get; set; }
        /// <summary>Tổng điểm sau đánh giá.</summary>
        public decimal? TotalScore { get; set; }
        /// <summary>Thứ hạng hồ sơ sau đánh giá.</summary>
        public int? Rank { get; set; }
        /// <summary>Trạng thái xử lý hồ sơ.</summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>Danh sách file tài liệu đính kèm.</summary>
        public List<SubmissionFileDto> Files { get; set; } = new();
    }
}
