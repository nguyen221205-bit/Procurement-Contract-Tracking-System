using ProcurementSystem.Core.Interfaces;
using ProcurementSystem.Infrastructure.Utilities;

namespace ProcurementSystem.Infrastructure.Services
{
    /// <summary>
    /// Dịch vụ chuyên biệt sinh tài liệu PDF hành chính mẫu khi tệp vật lý trong môi trường thử nghiệm bị thiếu (SRP).
    /// </summary>
    public class DocumentSampleFallbackProvider : IDocumentSampleFallbackProvider
    {
        public static readonly DocumentSampleFallbackProvider Instance = new();

        public bool NeedsFallbackGeneration(string physicalPath)
        {
            if (!File.Exists(physicalPath))
            {
                return true;
            }

            try
            {
                var fileInfo = new FileInfo(physicalPath);
                if (fileInfo.Length > 0 && fileInfo.Length < 2000)
                {
                    var headerBytes = new byte[Math.Min(1024, (int)fileInfo.Length)];
                    using (var fs = File.OpenRead(physicalPath))
                    {
                        _ = fs.Read(headerBytes, 0, headerBytes.Length);
                    }

                    var contentSample = System.Text.Encoding.ASCII.GetString(headerBytes);
                    if (contentSample.Contains("Project Paused") || contentSample.Contains("HE THONG DAU THAU DIEN TU"))
                    {
                        return true;
                    }
                }
            }
            catch
            {
                // Không chặn luồng nếu không đọc được file
            }

            return false;
        }

        public byte[] GenerateSampleDocumentBytes(string pathOrKey, string originalFileName)
        {
            var lowerKey = (pathOrKey ?? string.Empty).ToLowerInvariant();

            if (lowerKey.Contains("procuring") || lowerKey.Contains("establishment") || lowerKey.Contains("decision"))
            {
                return DocumentSampleGenerator.GenerateEstablishmentDecisionPdf(
                    "Ban Quan ly Du an Dau tu va Mua sam Tap trung",
                    "0100109106",
                    "Nguyen Thi Chuyen Vien Mua Sam");
            }

            if (lowerKey.Contains("guarantee") || lowerKey.Contains("bao-lanh"))
            {
                return DocumentSampleGenerator.GenerateGuaranteeDocumentPdf(
                    "BL-VCB-2026-0899",
                    "Ngan hang TMCP Ngoai thuong Viet Nam (Vietcombank)",
                    117500000m,
                    "HD-2026-PKG-SEC-2026-001-019",
                    "CONG TY CO PHAN PHAN MEM ABC");
            }

            if (lowerKey.Contains("contractor") || lowerKey.Contains("license") || lowerKey.Contains("gpkd"))
            {
                return DocumentSampleGenerator.GenerateBusinessLicensePdf(
                    "CONG TY CO PHAN CONG NGHE VA PHAN MEM VIET NAM",
                    "0100150619",
                    "Nguyen Van Dai Dien",
                    "Ha Noi, Viet Nam");
            }

            if (lowerKey.Contains("evaluator") || lowerKey.Contains("certificate") || lowerKey.Contains("chung-chi"))
            {
                return DocumentSampleGenerator.GenerateEvaluatorCertificatePdf(
                    "Nguyen Thi Chuyen Vien Mua Sam",
                    "Chung chi hanh nghe hoat dong dau thau chuyen nghiep");
            }

            var contractTitle = string.IsNullOrWhiteSpace(originalFileName)
                ? "HD-2026-PKG-STANDARD"
                : Path.GetFileNameWithoutExtension(originalFileName);

            return DocumentSampleGenerator.GenerateContractScannedPdf(
                contractTitle,
                "Goi thau Mua sam Trang thiet bi va Dich vu Cong nghe",
                "CONG TY CO PHAN PHAN MEM ABC",
                2350000000m);
        }

        public void EnsureLocalSamplePdfIfNeeded(string physicalPath, string originalFileName)
        {
            if (!NeedsFallbackGeneration(physicalPath))
            {
                return;
            }

            try
            {
                var dir = Path.GetDirectoryName(physicalPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                var validPdfBytes = GenerateSampleDocumentBytes(physicalPath, originalFileName);
                File.WriteAllBytes(physicalPath, validPdfBytes);
            }
            catch
            {
                // Không chặn luồng nếu không ghi đè được
            }
        }
    }
}
