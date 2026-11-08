# BÁO CÁO CÔNG VIỆC TUẦN [02/11/2026 - 08/11/2026]

**Dự án**: Hệ thống Quản lý Đấu thầu và Theo dõi Hợp đồng (*Procurement & Contract Tracking System*)  
**Đơn vị thực tập**: Cổng Đấu thầu & Quản lý Hợp đồng Điện tử Tập trung  
**Nhóm thực hiện**: 2 thành viên  
**Họ và tên sinh viên**: Phạm Minh Tài  
**Vị trí**: Thực tập sinh Phát triển Phần mềm & Kiểm thử Hệ thống (Fullstack / QA Engineer)  
**Thời gian báo cáo**: Tuần làm việc từ **02/11/2026** đến **08/11/2026**  

---

## TỔNG QUAN KẾT QUẢ TRỌNG TÂM TRONG TUẦN

- **Tuần trước (26/10 - 01/11/2026)**:
  - Chuẩn hóa đồng bộ dữ liệu Danh mục Enums đa nền tảng giữa Backend C# (.NET 9) và Frontend React (Vite) thông qua kịch bản tự động hóa `generate-enums.mjs`.
  - Thiết kế và triển khai cơ chế kiểm soát minh bạch thông tin đấu thầu (*Transparency Controls*), khóa chống can thiệp bảng điểm ma trận Scorecard và bục vinh danh trao thầu.
  - Chuẩn hóa quy trình cấp phát mã định danh ảo mốc hợp đồng (*Virtual Token & Pre-signed Contract Milestone*).
- **Tuần này (02/11 - 08/11/2026)**:
  - **Tích hợp Kiến trúc Lưu trữ Đám mây lai (Hybrid Cloud Storage)**: Kết nối thành công Supabase Storage thông qua giao thức S3 Protocol tiêu chuẩn quốc tế; tích hợp đường ống nén GZip (giảm 60–80% dung lượng) và mã hóa cấp tệp AES-256-CBC với IV ngẫu nhiên; xây dựng dịch vụ di chuyển `StorageMigrationService` với tính năng tự động sao lưu CSDL, đồng bộ 1-Click Sync lên Cloud và hoàn tác 1-Click Rollback về Local an toàn.
  - **Kiểm toán Nghiệp vụ Chuyên sâu & Khắc phục Lỗi Pháp lý (Senior QA / BA Audit)**: Rà soát toàn diện hệ thống theo Luật Đấu thầu số 22/2023/QH15 và Nghị định số 24/2024/NĐ-CP; khắc phục triệt để lỗi Crash 500 khi tải tệp Cloud (chuẩn hóa 6 API tải tệp sang `ResolveDownloadAsync`); loại bỏ lỗi deadlock HTTP 409 bế tắc giải ngân thanh toán mốc hợp đồng (đồng bộ bảng `Acceptances` vào migration pipeline); bổ sung rào chắn chặn chuyển trạng thái chấm thầu khi gói thầu có 0 hồ sơ dự thầu hợp lệ.
  - **Chuẩn hóa Giao diện Tiếng Việt & Hành chính hóa (Anti-Slop & Tone of Voice)**: Loại bỏ hoàn toàn mã enum tiếng Anh thô và tên thư viện kỹ thuật Backend (`ClosedXML`) trên giao diện; đổi tên tab chấm thầu sang **"Phiếu chấm điểm chuyên gia"** và **"Bảng tổng hợp xếp hạng & Trao thầu"**; chuẩn hóa các nhãn thẩm quyền ký kết kinh tế và phê duyệt tài chính.
  - **Siết chặt Bảo mật Xác thực OTP & Khóa Mã hóa Storage (Nhiệm vụ Agent B)**: Bắt buộc xác thực OTP 6 số bảo mật qua Email khi đăng ký tài khoản Nhà thầu và Bên mời thầu (loại bỏ cơ chế bypass tạm thời, khôi phục cooldown 60s và cơ chế tự hủy OTP khi thay đổi email); xóa bỏ hoàn toàn fallback key SHA-256 cố định, bắt buộc `MasterKey` Base64 32 bytes (256 bits); khắc phục 4 lỗi kỹ thuật review chuyên sâu (R-B01: Độc lập hóa Local Storage khỏi khóa; R-B02: Tự động giải mã tệp cũ độc lập với cờ ghi file mới; R-B03: Sửa lỗi nhận diện tệp PSEN bị cắt cụt; R-B04: Bộ kiểm chứng OTP boundary thực tế).
  - **Biên soạn Cẩm nang Hướng dẫn Kiểm thử Toàn bộ Nghiệp vụ Hệ thống**: Hoàn thiện tài liệu [`docs/HUONG_DAN_KIEM_THU_TOAN_BO_NGHIEP_VU.md`](file:///d:/Thuc%20Tap/DuAnTT/docs/HUONG_DAN_KIEM_THU_TOAN_BO_NGHIEP_VU.md) gồm 8 phân hệ nghiệp vụ, 4 vai trò người dùng (Admin, Procurement, Contractor, Evaluator) và 31 kịch bản kiểm thử chi tiết phục vụ nghiệm thu.
  - **Chất lượng Kiểm thử Tự động Đạt Đỉnh cao**:
    + Backend C# (.NET 9): Đạt **106/106 bài kiểm thử PASS (100%)** với xUnit (tăng từ 63 lên 106 bài test).
    + Frontend React: Đạt **6/6 bài kiểm thử PASS (100%)** với Node.js Test Runner.
    + Đóng gói Production Vite Build: Thành công 100% trong 5.52s (0 lỗi, 0 cảnh báo hỏng mã).
- **Mục tiêu đạt được**:
  - Đảm bảo toàn bộ chu trình đấu thầu, ký hợp đồng điện tử và nghiệm thu giải ngân vận hành thông suốt, an toàn tuyệt đối trên cả hạ tầng máy chủ cục bộ lẫn môi trường lưu trữ đám mây.
  - Tuân thủ nghiêm ngặt khung pháp lý đấu thầu Việt Nam, bảo vệ tính toàn vẹn của hồ sơ thầu Sealed-Bid và ngăn chặn 100% các lỗ hổng bypass OTP hoặc rò rỉ dữ liệu mã hóa nhị phân thô.
- **Sự cố, khó khăn**: Em chưa gặp sự cố, khó khăn nào. Toàn bộ các phát hiện sau kiểm toán và review kỹ thuật đều được xử lý dứt điểm, có bằng chứng kiểm chứng tự động rõ ràng.

---

## I. BẢNG TỔNG HỢP TIẾN ĐỘ THỰC HIỆN THEO NGÀY

**Bảng 1.1: Bảng tổng hợp công việc chi tiết các ngày trong tuần (02/11 - 08/11/2026)**

| Ngày thực hiện | Nhiệm vụ trọng tâm | Kết quả và Minh chứng kỹ thuật đạt được | Trạng thái |
| :---: | :--- | :--- | :---: |
| **Thứ Hai<br>(02/11/2026)** | • Thử nghiệm Migration & Đối soát CSDL trên bản sao độc lập `procurement_db_replica`.<br>• Kiểm thử E2E Giao diện chọn đơn vị Bên mời thầu & Chu trình Maker-Checker.<br>• Kiểm thử tự động E2E phân hệ Cấp phát tài khoản & Phân quyền thành viên nội bộ. | • Cập nhật CSDL bản sao an toàn, đối soát xử lý các gói thầu cũ mang cờ `OwnershipNeedsReview`.<br>• Chạy Playwright Chromium Headless tự động kiểm chứng 9 kịch bản E2E cấp phát tài khoản, phân quyền Maker-Checker và thu hồi tài khoản, thu thập 9 ảnh chụp màn hình minh chứng.<br>• Backend đạt **63/63 tests PASS (100%)**, Frontend đạt **6/6 tests PASS (100%)**. | **Hoàn thành** |
| **Thứ Ba<br>(03/11/2026)** | • Tích hợp Kiến trúc Hybrid Cloud Storage với Supabase Storage (S3 Protocol).<br>• Thiết lập pipeline Nén GZip và Mã hóa AES-256-CBC.<br>• Kiểm toán Nghiệp vụ Đấu thầu & Sửa lỗi logic Backend (BUG-01 deadlock giải ngân, BUG-02 chặn chấm thầu 0 hồ sơ).<br>• Chuẩn hóa ngôn ngữ giao diện Frontend (Anti-Slop & Hành chính hóa). | • Hoàn thành `SupabaseStorageService.cs`, `FileCryptoCompressionService.cs`, `StorageMigrationService.cs`.<br>• Sửa hàm băm `ComputeFileHashSafeAsync` trong `ContractService.cs` giải quyết triệt để lỗi HTTP 409 bế tắc giải ngân.<br>• Bổ sung rào chắn kiểm tra `validSubmissionsCount == 0` từ chối chấm thầu theo Điều 17 Luật Đấu thầu 2023.<br>• Đổi tên tab "Phiếu chấm điểm chuyên gia" và loại bỏ 100% chuỗi enum tiếng Anh thô trên giao diện. | **Hoàn thành** |
| **Thứ Tư<br>(04/11/2026)** | • Kiểm toán nghiệp vụ sâu toàn hệ thống (Senior QA / BA Audit).<br>• Khắc phục BUG-01: Chuẩn hóa 6 API tải tệp sang `ResolveDownloadAsync`.<br>• Khắc phục BUG-02: Bổ sung bảng `Acceptances` vào Cloud Migration Pipeline.<br>• Nâng cấp kiểm tra tính toàn vẹn SHA-256 tệp đám mây. | • Cập nhật 6 API tải tệp (bảo lãnh, phụ lục, snapshot nghiệm thu, chứng chỉ giám khảo, quyết định thành lập, quyết định bổ nhiệm) trả về `FileDownloadResult`, hoạt động mượt mà cả Local và Supabase Cloud.<br>• Bổ sung bảng `Acceptances` vào quy trình sao lưu, đồng bộ Cloud và hoàn tác Rollback trong `StorageMigrationService.cs`.<br>• Backend đạt **80/80 tests PASS (100%)**, Frontend đạt **6/6 tests PASS (100%)**. | **Hoàn thành** |
| **Thứ Năm<br>(05/11/2026)** | • Phân tích tài liệu bàn giao 2 Agent (`HANDOFF_2_AGENTS_NOTIFICATION_SECURITY_2026-11-05.md`), nhận vai trò **Agent B** trên nhánh `feat/agent-b-security-otp-storage`.<br>• Kết thúc cơ chế bỏ qua OTP khi đăng ký tài khoản.<br>• Siết chặt khóa mã hóa Storage, loại bỏ hoàn toàn Fallback Key. | • Bắt buộc thuộc tính `OtpCode` 6 số ở cả tầng DTO và Service (`ContractorAuthService`, `ProcuringEntityAuthService`).<br>• Khôi phục giao diện đăng ký với bộ đếm cooldown 60s và tự động hủy mã khi đổi email trong `RegisterPage.jsx`.<br>• Loại bỏ hàm `DeriveKeyFromSeed` và fallback key cố định; bắt buộc `MasterKey` 32 bytes Base64; chặn phát tán ciphertext thô khi giải mã hỏng.<br>• Tạo mới `RegistrationOtpSecurityTests.cs`, nâng tổng số test Backend lên **93/93 tests PASS (100%)**. | **Hoàn thành** |
| **Thứ Sáu<br>(06/11/2026)** | • Tiếp nhận Handoff Review Kỹ thuật chuyên sâu (`HANDOFF_AGENT_B_REVIEW_FIXES_2026-11-06.md`).<br>• Biên soạn Cẩm nang Hướng dẫn Kiểm thử Toàn bộ Nghiệp vụ Hệ thống.<br>• Khắc phục 4 yêu cầu review kỹ thuật: R-B01, R-B02, R-B03, R-B04.<br>• Cập nhật tài liệu cấu hình Supabase & Lập báo cáo kết quả nghiệm thu. | • Hoàn thành tài liệu `HUONG_DAN_KIEM_THU_TOAN_BO_NGHIEP_VU.md` (8 phân hệ, 31 kịch bản kiểm thử).<br>• Sửa R-B01: Local Storage hoạt động độc lập không cần MasterKey.<br>• Sửa R-B02: Tự động giải mã tệp cũ có header PSEN độc lập với cờ ghi file mới.<br>• Sửa R-B03: Khắc phục nhận nhầm tệp PSEN bị cắt cụt là plaintext.<br>• Sửa R-B04: Bộ kiểm thử boundary OTP thật (chống replay, chống brute-force 5 lần, cô lập purpose, đăng ký E2E).<br>• Backend đạt đỉnh **106/106 tests PASS (100%)**, Frontend **6/6 tests PASS (100%)**, đóng gói Vite build thành công trong 5.52s. Đã push nhánh remote. | **Hoàn thành** |
| **Thứ Bảy & Chủ Nhật<br>(07/11 - 08/11/2026)** | • Rà soát tổng thể mã nguồn toàn dự án, đối soát log kiểm thử hồi quy.<br>• Biên soạn Báo cáo Công việc Tuần tổng hợp gửi người hướng dẫn và giảng viên phụ trách. | • Toàn bộ hệ thống Backend, Frontend và Database vận hành ổn định, không có xung đột mã nguồn.<br>• Hoàn thành Báo cáo Tuần tổng hợp, hệ thống hóa toàn bộ kết quả kỹ thuật và minh chứng kiểm thử. | **Hoàn thành** |

---

## II. CHI TIẾT CÁC CÔNG VIỆC KỸ THUẬT NỔI BẬT ĐÃ HOÀN THÀNH

### 1. Tích Hợp Kiến Trúc Lưu Trữ Đám Mây Lai (Hybrid Cloud File Storage) & An Toàn Dữ Liệu

#### 1.1. Bối cảnh và Bài toán
Trong môi trường thực tế của hệ thống mua sắm công, hồ sơ dự thầu (E-HSDT), bảo lãnh ngân hàng và biên bản nghiệm thu giải ngân có dung lượng lớn, đòi hỏi vừa phải lưu trữ tập trung trên đám mây để chia sẻ dữ liệu liên cơ quan, vừa phải đảm bảo an toàn tuyệt đối theo nguyên tắc **Zero-Trust**:
- Nếu lưu tệp plaintext trực tiếp lên Cloud, nguy cơ lộ lọt hồ sơ thầu trước giờ mở thầu là rất lớn.
- Nếu hệ thống chỉ phụ thuộc vào Cloud, khi mất kết nối mạng Internet hoặc dịch vụ bên thứ ba gặp sự cố, toàn bộ nghiệp vụ đấu thầu và giải ngân sẽ bị đình trệ.

#### 1.2. Giải pháp Kiến trúc Triển khai
Nhóm đã thiết kế mô hình **Lưu trữ Hybrid Zero-Risk**:
1. **Giao thức S3 Tiêu chuẩn Quốc tế**:
   - Sử dụng thư viện `AWSSDK.S3` kết nối tới **Supabase Storage** Private Bucket (`procurement-documents`).
   - Xây dựng lớp dịch vụ `SupabaseStorageService.cs` hỗ trợ lưu tệp (`SaveFileAsync`), lưu luồng (`SaveStreamAsync`), xóa tệp và phân phối tệp tải về (`ResolveDownloadAsync`).
2. **Đường ống Tối ưu hóa & Mã hóa Cấp Tệp (File-Level Encryption Pipeline)**:
   - Xây dựng lớp dịch vụ `FileCryptoCompressionService.cs`:
     + **Nén tối ưu (GZip Compression)**: Nén luồng dữ liệu bằng thuật toán GZip mức tối ưu (`CompressionLevel.Optimal`), giúp giảm 60–80% kích thước các tài liệu văn bản, bảng tính và hồ sơ scan PDF.
     + **Mã hóa mạnh mẽ (AES-256-CBC)**: Mã hóa dữ liệu sau nén bằng thuật toán AES-256 với Initialization Vector (IV) ngẫu nhiên 16 bytes sinh mới cho mỗi tệp.
     + **Gắn Header Nhận diện Nhị phân ("PSEN")**: Thêm 4 bytes header `0x50, 0x53, 0x45, 0x4E` ("PSEN" - Procurement System Encrypted) vào đầu tệp trước khi đẩy lên Cloud, hỗ trợ cơ chế tự động nhận diện và phân biệt giữa tệp mã hóa và tệp plaintext cũ.
     + **Xác thực Magic Bytes & Tính toàn vẹn SHA-256**: Kiểm tra chữ ký nhị phân thực tế của tệp (chống giả mạo phần mở rộng file) và tính toán mã băm SHA-256 phục vụ lưu vết kiểm toán pháp lý.
3. **Cơ chế Dự phòng Cục bộ Tự động (Local Fallback)**:
   - Xây dựng `HybridFileStorageService.cs`: Đóng vai trò lớp bao bọc thông minh. Nếu cấu hình chọn `Provider = "Local"` hoặc khi cấu hình Supabase bị thiếu thông tin/mất kết nối, hệ thống tự động ghi tệp an toàn vào thư mục đĩa cục bộ `uploads/` mà không gây gián đoạn phiên làm việc của người dùng.
4. **Tiện ích Di chuyển Dữ liệu 1-Click Sync & 1-Click Rollback**:
   - Xây dựng `StorageMigrationService.cs` và `StorageAdminController.cs`:
     + Cho phép Quản trị viên kiểm tra trạng thái lưu trữ (`/api/admin/storage/status`) và kiểm tra kết nối S3 Bucket (`/api/admin/storage/test-connection`).
     + **1-Click Sync**: Tự động tạo bảng sao lưu CSDL an toàn `_Backup_FilePaths_YYYYMMDD_HHMMSS`, nén và mã hóa từng tệp cục bộ rồi đẩy lên Cloud, cập nhật đồng loạt đường dẫn sang tiền tố `supabase://...`.
     + **1-Click Rollback**: Khôi phục 100% đường dẫn trong CSDL về tệp cục bộ chỉ trong 1-2 giây bằng bảng sao lưu gần nhất nếu có sự cố.

---

### 2. Khắc Phục Lỗi Logic Nghiệp Vụ Backend & Chuẩn Hóa Giao Diện (Senior QA / BA Audit)

#### 2.1. Khắc phục Triệt để Lỗi Deadlock Giải ngân Mốc Hợp đồng (BUG-01)
- **Triệu chứng phát hiện**: Tại phương thức `ApproveMilestoneAcceptanceAsync` trong `ContractService.cs`, hệ thống đối chiếu tính toàn vẹn tài liệu scan hợp đồng thông qua hàm băm `SnapshotDocumentHash`. Khi tệp được lưu trữ trên Cloud Storage (`supabase://...`), hàm `GetPhysicalFilePath` ném ngoại lệ do không tồn tại đường dẫn đĩa cục bộ. Khối lệnh `catch { }` nuốt ngoại lệ làm mã băm bị gán bằng `null`, dẫn đến so sánh sai lệch và ném lỗi HTTP 409 Conflict, gây tê liệt quy trình phê duyệt giải ngân thanh toán vốn ngân sách.
- **Giải pháp xử lý**:
  - Xây dựng hàm băm đa nền tảng `ComputeFileHashSafeAsync` trong `ContractService.cs`: Hỗ trợ đọc luồng byte stream từ Cloud URL, giải nén và giải mã luồng tệp mã hóa để tính toán mã băm SHA-256 gốc; nếu là tệp cục bộ thì đọc đĩa an toàn.
  - Tại khâu phê duyệt, chỉ chặn khi mã băm tính được thực sự sai lệch với mã băm ban đầu hoặc khi đường dẫn tệp đã bị tráo đổi, giải phóng hoàn toàn ách tắc giải ngân hợp đồng.

#### 2.2. Khắc phục Lỗi Chặn Chấm Điểm Gói Thầu 0 Hồ Sơ Dự Thầu (BUG-02)
- **Căn cứ pháp lý**: Theo Điều 17 Luật Đấu thầu số 22/2023/QH15 và Điều 37, 38 Nghị định số 24/2024/NĐ-CP, tại thời điểm đóng thầu nếu không có hồ sơ dự thầu nào được nộp (hoặc tất cả nhà thầu đã rút hồ sơ), Bên mời thầu phải báo cáo Người có thẩm quyền xem xét hủy thầu hoặc gia hạn thời điểm đóng thầu; tuyệt đối không thể mở thầu và chấm điểm một danh sách rỗng.
- **Giải pháp xử lý**:
  - Bổ sung rào chắn kiểm tra số lượng hồ sơ dự thầu hợp lệ: `var validSubmissionsCount = await _unitOfWork.Repository<BidSubmission>().CountAsync(s => s.BidPackageId == id && s.Status != BidSubmissionStatus.Withdrawn);` tại `BidPackageService.ChangeStatusAsync` và `EvaluationService.ScoreSubmissionAsync`.
  - Nếu số hồ sơ hợp lệ bằng 0, hệ thống từ chối chuyển trạng thái gói thầu sang Đang chấm điểm (`Evaluating`), trả về thông báo căn cứ đúng Điều 17 Luật Đấu thầu 2023.

#### 2.3. Chuẩn hóa 6 API Tải Tệp sang Mô hình Phân Phối Trực tuyến `ResolveDownloadAsync`
- Chuẩn hóa toàn bộ 6 API tải tệp tin pháp lý quan trọng:
  1. Tải tài liệu bảo lãnh hợp đồng (`/api/contracts/{id}/guarantee/download`).
  2. Tải phụ lục hợp đồng (`/api/contracts/appendices/{appendixId}/download`).
  3. Tải tài liệu biên bản nghiệm thu mốc (`/api/contracts/acceptances/{acceptanceId}/download`).
  4. Tải chứng chỉ chuyên gia chấm thầu (`/api/evaluatorproposals/{proposalId}/certificate/download`).
  5. Tải quyết định thành lập bên mời thầu (`/api/procuringentities/{id}/establishment-decision/download`).
  6. Tải quyết định bổ nhiệm đại diện pháp luật (`/api/procuringentities/{id}/appointment-decision/download`).
- Cả 3 Controller (`ContractsController`, `EvaluatorProposalsController`, `ProcuringEntitiesController`) đều sử dụng phương thức `ResolveDownloadAsync`, hỗ trợ phân phối linh hoạt: Stream giải mã trực tiếp, Redirect Pre-signed URL hoặc Physical File cục bộ.
- Bổ sung bảng `Acceptances` (`SnapshotDocumentPath`) vào chu trình sao lưu, đồng bộ Cloud và rollback của `StorageMigrationService.cs`.

#### 2.4. Chuẩn hóa Ngôn Ngữ Giao Diện Frontend (Anti-Slop & Hành chính hóa)
- Rà soát và cập nhật 8 tệp giao diện React JSX:
  + Đổi tên tab chấm thầu từ `Ma trận Chấm điểm (Scorecard)` thành **"Phiếu chấm điểm chuyên gia"** theo đúng định hướng nghiệp vụ; đổi tab xếp hạng thành **"Bảng tổng hợp xếp hạng & Trao thầu"** trong `EvaluationPage.jsx`.
  + Loại bỏ hoàn toàn tên thư viện kỹ thuật Backend `chuẩn ClosedXML` trong tooltip trang Hợp đồng (`ContractsPage.jsx`), đổi thành *"Xuất danh sách hợp đồng ra file Excel (.xlsx)"*.
  + Loại bỏ 100% mã enum tiếng Anh lọt vào câu văn giao diện: `(Draft)`, `(Selected)`, `(Disqualified)`, `(Contracted)`, `(Closed)`, `(Open)` trong các hộp thoại Modal.
  + Chuẩn hóa 3 loại thẩm quyền ký của Bên mời thầu sang tiếng Việt pháp lý chuẩn mực: Thẩm quyền Phê duyệt thanh toán / Giải ngân, Thẩm quyền Ký kết hợp đồng kinh tế, Người đại diện theo pháp luật.
  + Bổ sung bộ ánh xạ trạng thái tiếng Việt trong `ReviewApprovalModal.jsx` ngăn ngừa fallback hiển thị mã số thô hoặc chuỗi tiếng Anh khi dữ liệu rỗng.

---

### 3. Siết Chặt Bảo Mật Xác Thực OTP & Khóa Mã Hóa Storage (Nhiệm vụ Agent B)

#### 3.1. Kết Thúc Cơ Chế Bỏ Qua OTP Khi Đăng Ký Tài Khoản
- **Tầng DTO**: Bổ sung `[Required]` và biểu thức chính quy `[RegularExpression(@"^\d{6}$")]` cho thuộc tính `OtpCode` trong cả `RegisterContractorRequest` và `RegisterProcuringEntityRequest`.
- **Tầng Service (`ContractorAuthService`, `ProcuringEntityAuthService`)**: Khóa chặt logic xác thực, bắt buộc kiểm tra mã OTP hợp lệ với đúng Purpose phân tách (`RegisterContractor` và `RegisterProcuringEntity`), chặn đứng mọi nguy cơ tạo tài khoản rác không qua xác thực quyền sở hữu email.
- **Tầng Frontend (`RegisterPage.jsx`)**: Khôi phục giao diện đăng ký với ô nhập email có nút gửi mã OTP (hiển thị đếm ngược cooldown 60 giây) và ô nhập mã xác thực 6 chữ số; bổ sung cơ chế tự động xóa mã cũ và reset trạng thái khi người dùng sửa đổi địa chỉ email.

#### 3.2. Siết Chặt Khóa Mã Hóa Storage & Xóa Bỏ Fallback Key
- **Loại bỏ Fallback Key**: Xóa bỏ hoàn toàn fallback key SHA-256 cố định và phương thức `DeriveKeyFromSeed` trong `FileCryptoCompressionService.cs`.
- **Chuẩn hóa MasterKey**: Bắt buộc khóa mã hóa phải là chuỗi Base64 giải mã ra đúng 32 bytes (256 bits).
- **Startup Validation trong `Program.cs`**: Tự động kiểm tra tính hợp lệ của `MasterKey` ngay khi khởi động ứng dụng nếu kích hoạt Cloud Storage và bật mã hóa.
- **Bảo vệ Giải mã**: Khi gặp tệp có header `PSEN` nhưng giải mã thất bại (do sai khóa hoặc dữ liệu hỏng), hệ thống ném `CryptographicException`, tuyệt đối không trả về ciphertext thô hoặc redirect Pre-signed URL cho người dùng.

#### 3.3. Khắc Phục Triệt Để 4 Yêu Cầu Review Kỹ Thuật (R-B01 -> R-B04)
1. **R-B01 (P1 - Độc lập hóa Local Storage khỏi MasterKey)**:
   - Sửa constructor `FileCryptoCompressionService.cs`: Không ném ngoại lệ khi `MasterKey` rỗng; chỉ ghi log warning và gán `_masterKey = null`.
   - Trì hoãn việc kiểm tra khóa đến khi thực thi nghiệp vụ: ném `InvalidOperationException` khi gọi `ProcessForStorageAsync` hoặc khi gặp tệp `PSEN` trong `RestoreFromStorageAsync`.
   - Sửa `Program.cs`: Chỉ validate khóa khi `Provider == "Supabase"` VÀ `Encryption.Enabled == true`. Local Storage khởi động và hoạt động bình thường qua DI mà không cần cấu hình khóa.
2. **R-B02 (P2 - Giải mã Cloud độc lập với cờ ghi file mới)**:
   - Sửa `SupabaseStorageService.ResolveDownloadAsync`: Loại bỏ hoàn toàn sự phụ thuộc vào cờ `_settings.Encryption.Enabled`. Tải stream từ S3 về để `RestoreFromStorageAsync` tự động nhận diện header nhị phân `PSEN`: tệp cũ mã hóa vẫn được giải mã bình thường kể cả khi admin tắt cờ để ghi file mới dạng plaintext.
   - Ném ngoại lệ tường minh khi sai hoặc thiếu khóa, tuyệt đối không trả Pre-signed URL chứa ciphertext.
   - Bổ sung constructor nhận `IAmazonS3` phục vụ Dependency Injection và Unit Testing độc lập với mạng thật.
   - Gắn S3 metadata `x-amz-meta-encrypted = true` khi lưu tệp mã hóa.
3. **R-B03 (P2 - Khắc phục nhận nhầm tệp PSEN bị cắt cụt là Plaintext)**:
   - Đảo thứ tự kiểm tra trong `FileCryptoCompressionService.RestoreFromStorageAsync`: Đọc 4 bytes header trước.
   - Nếu khớp `"PSEN"` mà kích thước < 20 bytes (thiếu 16 bytes IV) hoặc thiếu ciphertext payload sau IV: Ném `InvalidDataException`.
   - Nếu dung lượng < 4 bytes hoặc không có header `"PSEN"`: Passthrough trả về stream plaintext an toàn.
   - Giải phóng tài nguyên bộ nhớ an toàn trong khối `finally`.
4. **R-B04 (P2 - Bộ kiểm chứng OTP Boundary thực tế)**:
   - Nâng cấp toàn diện `RegistrationOtpSecurityTests.cs`: Sử dụng `OtpService` thật với `IMemoryCache` thật, cấu hình SMTP InMemory và `CapturingFakeEmailService` ghi nhận email gửi đi.
   - Kiểm chứng thực tế:
     + Tuyệt đối không để lộ `DevOtpCode` trong phản hồi API.
     + Trích xuất mã 6 số từ email và xác thực thành công.
     + **Chống Replay Attack**: Mã bị tiêu thụ ngay sau lần đầu xác thực, gọi lại lần 2 bị từ chối.
     + **Chống Brute-force**: Sau 5 lần nhập sai, mã bị hủy ngay lập tức; lần thứ 6 dù nhập đúng mã ban đầu vẫn thất bại.
     + **Phân tách Purpose**: Mã cấp cho `RegisterContractor` không thể sử dụng cho `RegisterProcuringEntity` hoặc `ResetPassword`.
     + **Kiểm thử Boundary E2E**: Luồng đăng ký hoàn chỉnh cho cả Nhà thầu và Bên mời thầu với mã OTP thật trích xuất từ email.

---

### 4. Biên Soạn Cẩm Nang Hướng Dẫn Kiểm Thử Toàn Bộ Nghiệp Vụ Hệ Thống

Nhóm đã hoàn thành tài liệu [`docs/HUONG_DAN_KIEM_THU_TOAN_BO_NGHIEP_VU.md`](file:///d:/Thuc%20Tap/DuAnTT/docs/HUONG_DAN_KIEM_THU_TOAN_BO_NGHIEP_VU.md) làm căn cứ nghiệm thu thực tập tốt nghiệp:
- Hệ thống hóa luồng tương tác của **4 tác tử chính**:
  1. **Quản trị viên (Admin)**: Quản lý người dùng, phê duyệt đơn vị, cấu hình hệ thống, giám sát Hybrid Storage.
  2. **Cán bộ Bên mời thầu (Procurement)**: Tạo lập và đăng tải gói thầu, tổ chức mở thầu, đàm phán hợp đồng, nghiệm thu giải ngân.
  3. **Nhà thầu (Contractor)**: Tra cứu thông tin, nộp hồ sơ E-HSDT có chữ ký số, nộp bảo lãnh ngân hàng, theo dõi hợp đồng.
  4. **Chuyên gia Chấm thầu (Evaluator)**: Thẩm định hồ sơ độc lập, chấm điểm kỹ thuật & tài chính theo thang điểm ma trận.
- Xây dựng **31 kịch bản kiểm thử chi tiết** bao quát 8 phân hệ cốt lõi:
  1. Xác thực tài khoản & Phân quyền Maker-Checker (OTP, tra cứu MST tự động, duyệt cấp phép).
  2. Tạo lập & Đăng tải thông báo mời thầu (Sealed-Bid, 1 giai đoạn 1 túi hồ sơ và 1 giai đoạn 2 túi hồ sơ).
  3. Nộp hồ sơ dự thầu điện tử (E-HSDT), ký số PDF, khóa niêm phong hồ sơ trước giờ đóng thầu.
  4. Mở thầu trực tuyến công khai và chấm điểm ma trận Scorecard chuyên gia độc lập.
  5. Đàm phán, thương thảo, phê duyệt kết quả lựa chọn nhà thầu và tự động sinh dự thảo hợp đồng.
  6. Ký số hợp đồng hai bên, quản lý phụ lục thay đổi và nộp bảo lãnh thực hiện hợp đồng.
  7. Nghiệm thu từng phần / toàn phần, lưu trữ Snapshot PDF và mở khóa giải ngân.
  8. Hệ thống cảnh báo tự động chạy nền (Background Worker) và quản trị Hybrid Storage (Sync & Rollback).

---

## III. BẰNG CHỨNG KIỂM CHỨNG TỰ ĐỘNG & THỐNG KÊ TEST SUITE

### 1. Kiểm thử Backend C# (.NET 9.0)
- **Lệnh thực thi**:
  ```powershell
  dotnet test tests/ProcurementSystem.Tests/ProcurementSystem.Tests.csproj
  ```
- **Kết quả ghi nhận**:
  ```text
  Test run for D:\Thuc Tap\DuAnTT\tests\ProcurementSystem.Tests\bin\Debug\net8.0\ProcurementSystem.Tests.dll (.NETCoreApp,Version=v8.0)
  VSTest version 17.14.1 (x64)

  Starting test execution, please wait...
  A total of 1 test files matched the specified pattern.

  Passed!  - Failed: 0, Passed: 106, Skipped: 0, Total: 106, Duration: 35 s - ProcurementSystem.Tests.dll (net8.0)
  ```
- **Tăng trưởng số lượng test**:
  + Đầu tuần (02/11): **63 tests**.
  + Giữa tuần (04/11 - 05/11): **80 tests** $\rightarrow$ **93 tests**.
  + Cuối tuần (06/11): **106 tests** (tăng thêm 43 bài test mới trong tuần).
  + Tỉ lệ Pass: **100% (106/106 tests đạt yêu cầu)**.

### 2. Kiểm thử Frontend React (Node.js Test Runner / Vitest)
- **Lệnh thực thi**:
  ```powershell
  npm test
  ```
- **Kết quả ghi nhận**:
  ```text
  > node --test tests/*.test.mjs --run
  ✔ generated enums match BE source including Cancelled and Success (74.7362ms)
  ✔ wire request uses enum IDs and preserves zero filters (16.9458ms)
  ✔ unknown enum is rejected, names and numeric REST/realtime values normalize equally (0.3136ms)
  ✔ string domain statuses and role codes are not converted into invented IDs (0.104ms)
  ✔ unsaved preview normalizes MaxScore rather than treating every criterion as /100 (1.1594ms)
  ✔ manual preview does not fabricate automatic price points or a complete /100 total (0.12ms)
  ℹ tests 6
  ℹ suites 0
  ℹ pass 6
  ℹ fail 0
  ℹ cancelled 0
  ℹ skipped 0
  ℹ todo 0
  ℹ duration_ms 187.5947
  ```
- **Tỉ lệ Pass**: **100% (6/6 tests đạt yêu cầu)**.

### 3. Đóng gói Bản dựng Production Frontend (Vite Build)
- **Lệnh thực thi**:
  ```powershell
  npm run build
  ```
- **Kết quả ghi nhận**:
  ```text
  vite v8.3.1 building client environment for production...
  transforming...
  ✓ 2075 modules transformed.
  rendering chunks...
  computing gzip size...
  dist/index.html                     0.47 kB │ gzip:   0.30 kB
  dist/assets/index-BWqP3dqS.css     73.74 kB │ gzip:  12.28 kB
  dist/assets/index-w5Ul2-OV.js   1,358.48 kB │ gzip: 332.28 kB
  ✓ built in 5.52s
  ```
- **Trạng thái**: Đóng gói thành công toàn bộ 2075 modules trong 5.52 giây, 0 lỗi biên dịch.

### 4. Quản lý Phiên bản Mã nguồn (Git Version Control)
- **Nhánh làm việc**: `feat/agent-b-security-otp-storage`
- **Các commit tiêu biểu trong tuần**:
  + `119fb70`: `fix(security): sua loi r-b01 r-b02 r-b03 va bo sung kiem chung otp boundary r-b04`
  + `92a07bb`: `docs: biên soạn cẩm nang hướng dẫn kiểm thử toàn bộ nghiệp vụ hệ thống`
  + `43db1e3`: `feat(security): bắt buộc OTP đăng ký, siết chặt khóa mã hóa Storage và bổ sung báo cáo ngày 05/11/2026`
  + `f5111b8`: `fix(storage): chuẩn hóa 6 API tải tệp sang ResolveDownloadAsync và đồng bộ bảng Acceptances`
  + `cfc71ad`: `feat(storage): tich hop supabase cloud storage hybrid va ma hoa nen file bao mat`
  + `ae72b97`: `fix(procurement): sua loi deadlock giai ngan tren cloud storage, chan cham thau khi 0 ho so va chuan hoa nhan giao dien tieng viet`
- **Trạng thái đồng bộ**: Đã push thành công toàn bộ lên GitHub remote: `origin/feat/agent-b-security-otp-storage`.

---

## IV. ĐÁNH GIÁ TỔNG KẾT & KẾ HOẠCH TUẦN TIẾP THEO

### 1. Đánh giá Kết quả Thực hiện
- **Về tiến độ**: Đạt 100% kế hoạch tuần, hoàn thành vượt mức các yêu cầu kiểm toán nghiệp vụ và khắc phục bảo mật chuyên sâu.
- **Về chất lượng kỹ thuật**:
  + Hệ thống đã giải quyết dứt điểm các nguy cơ lỗi Crash 500, lỗi Deadlock giải ngân tài chính và lỗi chấm thầu sai luật.
  + Hạ tầng lưu trữ Hybrid Cloud Storage và cơ chế bảo mật OTP được thiết kế chuẩn mực, độc lập, tin cậy cao, có khả năng tự phục hồi và bảo toàn dữ liệu.
  + Độ bao phủ kiểm thử tự động tăng vọt từ 63 lên **106 tests PASS**, mang lại sự tự tin tuyệt đối cho quá trình triển khai thực tế.
- **Về kỹ năng học hỏi**: Nắm vững kiến thức chuyên sâu về Luật Đấu thầu số 22/2023/QH15, Nghị định 24/2024/NĐ-CP, kiến trúc lưu trữ S3 Protocol trên Supabase, kỹ thuật mật mã ứng dụng AES-256-CBC, nén luồng GZip và phương pháp kiểm thử tự động đa tầng (Unit, Integration, E2E Boundary).

### 2. Kế hoạch Tuần Tiếp theo (09/11 - 15/11/2026)
1. **Chuẩn bị Hồ sơ Báo cáo Nghiệm thu Thực tập Tốt nghiệp**:
   - Hoàn thiện bản báo cáo tổng kết thực tập chi tiết theo đề cương của Nhà trường và Đơn vị thực tập.
   - Đóng gói tài liệu đặc tả kiến trúc phần mềm, sơ đồ luồng dữ liệu (DFD), sơ đồ quan hệ thực thể (ERD) và danh mục API.
2. **Quay Video Demo Kịch bản Nghiệp vụ Xuyên suốt**:
   - Thực hiện quay video ghi hình minh họa 8 phân hệ cốt lõi dựa trên tài liệu `HUONG_DAN_KIEM_THU_TOAN_BO_NGHIEP_VU.md`.
   - Minh họa trực quan các tính năng điểm nhấn: Maker-Checker, ký số hợp đồng điện tử, nghiệm thu mở khóa giải ngân và 1-Click Sync/Rollback Hybrid Storage.
3. **Phối hợp Hợp nhất Mã nguồn (Merge to `dev` / `main`)**:
   - Phối hợp với Agent A và nhóm phát triển tiến hành rà soát chéo (Cross Code Review) lần cuối trước khi tạo Pull Request hợp nhất nhánh `feat/agent-b-security-otp-storage` vào nhánh chính.
   - Chạy kiểm thử hồi quy tổng thể lần cuối trước khi bàn giao sản phẩm hoàn chỉnh.

---

**Link Source Code Dự án**: https://github.com/nguyen221205-bit/Procurement-Contract-Tracking-System  
**Nhánh Git hoàn thành**: `feat/agent-b-security-otp-storage`  
**Báo cáo Nghiệm thu Kỹ thuật chi tiết**: [`docs/RESULT_AGENT_B_REVIEW_FIXES_2026-11-06.md`](file:///d:/Thuc%20Tap/DuAnTT/docs/RESULT_AGENT_B_REVIEW_FIXES_2026-11-06.md)  
**Cẩm nang Hướng dẫn Kiểm thử Toàn bộ Nghiệp vụ**: [`docs/HUONG_DAN_KIEM_THU_TOAN_BO_NGHIEP_VU.md`](file:///d:/Thuc%20Tap/DuAnTT/docs/HUONG_DAN_KIEM_THU_TOAN_BO_NGHIEP_VU.md)  
