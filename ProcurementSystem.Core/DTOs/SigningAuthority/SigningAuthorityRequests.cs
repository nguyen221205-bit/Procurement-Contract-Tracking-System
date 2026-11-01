using System.ComponentModel.DataAnnotations;
using ProcurementSystem.Core.Enums;

namespace ProcurementSystem.Core.DTOs.SigningAuthority
{
    public class RequestSigningAuthorityRequest
    {
        [Required(ErrorMessage = "Vui lòng chọn nhân sự được phân công.")]
        public int UserId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn loại thẩm quyền ký duyệt.")]
        public SigningAuthorityType AuthorityType { get; set; } = SigningAuthorityType.DisbursementApproval;

        [MaxLength(100, ErrorMessage = "Phạm vi thẩm quyền tối đa 100 ký tự.")]
        public string Scope { get; set; } = "EntityWide";

        [Required(ErrorMessage = "Vui lòng chọn ngày bắt đầu hiệu lực.")]
        public DateTime ValidFrom { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn ngày kết thúc hiệu lực.")]
        public DateTime ValidTo { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số quyết định hoặc văn bản ủy quyền.")]
        [MaxLength(100, ErrorMessage = "Số quyết định tối đa 100 ký tự.")]
        public string DecisionNumber { get; set; } = string.Empty;

        [MaxLength(500, ErrorMessage = "Đường dẫn tài liệu tối đa 500 ký tự.")]
        public string? DocumentUrl { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập căn cứ pháp lý hoặc nội dung ủy quyền.")]
        [MaxLength(500, ErrorMessage = "Căn cứ pháp lý tối đa 500 ký tự.")]
        public string LegalBasis { get; set; } = string.Empty;
    }

    public class VerifySigningAuthorityRequest
    {
        public bool IsApproved { get; set; }

        [MaxLength(1000, ErrorMessage = "Ghi chú thẩm định tối đa 1000 ký tự.")]
        public string? VerificationNotes { get; set; }

        public DateTime? ValidFrom { get; set; }
        public DateTime? ValidTo { get; set; }
    }

    public class RevokeSigningAuthorityRequest
    {
        [Required(ErrorMessage = "Vui lòng nêu rõ lý do thu hồi thẩm quyền.")]
        [MaxLength(500, ErrorMessage = "Lý do thu hồi tối đa 500 ký tự.")]
        public string RevocationReason { get; set; } = string.Empty;
    }
}
