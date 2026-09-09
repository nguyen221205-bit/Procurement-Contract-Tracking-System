using iText.Kernel.Pdf;
using iText.Signatures;
using ProcurementSystem.Core.Interfaces;

namespace ProcurementSystem.Infrastructure.Services
{
    public class PdfSecurityService : IPdfSecurityService
    {
        public PdfSignatureResult InspectPdfSignature(Stream pdfStream)
        {
            var result = new PdfSignatureResult();
            try
            {
                using var reader = new PdfReader(pdfStream);
                using var pdfDoc = new PdfDocument(reader);
                var signUtil = new SignatureUtil(pdfDoc);
                var names = signUtil.GetSignatureNames();

                if (names == null || names.Count == 0)
                {
                    result.IsSigned = false;
                    result.SummaryMessage = "Bản scan thông thường (Không có chữ ký số điện tử).";
                    return result;
                }

                // Có chữ ký số -> kiểm tra tính toàn vẹn của chữ ký đầu tiên
                string firstSign = names[0];
                PdfPKCS7 pkcs7 = signUtil.ReadSignatureData(firstSign);
                bool isValid = pkcs7.VerifySignatureIntegrityAndAuthenticity();

                result.IsSigned = true;
                result.IsIntegrityValid = isValid;
                result.SignDate = pkcs7.GetSignDate();
                
                var cert = pkcs7.GetSigningCertificate();
                result.SignerName = cert.GetSubjectDN().ToString();
                result.SummaryMessage = isValid
                    ? $"Chữ ký số hợp lệ và toàn vẹn. Đơn vị ký: {result.SignerName}"
                    : "CẢNH BÁO: Chữ ký số không hợp lệ hoặc tài liệu đã bị can thiệp/sửa đổi sau khi ký!";
            }
            catch (Exception ex)
            {
                result.IsSigned = false;
                result.SummaryMessage = "Không thể phân tích chữ ký: " + ex.Message;
            }

            return result;
        }
    }
}
