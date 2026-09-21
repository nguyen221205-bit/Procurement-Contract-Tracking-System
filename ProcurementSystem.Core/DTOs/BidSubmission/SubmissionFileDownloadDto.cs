namespace ProcurementSystem.Core.DTOs.BidSubmission
{
    public class SubmissionFileDownloadDto
    {
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = "application/octet-stream";
        public string PhysicalPath { get; set; } = string.Empty;
    }
}
