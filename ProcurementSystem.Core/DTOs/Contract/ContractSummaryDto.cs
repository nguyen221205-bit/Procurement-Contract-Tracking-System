using ProcurementSystem.Core.Enums;

namespace ProcurementSystem.Core.DTOs.Contract
{
    /// <summary>
    /// Thông tin tóm tắt hợp đồng phục vụ danh sách và tra cứu lịch sử
    /// </summary>
    public class ContractSummaryDto
    {
        /// <summary>Mã định danh duy nhất của hợp đồng</summary>
        public int Id { get; set; }

        /// <summary>Mã định danh gói thầu liên quan</summary>
        public int BidPackageId { get; set; }

        /// <summary>Mã ký hiệu gói thầu</summary>
        public string BidPackageCode { get; set; } = string.Empty;

        /// <summary>Mã định danh nhà thầu ký kết</summary>
        public int ContractorId { get; set; }

        /// <summary>Tên doanh nghiệp nhà thầu</summary>
        public string CompanyName { get; set; } = string.Empty;

        /// <summary>Số hiệu hợp đồng kinh tế</summary>
        public string ContractNumber { get; set; } = string.Empty;

        /// <summary>Tổng giá trị hợp đồng (VNĐ)</summary>
        public decimal Value { get; set; }

        /// <summary>Trạng thái hợp đồng dạng enum</summary>
        public ContractStatus Status { get; set; }

        /// <summary>Tên hiển thị trạng thái hợp đồng</summary>
        public string StatusName { get; set; } = string.Empty;

        /// <summary>Ngày bắt đầu hiệu lực</summary>
        public DateTime StartDate { get; set; }

        /// <summary>Ngày kết thúc hợp đồng</summary>
        public DateTime EndDate { get; set; }

        /// <summary>Số lượng mốc thanh toán nghiệm thu</summary>
        public int MilestoneCount { get; set; }

        /// <summary>Tổng số tiền đã giải ngân từ các mốc nghiệm thu thành công</summary>
        public decimal TotalDisbursedAmount { get; set; }

        /// <summary>Số tiền còn lại chưa giải ngân</summary>
        public decimal TotalRemainingAmount { get; set; }
    }
}
