using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace ProcurementSystem.Core.DTOs.EvaluatorProposal
{
    /// <summary>
    /// DTO yêu cầu đề xuất Giám khảo / Thành viên Tổ chuyên gia từ Bên mời thầu (Procurement)
    /// </summary>
    public class CreateEvaluatorProposalRequest
    {
        [Required(ErrorMessage = "Họ và tên giám khảo là bắt buộc.")]
        [MaxLength(100, ErrorMessage = "Họ và tên không được vượt quá 100 ký tự.")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email giám khảo là bắt buộc.")]
        [EmailAddress(ErrorMessage = "Địa chỉ email không đúng định dạng.")]
        [MaxLength(150, ErrorMessage = "Email không được vượt quá 150 ký tự.")]
        public string Email { get; set; } = string.Empty;

        [MaxLength(20, ErrorMessage = "Số điện thoại không được vượt quá 20 ký tự.")]
        public string? Phone { get; set; }

        [MaxLength(200, ErrorMessage = "Lĩnh vực chuyên môn không được vượt quá 200 ký tự.")]
        public string? Specialization { get; set; }

        [MaxLength(250, ErrorMessage = "Đơn vị công tác không được vượt quá 250 ký tự.")]
        public string? Workplace { get; set; }

        [Range(0, 70, ErrorMessage = "Số năm kinh nghiệm không hợp lệ.")]
        public int? ExperienceYears { get; set; }

        [MaxLength(200, ErrorMessage = "Tên chứng chỉ không được vượt quá 200 ký tự.")]
        public string? CertificateName { get; set; }

        /// <summary>
        /// Tệp Chứng chỉ nghiệp vụ chuyên môn về đấu thầu hoặc bằng cấp chuyên môn (PDF, DOCX, Ảnh PNG/JPG)
        /// </summary>
        public IFormFile? CertificateFile { get; set; }

        [MaxLength(1000, ErrorMessage = "Ghi chú đề xuất không được vượt quá 1000 ký tự.")]
        public string? Notes { get; set; }
    }
}
