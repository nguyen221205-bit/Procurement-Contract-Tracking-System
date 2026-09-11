using ProcurementSystem.Core.Enums;

namespace ProcurementSystem.Core.DTOs.BidSubmission
{
    public class SubmissionFileDto
    {
        public int Id { get; set; }
        public SubmissionFileType FileType { get; set; }
        public string FileTypeName { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public DateTime UploadedAt { get; set; }
    }
}
