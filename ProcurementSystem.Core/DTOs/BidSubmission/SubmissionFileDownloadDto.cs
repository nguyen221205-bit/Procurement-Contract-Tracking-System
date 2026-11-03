namespace ProcurementSystem.Core.DTOs.BidSubmission
{
    public class SubmissionFileDownloadDto
    {
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = "application/octet-stream";
        public string? PhysicalPath { get; set; }
        public bool IsRedirect { get; set; }
        public string? RedirectUrl { get; set; }
        public Stream? FileStream { get; set; }
        public bool IsStream => FileStream != null;
    }
}
