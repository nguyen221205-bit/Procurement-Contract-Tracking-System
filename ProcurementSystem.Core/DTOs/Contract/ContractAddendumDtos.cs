using System.ComponentModel.DataAnnotations;
using ProcurementSystem.Core.Enums;

namespace ProcurementSystem.Core.DTOs.Contract
{
    public class ContractAddendumDto
    {
        public int Id { get; set; }
        public int ContractId { get; set; }
        public string AddendumNumber { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public AddendumType Type { get; set; }
        public string TypeName => Type switch
        {
            AddendumType.TimelineExtension => "Gia hạn thời gian hoàn thành",
            AddendumType.ValueAdjustment => "Điều chỉnh giá trị hợp đồng",
            AddendumType.ScopeChange => "Thay đổi phạm vi / khối lượng",
            AddendumType.GeneralTerms => "Điều chỉnh điều khoản chung",
            _ => "Khác"
        };
        public decimal AdditionalValue { get; set; }
        public DateTime? AdjustedEndDate { get; set; }
        public int ExtensionDays { get; set; }
        public string? Reason { get; set; }
        public DateTime SignedDate { get; set; }
        public string? ScannedDocumentPath { get; set; }
        public AddendumStatus Status { get; set; }
        public string StatusName => Status switch
        {
            AddendumStatus.Draft => "Dự thảo",
            AddendumStatus.Pending => "Chờ phê duyệt",
            AddendumStatus.Approved => "Đã phê duyệt / Có hiệu lực",
            AddendumStatus.Rejected => "Bị từ chối",
            _ => "Không xác định"
        };
        public int CreatedBy { get; set; }
        public string? CreatedByName { get; set; }
        public DateTime CreatedAt { get; set; }
        public int? ApprovedBy { get; set; }
        public string? ApprovedByName { get; set; }
        public DateTime? ApprovedAt { get; set; }
    }

    public class CreateContractAddendumRequest
    {
        [Required(ErrorMessage = "Số phụ lục hợp đồng không được để trống")]
        [MaxLength(100)]
        public string AddendumNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tiêu đề phụ lục không được để trống")]
        [MaxLength(300)]
        public string Title { get; set; } = string.Empty;

        public AddendumType Type { get; set; } = AddendumType.TimelineExtension;

        public decimal AdditionalValue { get; set; } = 0;

        public DateTime? AdjustedEndDate { get; set; }

        public int ExtensionDays { get; set; } = 0;

        [MaxLength(2000)]
        public string? Reason { get; set; }

        [Required(ErrorMessage = "Ngày ký phụ lục không được để trống")]
        public DateTime SignedDate { get; set; }
    }

    public class ApproveContractAddendumRequest
    {
        public bool IsApproved { get; set; } = true;

        [MaxLength(1000)]
        public string? Notes { get; set; }
    }
}
