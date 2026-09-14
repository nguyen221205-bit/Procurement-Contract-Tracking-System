using System.ComponentModel.DataAnnotations;

namespace ProcurementSystem.Core.DTOs.Contract
{
    public class CreateContractRequest
    {
        [Required(ErrorMessage = "Phải chỉ định gói thầu.")]
        public int BidPackageId { get; set; }

        [Required(ErrorMessage = "Phải chỉ định nhà thầu trúng thầu.")]
        public int ContractorId { get; set; }

        [Required(ErrorMessage = "Số hợp đồng là bắt buộc.")]
        [MaxLength(50, ErrorMessage = "Số hợp đồng không được vượt quá 50 ký tự.")]
        public string ContractNumber { get; set; } = string.Empty;

        [Range(1, double.MaxValue, ErrorMessage = "Giá trị hợp đồng phải lớn hơn 0.")]
        public decimal Value { get; set; }

        [MaxLength(3000, ErrorMessage = "Điều khoản hợp đồng không được vượt quá 3000 ký tự.")]
        public string? Terms { get; set; }

        [Required(ErrorMessage = "Ngày bắt đầu hợp đồng là bắt buộc.")]
        public DateTime StartDate { get; set; }

        [Required(ErrorMessage = "Ngày kết thúc hợp đồng là bắt buộc.")]
        public DateTime EndDate { get; set; }
    }
}
