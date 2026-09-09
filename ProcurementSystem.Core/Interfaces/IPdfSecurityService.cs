namespace ProcurementSystem.Core.Interfaces
{
    public class PdfSignatureResult
    {
        public bool IsSigned { get; set; }
        public bool IsIntegrityValid { get; set; }
        public string? SignerName { get; set; }
        public DateTime? SignDate { get; set; }
        public string SummaryMessage { get; set; } = string.Empty;
    }

    public interface IPdfSecurityService
    {
        PdfSignatureResult InspectPdfSignature(Stream pdfStream);
    }
}
