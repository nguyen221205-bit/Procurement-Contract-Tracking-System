using System.ComponentModel.DataAnnotations;

namespace ProcurementSystem.Core.DTOs.Contract
{
    /// <summary>
    /// Yêu cầu tạo mới hợp đồng kinh tế từ kết quả trúng thầu
    /// </summary>
    public class CreateContractRequest
    {
        /// <summary>
        /// Mã định danh gói thầu đã được phê duyệt kết quả trao thầu
        /// </summary>
        [Required(ErrorMessage = "Phải chỉ định gói thầu.")]
        public int BidPackageId { get; set; }

        /// <summary>
        /// Mã định danh nhà thầu đã được phê duyệt trúng thầu (Selected)
        /// </summary>
        [Required(ErrorMessage = "Phải chỉ định nhà thầu trúng thầu.")]
        public int ContractorId { get; set; }

        /// <summary>
        /// Số hiệu hợp đồng (Nếu để trống hệ thống sẽ tự sinh theo quy tắc: HD-{Năm}-{MãGói}-{STT})
        /// </summary>
        [Required(ErrorMessage = "Số hợp đồng là bắt buộc.")]
        [MaxLength(50, ErrorMessage = "Số hợp đồng không được vượt quá 50 ký tự.")]
        public string ContractNumber { get; set; } = string.Empty;

        /// <summary>
        /// Giá trị hợp đồng (VNĐ), không được vượt quá ngân sách dự toán của gói thầu
        /// </summary>
        [Range(1, double.MaxValue, ErrorMessage = "Giá trị hợp đồng phải lớn hơn 0.")]
        public decimal Value { get; set; }

        /// <summary>
        /// Các điều khoản, thỏa thuận hoặc ghi chú chi tiết của hợp đồng
        /// </summary>
        [MaxLength(3000, ErrorMessage = "Điều khoản hợp đồng không được vượt quá 3000 ký tự.")]
        public string? Terms { get; set; }

        /// <summary>
        /// Ngày bắt đầu có hiệu lực của hợp đồng
        /// </summary>
        [Required(ErrorMessage = "Ngày bắt đầu hợp đồng là bắt buộc.")]
        public DateTime StartDate { get; set; }

        /// <summary>
        /// Ngày kết thúc hiệu lực của hợp đồng (phải sau ngày bắt đầu)
        /// </summary>
        [Required(ErrorMessage = "Ngày kết thúc hợp đồng là bắt buộc.")]
        public DateTime EndDate { get; set; }
    }
}
