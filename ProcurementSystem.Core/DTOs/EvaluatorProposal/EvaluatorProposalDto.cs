namespace ProcurementSystem.Core.DTOs.EvaluatorProposal
{
    /// <summary>
    /// DTO thông tin chi tiết đề xuất Giám khảo / Thành viên Tổ chuyên gia
    /// </summary>
    public class EvaluatorProposalDto
    {
        public int Id { get; set; }

        public int ProposerUserId { get; set; }
        public string ProposerName { get; set; } = string.Empty;
        public string ProposerEmail { get; set; } = string.Empty;

        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Specialization { get; set; }
        public string? Workplace { get; set; }
        public int? ExperienceYears { get; set; }

        public string? CertificateName { get; set; }
        public string? CertificateFile { get; set; }

        public string? Notes { get; set; }
        public string Status { get; set; } = "Pending";
        public string? AdminNotes { get; set; }

        public int? ReviewedByUserId { get; set; }
        public string? ReviewedByName { get; set; }
        public DateTime? ReviewedAt { get; set; }

        public int? CreatedUserId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
