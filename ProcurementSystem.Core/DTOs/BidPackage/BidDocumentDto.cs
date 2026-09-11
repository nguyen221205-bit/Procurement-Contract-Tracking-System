namespace ProcurementSystem.Core.DTOs.BidPackage
{
    public class BidDocumentDto
    {
        public int Id { get; set; }
        public int BidPackageId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public DateTime UploadedAt { get; set; }
    }
}
