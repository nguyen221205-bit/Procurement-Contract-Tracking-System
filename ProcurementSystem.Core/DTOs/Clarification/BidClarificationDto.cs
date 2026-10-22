using ProcurementSystem.Core.Enums;

namespace ProcurementSystem.Core.DTOs.Clarification
{
    public class BidClarificationDto
    {
        public int Id { get; set; }
        public int BidPackageId { get; set; }
        public string BidPackageCode { get; set; } = string.Empty;
        public string BidPackageName { get; set; } = string.Empty;
        public int? BidSubmissionId { get; set; }
        public string? ContractorName { get; set; }
        public ClarificationType Type { get; set; }
        public string TypeName { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string QuestionContent { get; set; } = string.Empty;
        public string? QuestionAttachmentUrl { get; set; }
        public int AskedByUserId { get; set; }
        public string AskedByUserName { get; set; } = string.Empty;
        public DateTime AskedAt { get; set; }
        public DateTime? Deadline { get; set; }
        public string? AnswerContent { get; set; }
        public string? AnswerAttachmentUrl { get; set; }
        public int? AnsweredByUserId { get; set; }
        public string? AnsweredByUserName { get; set; }
        public DateTime? AnsweredAt { get; set; }
        public ClarificationStatus Status { get; set; }
        public string StatusName { get; set; } = string.Empty;
    }
}
