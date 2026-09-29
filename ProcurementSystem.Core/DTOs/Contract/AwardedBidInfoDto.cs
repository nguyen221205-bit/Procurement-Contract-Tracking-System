namespace ProcurementSystem.Core.DTOs.Contract
{
    /// <summary>
    /// Dữ liệu bàn giao từ kết quả trúng thầu sang phân hệ Hợp đồng (Pre-fill)
    /// </summary>
    public class AwardedBidInfoDto
    {
        // ==== Thông tin Gói thầu ====
        public int BidPackageId { get; set; }
        public string PackageCode { get; set; } = string.Empty;
        public string PackageName { get; set; } = string.Empty;

        /// <summary>Dự toán được duyệt của gói thầu (Budget)</summary>
        public decimal EstimatedBudget { get; set; }

        // ==== Thông tin Nhà thầu trúng thầu ====
        public int ContractorId { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public string? TaxCode { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }

        // ==== Thông tin Hồ sơ trúng thầu ====
        public int SubmissionId { get; set; }

        /// <summary>Điểm tổng hợp theo trọng số của hồ sơ trúng thầu</summary>
        public decimal? TotalScore { get; set; }

        public int? Rank { get; set; }

        /// <summary>Giá trị đề xuất điền vào hợp đồng = Bằng với dự toán gói thầu nếu không có giá thầu riêng</summary>
        public decimal SuggestedContractValue { get; set; }

        // ==== Trạng thái liên thông ====
        /// <summary>Đã có hồ sơ trúng thầu được phê duyệt (Status == "Selected") chưa</summary>
        public bool IsAwarded { get; set; }

        /// <summary>Gói thầu đã được lập hợp đồng chưa</summary>
        public bool HasContract { get; set; }

        /// <summary>Mã hợp đồng nếu đã tồn tại</summary>
        public int? ExistingContractId { get; set; }

        /// <summary>Gợi ý số hợp đồng tự động theo chuẩn HD-{Năm}-{MãGóiThầu}-{SốThứTự}</summary>
        public string SuggestedContractNumber { get; set; } = string.Empty;
    }
}
