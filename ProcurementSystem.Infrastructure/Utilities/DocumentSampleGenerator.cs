using iText.IO.Font.Constants;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Borders;
using iText.Layout.Element;
using iText.Layout.Properties;

namespace ProcurementSystem.Infrastructure.Utilities
{
    /// <summary>
    /// Bộ tiện ích sinh văn bản hành chính & chứng từ đấu thầu tiêu chuẩn định dạng PDF (iText7)
    /// Đảm bảo tính pháp lý, toàn vẹn cấu trúc PDF và hiển thị mượt mà trên mọi trình duyệt.
    /// </summary>
    public static class DocumentSampleGenerator
    {
        private static PdfFont BoldFont => PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
        private static PdfFont RegularFont => PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
        private static PdfFont ItalicFont => PdfFontFactory.CreateFont(StandardFonts.HELVETICA_OBLIQUE);

        /// <summary>
        /// Tạo văn bản Quyết định thành lập Bên mời thầu / Chủ đầu tư
        /// </summary>
        public static byte[] GenerateEstablishmentDecisionPdf(string orgName, string taxCode, string repName, string? docNumber = null)
        {
            using var ms = new MemoryStream();
            using (var writer = new PdfWriter(ms))
            using (var pdf = new PdfDocument(writer))
            using (var doc = new Document(pdf, PageSize.A4))
            {
                doc.SetMargins(36, 40, 36, 40);

                var headerTable = new Table(UnitValue.CreatePercentArray(new float[] { 45, 55 }))
                    .UseAllAvailableWidth()
                    .SetBorder(Border.NO_BORDER);

                var leftHeader = new Cell()
                    .SetBorder(Border.NO_BORDER)
                    .SetTextAlignment(TextAlignment.CENTER)
                    .Add(new Paragraph("CO QUAN CHU QUAN CAP TREN").SetFontSize(10).SetFont(BoldFont))
                    .Add(new Paragraph(orgName.ToUpperInvariant()).SetFontSize(10).SetFont(BoldFont))
                    .Add(new Paragraph($"So: {docNumber ?? "128/QD-BQLDA"}").SetFontSize(9).SetFont(ItalicFont));

                var rightHeader = new Cell()
                    .SetBorder(Border.NO_BORDER)
                    .SetTextAlignment(TextAlignment.CENTER)
                    .Add(new Paragraph("CONG HOA XA HOI CHU NGHIA VIET NAM").SetFontSize(10).SetFont(BoldFont))
                    .Add(new Paragraph("Doc lap - Tu do - Hanh phuc").SetFontSize(10).SetFont(BoldFont).SetUnderline())
                    .Add(new Paragraph($"Ha Noi, ngay {DateTime.Now:dd} thang {DateTime.Now:MM} nam {DateTime.Now:yyyy}").SetFontSize(9).SetFont(ItalicFont));

                headerTable.AddCell(leftHeader);
                headerTable.AddCell(rightHeader);
                doc.Add(headerTable);

                doc.Add(new Paragraph("\n").SetFontSize(6));

                doc.Add(new Paragraph("QUYET DINH")
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetFontSize(16)
                    .SetFont(BoldFont)
                    .SetFontColor(ColorConstants.BLUE));

                doc.Add(new Paragraph("Ve viec kien toan to chuc, chuc nang, nhiem vu va tham quyen cua Ben moi thau\nphuc vu cong tac dau thau lua chon nha thau qua Mang dau thau Quoc gia")
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetFontSize(10.5f)
                    .SetFont(ItalicFont)
                    .SetMarginBottom(12));

                doc.Add(new Paragraph("THAM QUYEN NGUOI CO THAM QUYEN / THU TRUONG DON VI").SetFontSize(10).SetFont(BoldFont));
                doc.Add(new Paragraph("- Can cu Luat Dau thau so 22/2023/QH15 ngay 23 thang 06 nam 2023 cua Quoc hoi nuoc Cong hoa Xa hoi Chu nghia Viet Nam;\n" +
                                      "- Can cu Nghi dinh so 24/2024/ND-CP ngay 27 thang 02 nam 2024 cua Chinh phu quy dinh chi tiet mot so dieu va bien phap thi hanh Luat Dau thau ve lua chon nha thau;\n" +
                                      "- Can cu Thong tu so 06/2024/TT-BKHDT huong dan viec cung cap, dang tai thong tin ve dau thau va lua chon nha thau tren He thong mang dau thau quoc gia;\n" +
                                      "- Theo de nghi cua Truong phong To chuc Can bo va Truong phong Quan ly Mua sam - Dau thau.").SetFontSize(9).SetFont(ItalicFont));

                doc.Add(new Paragraph("\nQUYET DINH:").SetFontSize(10.5f).SetFont(BoldFont));

                doc.Add(new Paragraph("Dieu 1. Xac nhan tu cach phap nhan va kien toan to chuc cua Don vi:").SetFontSize(9.5f).SetFont(BoldFont));
                doc.Add(new Paragraph($"  + Ten don vi Ben moi thau: {orgName}\n" +
                                      $"  + Ma so thue / Ma dinh danh NS: {taxCode}\n" +
                                      $"  + Nguoi dai dien theo phap luat: {repName}\n" +
                                      $"  + Pham vi hoat dong: To chuc lap ho so moi thau, danh gia ho so du thau va quan ly thuc hien hop dong kinh te theo quy dinh hien hanh.").SetFontSize(9).SetFont(RegularFont));

                doc.Add(new Paragraph("Dieu 2. Trach nhiem va Quyen han cua Nguoi dai dien:").SetFontSize(9.5f).SetFont(BoldFont));
                doc.Add(new Paragraph("  - Chi dao lap, tham dinh va phe duyet Ke hoach lua chon nha thau, E-HSMT, ket qua lua chon nha thau theo tham quyen duoc phan cap.\n" +
                                      "  - Thuc hien ky so dien tu, cong bo cong khai thong tin goi thau tren He thong Mang dau thau Quoc gia va chiu trach nhiem truoc phap luat.").SetFontSize(9).SetFont(RegularFont));

                doc.Add(new Paragraph("Dieu 3. Hieu luc thi hanh:").SetFontSize(9.5f).SetFont(BoldFont));
                doc.Add(new Paragraph("  Quyet dinh nay co hieu luc thi hanh ke tu ngay ky. Cac phong ban chuyen mon, thanh vien to chuyen gia dau thau va ca nhan co lien quan chiu trach nhiem thi hanh quyet dinh nay.").SetFontSize(9).SetFont(RegularFont));

                doc.Add(new Paragraph("\n").SetFontSize(10));

                var signTable = new Table(UnitValue.CreatePercentArray(new float[] { 50, 50 }))
                    .UseAllAvailableWidth()
                    .SetBorder(Border.NO_BORDER);

                var noiNhanCell = new Cell()
                    .SetBorder(Border.NO_BORDER)
                    .Add(new Paragraph("Noi nhan:").SetFontSize(8.5f).SetFont(BoldFont))
                    .Add(new Paragraph("- Nhu Dieu 3;\n- Cuc Quan ly Dau thau (de b/c);\n- Luu: VT, TCCB.").SetFontSize(8).SetFont(ItalicFont));

                var kyTenCell = new Cell()
                    .SetBorder(Border.NO_BORDER)
                    .SetTextAlignment(TextAlignment.CENTER)
                    .Add(new Paragraph("THU TRUONG CO QUAN / NGUOI DAI DIEN").SetFontSize(9.5f).SetFont(BoldFont))
                    .Add(new Paragraph("(Ky ten, dong dau phap nhan)").SetFontSize(8.5f).SetFont(ItalicFont))
                    .Add(new Paragraph("\n[DA KY SO DIEN TU VA DONG DAU]").SetFontSize(8.5f).SetFont(BoldFont).SetFontColor(ColorConstants.RED))
                    .Add(new Paragraph(repName).SetFontSize(9.5f).SetFont(BoldFont));

                signTable.AddCell(noiNhanCell);
                signTable.AddCell(kyTenCell);
                doc.Add(signTable);
            }
            return ms.ToArray();
        }

        /// <summary>
        /// Tạo văn bản Hợp đồng kinh tế đã ký kết hoàn chỉnh
        /// </summary>
        public static byte[] GenerateContractScannedPdf(string contractNumber, string packageName, string contractorName, decimal value, string? procuringEntityName = null)
        {
            using var ms = new MemoryStream();
            using (var writer = new PdfWriter(ms))
            using (var pdf = new PdfDocument(writer))
            using (var doc = new Document(pdf, PageSize.A4))
            {
                doc.SetMargins(36, 40, 36, 40);

                var headerTable = new Table(UnitValue.CreatePercentArray(new float[] { 100 }))
                    .UseAllAvailableWidth()
                    .SetBorder(Border.NO_BORDER);

                headerTable.AddCell(new Cell()
                    .SetBorder(Border.NO_BORDER)
                    .SetTextAlignment(TextAlignment.CENTER)
                    .Add(new Paragraph("CONG HOA XA HOI CHU NGHIA VIET NAM").SetFontSize(11).SetFont(BoldFont))
                    .Add(new Paragraph("Doc lap - Tu do - Hanh phuc").SetFontSize(10.5f).SetFont(BoldFont).SetUnderline())
                    .Add(new Paragraph($"Ha Noi, ngay {DateTime.Now:dd} thang {DateTime.Now:MM} nam {DateTime.Now:yyyy}").SetFontSize(9).SetFont(ItalicFont)));
                doc.Add(headerTable);

                doc.Add(new Paragraph("\n").SetFontSize(6));

                doc.Add(new Paragraph("HOP DONG KINH TE")
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetFontSize(16)
                    .SetFont(BoldFont)
                    .SetFontColor(ColorConstants.BLUE));

                doc.Add(new Paragraph($"So hop dong: {contractNumber}")
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetFontSize(11)
                    .SetFont(BoldFont)
                    .SetMarginBottom(8));

                doc.Add(new Paragraph($"Goi thau: {packageName}")
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetFontSize(10)
                    .SetFont(ItalicFont)
                    .SetMarginBottom(12));

                doc.Add(new Paragraph("CAN CU PHAP LY HOP DONG:").SetFontSize(9.5f).SetFont(BoldFont));
                doc.Add(new Paragraph("- Can cu Bo luat Dan su so 91/2015/QH13 ngay 24/11/2015;\n" +
                                      "- Can cu Luat Dau thau so 22/2023/QH15 va Nghi dinh 24/2024/ND-CP cua Chinh phu;\n" +
                                      "- Can cu Quyet dinh phe duyet Ket qua lua chon nha thau.").SetFontSize(8.5f).SetFont(ItalicFont));

                doc.Add(new Paragraph("\nI. DAI DIEN CAC BEN THAM GIA HOP DONG:").SetFontSize(10).SetFont(BoldFont));

                doc.Add(new Paragraph("BEN A: CHU DAU TU / BEN MOI THAU").SetFontSize(9.5f).SetFont(BoldFont));
                doc.Add(new Paragraph($"  + Ten don vi: {procuringEntityName ?? "BAN QUAN LY DU AN DAU TU VA MUA SAM DOANH NGHIEP"}\n" +
                                      $"  + Dai dien boi: Giam doc Ban Quan ly Du an\n" +
                                      $"  + Tai khoan thanh toan: 1100223344 tai Kho bac Nha nuoc / Ngan hang BIDV").SetFontSize(9).SetFont(RegularFont));

                doc.Add(new Paragraph("BEN B: DON VI TRUNG THAU / NHA THAU THUC HIEN").SetFontSize(9.5f).SetFont(BoldFont));
                doc.Add(new Paragraph($"  + Ten nha thau: {contractorName}\n" +
                                      $"  + Dai dien boi: Tong Giam doc / Giam doc Doanh nghiep\n" +
                                      $"  + Nang luc: Dap ung 100% yeu cau ky thuat va tai chinh cua E-HSMT").SetFontSize(9).SetFont(RegularFont));

                doc.Add(new Paragraph("\nII. CAC DIEU KHOAN CHINH:").SetFontSize(10).SetFont(BoldFont));
                doc.Add(new Paragraph($"Dieu 1. Noi dung cong viec: Nha thau cam ket cung cap day du vat tu, thiet bi va dich vu phan mem cho goi thau theo dung thiet ke va E-HSDT da trung thau.").SetFontSize(9).SetFont(RegularFont));
                doc.Add(new Paragraph($"Dieu 2. Gia tri hop dong: {value:N0} VND (Da bao gom thue GTGT 10% va toan bo chi phi lien quan).").SetFontSize(9).SetFont(BoldFont));
                doc.Add(new Paragraph($"Dieu 3. Tien do & Thanh toan: Giai ngan theo tung moc nghiem thu ky thuat thuc te (Tung dot: 30% - 50% - 20%) sau khi co Bien ban nghiem thu co xac nhan Maker-Checker.").SetFontSize(9).SetFont(RegularFont));
                doc.Add(new Paragraph($"Dieu 4. Bao dam thuc hien hop dong: Nha thau phai nop Thu bao lanh ngan hang voi gia tri tuong duong 5% gia tri hop dong truoc khi hop dong co hieu luc.").SetFontSize(9).SetFont(RegularFont));
                doc.Add(new Paragraph($"Dieu 5. Hieu luc thi hanh: Hop dong co hieu luc ke tu ngay ky va ket thuc sau khi hai ben hoan tat nghiem thu thanh quyet toan va het han bao hanh 24 thang.").SetFontSize(9).SetFont(RegularFont));

                doc.Add(new Paragraph("\n").SetFontSize(10));

                var signTable = new Table(UnitValue.CreatePercentArray(new float[] { 50, 50 }))
                    .UseAllAvailableWidth()
                    .SetBorder(Border.NO_BORDER);

                var benACell = new Cell()
                    .SetBorder(Border.NO_BORDER)
                    .SetTextAlignment(TextAlignment.CENTER)
                    .Add(new Paragraph("DAI DIEN BEN A (CHU DAU TU)").SetFontSize(9.5f).SetFont(BoldFont))
                    .Add(new Paragraph("(Ky ten, dong dau phap nhan)").SetFontSize(8.5f).SetFont(ItalicFont))
                    .Add(new Paragraph("\n[DA KY SO & DONG DAU MOC DO]").SetFontSize(8.5f).SetFont(BoldFont).SetFontColor(ColorConstants.RED))
                    .Add(new Paragraph("\nGIAM DOC BAN QLDA").SetFontSize(9.5f).SetFont(BoldFont));

                var benBCell = new Cell()
                    .SetBorder(Border.NO_BORDER)
                    .SetTextAlignment(TextAlignment.CENTER)
                    .Add(new Paragraph("DAI DIEN BEN B (NHA THAU)").SetFontSize(9.5f).SetFont(BoldFont))
                    .Add(new Paragraph("(Ky ten, dong dau doanh nghiep)").SetFontSize(8.5f).SetFont(ItalicFont))
                    .Add(new Paragraph("\n[DA KY SO CHUNG THU SO PKCS#7]").SetFontSize(8.5f).SetFont(BoldFont).SetFontColor(ColorConstants.BLUE))
                    .Add(new Paragraph($"\n{contractorName.ToUpperInvariant()}").SetFontSize(9f).SetFont(BoldFont));

                signTable.AddCell(benACell);
                signTable.AddCell(benBCell);
                doc.Add(signTable);
            }
            return ms.ToArray();
        }

        /// <summary>
        /// Tạo văn bản Chứng thư Bảo lãnh thực hiện hợp đồng (Vietcombank)
        /// </summary>
        public static byte[] GenerateGuaranteeDocumentPdf(string guaranteeNumber, string bankName, decimal amount, string contractNumber, string? contractorName = null)
        {
            using var ms = new MemoryStream();
            using (var writer = new PdfWriter(ms))
            using (var pdf = new PdfDocument(writer))
            using (var doc = new Document(pdf, PageSize.A4))
            {
                doc.SetMargins(36, 40, 36, 40);

                var bankHeaderTable = new Table(UnitValue.CreatePercentArray(new float[] { 50, 50 }))
                    .UseAllAvailableWidth()
                    .SetBorder(Border.NO_BORDER);

                var leftBankCell = new Cell()
                    .SetBorder(Border.NO_BORDER)
                    .Add(new Paragraph(bankName.ToUpperInvariant()).SetFontSize(10.5f).SetFont(BoldFont).SetFontColor(ColorConstants.DARK_GRAY))
                    .Add(new Paragraph("HOI SO CHINH - KHOI KHACH HANG DOANH NGHIEP").SetFontSize(8.5f).SetFont(RegularFont))
                    .Add(new Paragraph($"So phat hanh: {guaranteeNumber}").SetFontSize(9).SetFont(BoldFont));

                var rightBankCell = new Cell()
                    .SetBorder(Border.NO_BORDER)
                    .SetTextAlignment(TextAlignment.RIGHT)
                    .Add(new Paragraph("CONG HOA XA HOI CHU NGHIA VIET NAM").SetFontSize(10).SetFont(BoldFont))
                    .Add(new Paragraph("Doc lap - Tu do - Hanh phuc").SetFontSize(9.5f).SetFont(BoldFont).SetUnderline())
                    .Add(new Paragraph($"Ngay phat hanh: {DateTime.Now:dd/MM/yyyy}").SetFontSize(8.5f).SetFont(ItalicFont));

                bankHeaderTable.AddCell(leftBankCell);
                bankHeaderTable.AddCell(rightBankCell);
                doc.Add(bankHeaderTable);

                doc.Add(new Paragraph("\n").SetFontSize(8));

                doc.Add(new Paragraph("THU BAO LANH THUC HIEN HOP DONG")
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetFontSize(15)
                    .SetFont(BoldFont)
                    .SetFontColor(ColorConstants.BLUE));

                doc.Add(new Paragraph("(Bao lanh vo dieu kien va khong huy ngang theo Dieu 14 & Dieu 68 Luat Dau thau 2023)")
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetFontSize(10)
                    .SetFont(ItalicFont)
                    .SetMarginBottom(15));

                doc.Add(new Paragraph("Kinh gui: BAN QUAN LY DU AN & CHU DAU TU (Ben thu huong bao lanh)").SetFontSize(10).SetFont(BoldFont));
                doc.Add(new Paragraph($"Chung toi duoc thong bao rang Ben duoc bao lanh la Nha thau: {contractorName ?? "DON VI TRUNG THAU HOP DONG"}\n" +
                                      $"da ky ket Hop dong kinh te so: {contractNumber} de thuc hien goi thau theo quy dinh cua Luat Dau thau so 22/2023/QH15.\n\n" +
                                      $"Theo yeu cau cua Hop dong, Ben duoc bao lanh phai nop cho Ben thu huong mot Thu bao lanh thuc hien hop dong do mot Ngan hang thuong mai hoat dong hop phap tai Viet Nam phat hanh.").SetFontSize(9.5f).SetFont(RegularFont));

                doc.Add(new Paragraph("\nCAM KET CUA NGAN HANG BAO LANH:").SetFontSize(10).SetFont(BoldFont));
                doc.Add(new Paragraph($"1. {bankName} cam ket vo dieu kien, khong the huy ngang se thanh toan ngay cho Ben thu huong bat ky khoan tien nao trong gioi han toi da la:").SetFontSize(9.5f).SetFont(RegularFont));
                
                doc.Add(new Paragraph($"   {amount:N0} VND")
                    .SetFontSize(13)
                    .SetFont(BoldFont)
                    .SetFontColor(ColorConstants.RED)
                    .SetMarginLeft(20));

                doc.Add(new Paragraph($"2. Ngan hang se thuc hien chuyen tien ngay trong vong 03 (ba) ngay lam viec ke tu khi nhan duoc van ban yeu cau thanh toan dau tien cua Ben thu huong co neu ro ly do Ben duoc bao lanh vi pham hop dong, ma khong yeu cau Ben thu huong phai chung minh hoac dua ra can cu doi voi yeu cau do.\n\n" +
                                      $"3. Thoi han hieu luc cua Thu bao lanh: Co hieu luc ke tu ngay phat hanh cho den khi hop dong duoc hoan tat nghiem thu va Ben duoc bao lanh chuyen sang nghia vu bao hanh cong trinh/san pham theo quy dinh.").SetFontSize(9.5f).SetFont(RegularFont));

                doc.Add(new Paragraph("\n").SetFontSize(15));

                var signTable = new Table(UnitValue.CreatePercentArray(new float[] { 50, 50 }))
                    .UseAllAvailableWidth()
                    .SetBorder(Border.NO_BORDER);

                var leftEmpty = new Cell()
                    .SetBorder(Border.NO_BORDER)
                    .Add(new Paragraph("Kiem soat vien Ngan hang:").SetFontSize(8.5f).SetFont(ItalicFont))
                    .Add(new Paragraph("[DA DOI CHIEU TIN DUNG]").SetFontSize(8f).SetFont(BoldFont));

                var rightBankSign = new Cell()
                    .SetBorder(Border.NO_BORDER)
                    .SetTextAlignment(TextAlignment.CENTER)
                    .Add(new Paragraph($"DAI DIEN CO THAM QUYEN CUA NGAN HANG").SetFontSize(9.5f).SetFont(BoldFont))
                    .Add(new Paragraph(bankName.ToUpperInvariant()).SetFontSize(9f).SetFont(BoldFont))
                    .Add(new Paragraph("\n[DA XAC THUC CHU KY SO NGAN HANG]").SetFontSize(8.5f).SetFont(BoldFont).SetFontColor(ColorConstants.RED))
                    .Add(new Paragraph("\nGIAM DOC KHOI TINDUNG & BAO LANH").SetFontSize(9.5f).SetFont(BoldFont));

                signTable.AddCell(leftEmpty);
                signTable.AddCell(rightBankSign);
                doc.Add(signTable);
            }
            return ms.ToArray();
        }

        /// <summary>
        /// Tạo Giấy chứng nhận Đăng ký Doanh nghiệp (GPKD)
        /// </summary>
        public static byte[] GenerateBusinessLicensePdf(string companyName, string taxCode, string repName, string address)
        {
            using var ms = new MemoryStream();
            using (var writer = new PdfWriter(ms))
            using (var pdf = new PdfDocument(writer))
            using (var doc = new Document(pdf, PageSize.A4))
            {
                doc.SetMargins(36, 40, 36, 40);

                var headerTable = new Table(UnitValue.CreatePercentArray(new float[] { 45, 55 }))
                    .UseAllAvailableWidth()
                    .SetBorder(Border.NO_BORDER);

                headerTable.AddCell(new Cell()
                    .SetBorder(Border.NO_BORDER)
                    .SetTextAlignment(TextAlignment.CENTER)
                    .Add(new Paragraph("SO KE HOACH VA DAU TU").SetFontSize(10).SetFont(BoldFont))
                    .Add(new Paragraph("PHONG DANG KY KINH DOANH").SetFontSize(9.5f).SetFont(BoldFont)));

                headerTable.AddCell(new Cell()
                    .SetBorder(Border.NO_BORDER)
                    .SetTextAlignment(TextAlignment.CENTER)
                    .Add(new Paragraph("CONG HOA XA HOI CHU NGHIA VIET NAM").SetFontSize(10).SetFont(BoldFont))
                    .Add(new Paragraph("Doc lap - Tu do - Hanh phuc").SetFontSize(9.5f).SetFont(BoldFont).SetUnderline()));
                doc.Add(headerTable);

                doc.Add(new Paragraph("\n").SetFontSize(8));

                doc.Add(new Paragraph("GIAY CHUNG NHAN DANG KY DOANH NGHIEP")
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetFontSize(15)
                    .SetFont(BoldFont)
                    .SetFontColor(ColorConstants.BLUE));

                doc.Add(new Paragraph("CONG TY TRACH NHIEM HUU HAN / CONG TY CO PHAN")
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetFontSize(11)
                    .SetFont(BoldFont));

                doc.Add(new Paragraph($"Ma so doanh nghiep / MST: {taxCode}")
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetFontSize(12)
                    .SetFont(BoldFont)
                    .SetMarginBottom(15));

                doc.Add(new Paragraph($"1. Ten cong ty: {companyName.ToUpperInvariant()}").SetFontSize(10).SetFont(BoldFont));
                doc.Add(new Paragraph($"2. Dia chi tru so chinh: {address}").SetFontSize(9.5f).SetFont(RegularFont));
                doc.Add(new Paragraph($"3. Nguoi dai dien theo phap luat: {repName}").SetFontSize(9.5f).SetFont(BoldFont));
                doc.Add(new Paragraph($"4. Von dieu le: 50.000.000.000 VND (Nam muoi ty dong chan)").SetFontSize(9.5f).SetFont(RegularFont));
                doc.Add(new Paragraph($"5. Nganh nghe kinh doanh chinh: Xay dung, Cong nghe thong tin, Mua sam trang thiet bi y te & vien thong phuc vu cac du an dau thau.").SetFontSize(9.5f).SetFont(RegularFont));

                doc.Add(new Paragraph("\n\n").SetFontSize(15));

                var signTable = new Table(UnitValue.CreatePercentArray(new float[] { 50, 50 }))
                    .UseAllAvailableWidth()
                    .SetBorder(Border.NO_BORDER);

                signTable.AddCell(new Cell().SetBorder(Border.NO_BORDER));
                signTable.AddCell(new Cell()
                    .SetBorder(Border.NO_BORDER)
                    .SetTextAlignment(TextAlignment.CENTER)
                    .Add(new Paragraph("TRUONG PHONG DANG KY KINH DOANH").SetFontSize(10).SetFont(BoldFont))
                    .Add(new Paragraph("(Ky ten, dong dau xac nhan)").SetFontSize(8.5f).SetFont(ItalicFont))
                    .Add(new Paragraph("\n[DA XAC THUC PHAP LY & CON DAU]").SetFontSize(8.5f).SetFont(BoldFont).SetFontColor(ColorConstants.RED)));
                doc.Add(signTable);
            }
            return ms.ToArray();
        }

        /// <summary>
        /// Tạo Chứng chỉ bồi dưỡng chuyên môn về nghiệp vụ đấu thầu
        /// </summary>
        public static byte[] GenerateEvaluatorCertificatePdf(string fullName, string? certName = null)
        {
            using var ms = new MemoryStream();
            using (var writer = new PdfWriter(ms))
            using (var pdf = new PdfDocument(writer))
            using (var doc = new Document(pdf, PageSize.A4.Rotate()))
            {
                doc.SetMargins(30, 40, 30, 40);

                doc.Add(new Paragraph("BO KE HOACH VA DAU TU - HOC VIEN CHINH SACH VA PHAT TRIEN")
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetFontSize(12)
                    .SetFont(BoldFont));

                doc.Add(new Paragraph("CONG HOA XA HOI CHU NGHIA VIET NAM\nDoc lap - Tu do - Hanh phuc")
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetFontSize(11)
                    .SetFont(BoldFont)
                    .SetMarginBottom(15));

                doc.Add(new Paragraph("CHUNG CHI")
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetFontSize(22)
                    .SetFont(BoldFont)
                    .SetFontColor(ColorConstants.BLUE));

                doc.Add(new Paragraph("BOI DUONG NGHIEP VU CHUYEN MON VE DAU THAU")
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetFontSize(14)
                    .SetFont(BoldFont)
                    .SetMarginBottom(15));

                doc.Add(new Paragraph($"Chung nhan Ong/Ba: {fullName.ToUpperInvariant()}")
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetFontSize(15)
                    .SetFont(BoldFont)
                    .SetFontColor(ColorConstants.DARK_GRAY));

                doc.Add(new Paragraph($"Da hoan thanh xuat sac khoa dao tao, boi duong kien thuc phap luat dau thau va dap ung day du dieu kien nang luc chuyen mon theo quy dinh cua Luat Dau thau so 22/2023/QH15 va Thong tu 06/2024/TT-BKHDT.")
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetFontSize(11)
                    .SetFont(ItalicFont)
                    .SetMarginLeft(50)
                    .SetMarginRight(50)
                    .SetMarginBottom(20));

                var signTable = new Table(UnitValue.CreatePercentArray(new float[] { 50, 50 }))
                    .UseAllAvailableWidth()
                    .SetBorder(Border.NO_BORDER);

                signTable.AddCell(new Cell()
                    .SetBorder(Border.NO_BORDER)
                    .Add(new Paragraph($"So hieu chung chi: CC-DT-2026/0899\nNgay cap: {DateTime.Now:dd/MM/yyyy}").SetFontSize(9).SetFont(ItalicFont)));

                signTable.AddCell(new Cell()
                    .SetBorder(Border.NO_BORDER)
                    .SetTextAlignment(TextAlignment.CENTER)
                    .Add(new Paragraph("GIAM DOC HOC VIEN CHINH SACH VA PHAT TRIEN").SetFontSize(10.5f).SetFont(BoldFont))
                    .Add(new Paragraph("[DA KY SO & DONG DAU]").SetFontSize(8.5f).SetFont(BoldFont).SetFontColor(ColorConstants.RED)));

                doc.Add(signTable);
            }
            return ms.ToArray();
        }
    }
}
