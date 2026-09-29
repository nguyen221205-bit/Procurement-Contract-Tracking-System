using ProcurementSystem.Core.Enums;

namespace ProcurementSystem.Core.DTOs.Contract
{
    /// <summary>
    /// Thông tin chi tiết hợp đồng, nhà thầu, gói thầu và các mốc thanh toán.
    /// </summary>
    public class ContractDto
    {
        /// <summary>Mã hợp đồng.</summary>
        public int Id { get; set; }
        /// <summary>Mã gói thầu liên kết với hợp đồng.</summary>
        public int BidPackageId { get; set; }
        /// <summary>Mã định danh gói thầu.</summary>
        public string BidPackageCode { get; set; } = string.Empty;
        /// <summary>Tên gói thầu.</summary>
        public string BidPackageName { get; set; } = string.Empty;

        /// <summary>Mã nhà thầu ký hợp đồng.</summary>
        public int ContractorId { get; set; }
        /// <summary>Tên công ty nhà thầu.</summary>
        public string CompanyName { get; set; } = string.Empty;
        /// <summary>Mã số thuế nhà thầu.</summary>
        public string? TaxCode { get; set; }

        /// <summary>Số hợp đồng duy nhất trong hệ thống.</summary>
        public string ContractNumber { get; set; } = string.Empty;
        /// <summary>Tổng giá trị hợp đồng.</summary>
        public decimal Value { get; set; }
        /// <summary>Điều khoản hợp đồng.</summary>
        public string? Terms { get; set; }
        /// <summary>Ngày bắt đầu hiệu lực.</summary>
        public DateTime StartDate { get; set; }
        /// <summary>Ngày kết thúc dự kiến.</summary>
        public DateTime EndDate { get; set; }

        /// <summary>Trạng thái hợp đồng dạng enum.</summary>
        public ContractStatus Status { get; set; }
        /// <summary>Tên trạng thái hợp đồng.</summary>
        public string StatusName { get; set; } = string.Empty;

        /// <summary>Đường dẫn file scan hợp đồng đã ký.</summary>
        public string? ScannedFilePath { get; set; }
        /// <summary>Thời điểm tạo hợp đồng.</summary>
        public DateTime CreatedAt { get; set; }
        /// <summary>Thời điểm cập nhật gần nhất.</summary>
        public DateTime? UpdatedAt { get; set; }

        /// <summary>Tổng tiền đã giải ngân từ các mốc thanh toán đã nghiệm thu.</summary>
        public decimal TotalDisbursedAmount { get; set; }
        /// <summary>Số tiền còn lại chưa giải ngân.</summary>
        public decimal TotalRemainingAmount { get; set; }
        /// <summary>Tỷ lệ giải ngân so với giá trị hợp đồng.</summary>
        public decimal DisbursementRate { get; set; }

        /// <summary>Danh sách mốc thanh toán/nghiệm thu của hợp đồng.</summary>
        public List<ContractMilestoneDto> Milestones { get; set; } = new();
    }
}
