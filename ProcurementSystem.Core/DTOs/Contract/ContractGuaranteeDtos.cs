using System.ComponentModel.DataAnnotations;
using ProcurementSystem.Core.Enums;

namespace ProcurementSystem.Core.DTOs.Contract
{
    public class ContractGuaranteeDto
    {
        public int Id { get; set; }
        public int ContractId { get; set; }
        public string GuaranteeNumber { get; set; } = string.Empty;
        public string BankName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public decimal Percentage { get; set; }
        public DateTime EffectiveDate { get; set; }
        public DateTime ExpiryDate { get; set; }
        public string? ScannedDocumentPath { get; set; }
        public GuaranteeStatus Status { get; set; }
        public string StatusName => Status switch
        {
            GuaranteeStatus.PendingApproval => "Chờ duyệt",
            GuaranteeStatus.Valid => "Đang có hiệu lực",
            GuaranteeStatus.Released => "Đã giải tỏa / hoàn trả",
            GuaranteeStatus.Claimed => "Bị tịch thu",
            GuaranteeStatus.Expired => "Đã hết hạn",
            _ => "Không xác định"
        };
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsNearExpiry => Status == GuaranteeStatus.Valid && ExpiryDate <= DateTime.UtcNow.AddDays(30) && ExpiryDate >= DateTime.UtcNow;
    }

    public class CreateContractGuaranteeRequest
    {
        [Required(ErrorMessage = "Số chứng thư bảo lãnh không được để trống")]
        [MaxLength(100)]
        public string GuaranteeNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tên ngân hàng phát hành không được để trống")]
        [MaxLength(200)]
        public string BankName { get; set; } = string.Empty;

        [Range(0.01, double.MaxValue, ErrorMessage = "Số tiền bảo lãnh phải lớn hơn 0")]
        public decimal Amount { get; set; }

        [Range(0, 100, ErrorMessage = "Tỷ lệ phần trăm bảo lãnh phải từ 0% đến 100%")]
        public decimal Percentage { get; set; }

        [Required(ErrorMessage = "Ngày có hiệu lực không được để trống")]
        public DateTime EffectiveDate { get; set; }

        [Required(ErrorMessage = "Ngày hết hạn bảo lãnh không được để trống")]
        public DateTime ExpiryDate { get; set; }

        [MaxLength(1000)]
        public string? Notes { get; set; }
    }

    public class UpdateGuaranteeStatusRequest
    {
        [Required]
        public GuaranteeStatus Status { get; set; }

        [MaxLength(1000)]
        public string? Notes { get; set; }
    }
}
