# BÁO CÁO TỔNG HỢP CÔNG VIỆC THỰC HIỆN
## DỰ ÁN: HỆ THỐNG QUẢN LÝ ĐẤU THẦU & THEO DÕI HỢP ĐỒNG (PROCUREMENT & CONTRACT TRACKING SYSTEM)
**Nền tảng:** ASP.NET Core 8 Web API | Entity Framework Core 8 | Microsoft SQL Server | Clean 3-Layer Architecture  
**Cơ sở pháp lý & quy chuẩn:** Luật Đấu thầu số 22/2023/QH15, Nghị định 24/2024/NĐ-CP, Tiêu chuẩn an toàn OWASP Top 10  
**Ngày cập nhật:** 16/10/2026

---

## MỤC LỤC
1. [Tổng Quan Dự Án & Phạm Vi Thực Hiện](#1-tổng-quan-dự-án--phạm-vi-thực-hiện)
2. [Chi Tiết Hạng Mục Công Việc, Vấn Đề Cần Xử Lý & Hướng Xử Lý](#2-chi-tiết-hạng-mục-công-việc-vấn-đề-cần-xử-lý--hướng-xử-lý)
   - [Nhóm P0: An Ninh Bảo Mật Cực Kỳ Nghiêm Trọng](#nhóm-p0-an-ninh-bảo-mật-cực-kỳ-nghiêm-trọng)
   - [Nhóm P1: Logic Nghiệp Vụ Đấu Thầu & Tính Toán Pháp Lý Trọng Yếu](#nhóm-p1-logic-nghiệp-vụ-đấu-thầu--tính-toán-pháp-lý-trọng-yếu)
   - [Nhóm P2: Kiểm Toán, Tự Động Hóa & Báo Cáo Quản Trị](#nhóm-p2-kiểm-toán-tự-động-hóa--báo-cáo-quản-trị)
   - [Nhóm P3: Chuẩn Hóa Mã Nguồn & Tài Liệu Kỹ Thuật](#nhóm-p3-chuẩn-hóa-mã-nguồn--tài-liệu-kỹ-thuật)
3. [Bảng Tổng Hợp Quy Tắc Bất Biến Cho Hệ Thống (Rules N1 – N26)](#3-bảng-tổng-hợp-quy-tắc-bất-biến-cho-hệ-thống-rules-n1--n26)
4. [Danh Sách Các Endpoint API Mới Bổ Sung](#4-danh-sách-các-endpoint-api-mới-bổ-sung)
5. [Kết Quả Biên Dịch & Nghiệm Thu Kỹ Thuật](#5-kết-quả-biên-dịch--nghiệm-thu-kỹ-thuật)

---

## 1. TỔNG QUAN DỰ ÁN & PHẠM VI THỰC HIỆN

Hệ thống **Procurement & Contract Tracking System** quản lý khép kín vòng đời từ khi phát hành hồ sơ mời thầu, tiếp nhận hồ sơ đề xuất, tổ chức chấm thầu đa giám khảo độc lập, trao thầu, thương thảo ký kết hợp đồng kinh tế đến nghiệm thu mốc thanh toán và giải ngân ngân sách.

Hệ thống đã trải qua quá trình rà soát, tái cấu trúc và hoàn thiện toàn diện qua 4 giai đoạn:
- **Vòng 1 (R1):** Khảo sát cấu trúc nghiệp vụ, phân loại mức độ ưu tiên theo chuẩn an ninh và quy định đấu thầu (P0, P1, P2, P3).
- **Vòng 2 (R2-1 → R2-12):** Chuẩn hóa tiêu chí giá tự động (`IsPriceCriteria`), cơ chế gia hạn thầu (`EXTEND_BID_PACKAGE_DEADLINE`), điều kiện chấm thầu đủ giám khảo, phòng vệ IDOR hợp đồng (`CreatedBy`), cơ chế thu hồi trao thầu (`revoke-award`), kiểm toán dữ liệu tự động (`AuditSaveChangesInterceptor`), chống injection file Excel/CSV.
- **Vòng 3 (R3-1 → R3-11):** Triệt tiêu các lỗ hổng xác thực hàng loạt, bảo mật độc lập tuyệt đối giữa các giám khảo trong Tổ chuyên gia, cơ chế thay thế giám khảo nguyên tử, ngăn ngừa nhà thầu có hợp đồng bị chấm dứt (`Terminated`) được chọn lại, phân định ranh giới kiểm toán, chuẩn hóa IP chống spoofing, tự động hóa tính điểm uy tín nhà thầu (`Rating`), bổ sung hệ thống báo cáo quản trị chuyên sâu.
- **Vòng 4 (R4-1 → R4-5 - P0 & P1):** Triệt tiêu triệt để lỗ hổng IDOR xem/tải HSDT trái thẩm quyền của Bên mời thầu / Giám khảo không thuộc gói thầu; che giấu điểm số và thứ hạng khi đang chấm thầu; ngăn ngừa xung đột lợi ích giữa vai trò Nhà thầu và các vai trò Nội bộ (Điều 6 Luật Đấu thầu 2023); bảo vệ Tổ chuyên gia tránh nguy cơ bị khóa/gỡ vai trò gây bế tắc gói thầu; cấp phát an toàn tài khoản giám khảo qua email và kích hoạt `MustChangePassword = true`; đồng bộ JWT Claims gỡ bỏ cờ chặn sau khi đổi mật khẩu; thiết lập cơ chế tự động đưa hồ sơ Nhà thầu/Bên mời thầu về `Pending` để thẩm định lại khi cập nhật giấy phép kinh doanh/hồ sơ pháp lý.

---

## 2. CHI TIẾT HẠNG MỤC CÔNG VIỆC, VẤN ĐỀ CẦN XỬ LÝ & HƯỚNG XỬ LÝ

---

### NHÓM P0: AN NINH BẢO MẬT CỰC KỲ NGHIÊM TRỌNG

#### 🔴 R3-1: Lỗ hổng tạo tài khoản hàng loạt bằng mật khẩu cố định qua nhập Excel
- **Vị trí phát hiện:** `ContractorService.ImportContractorsFromExcelAsync` (ProcurementSystem.Infrastructure).
- **Vấn đề cần xử lý:**
  - Khi quản trị viên nhập danh sách nhà thầu từ file Excel (.xlsx), hệ thống gán cứng mật khẩu mặc định `"Contractor@123"`, tự động cấp trạng thái `IsActive = true`, `VerificationStatus = "Approved"`, và điểm uy tín `Rating = 5.0`.
  - **Hệ quả / Rủi ro:** Bất kỳ ai biết địa chỉ email của nhà thầu đều có thể đăng nhập ngay lập tức với mật khẩu mặc định đã biết để xem thông tin nội bộ hoặc nộp hồ sơ thầu, hoàn toàn bỏ qua bước thẩm định Giấy phép kinh doanh (GPKD) và quy trình xác minh năng lực theo quy định Luật Đấu thầu.
- **Hướng xử lý & Giải pháp kỹ thuật:**
  1. **Khởi tạo mật khẩu ngẫu nhiên an toàn:** Thay thế chuỗi hash cố định bằng hàm sinh mật khẩu ngẫu nhiên đạt chuẩn mã hóa sử dụng `System.Security.Cryptography.RandomNumberGenerator.GetInt32(32, 127)`. Mật khẩu tạm thời được gửi trực tiếp đến email đại diện của nhà thầu qua `IEmailService.SendEmailAsync` kèm hướng dẫn kích hoạt tài khoản.
  2. **Bắt buộc đổi mật khẩu lần đầu (`MustChangePassword`):**
     - Bổ sung thuộc tính `public bool MustChangePassword { get; set; } = false;` vào thực thể `User`.
     - Tạo và áp dụng Migration EF Core: `20261015091936_AddMustChangePasswordToUser`.
     - Thiết lập cờ `user.MustChangePassword = true` cho mọi tài khoản được tạo từ file Excel.
     - Phát triển `MustChangePasswordMiddleware` đặt trong pipeline của `Program.cs`. Khi một token JWT chứa Claim `MustChangePassword = true`, middleware sẽ lập tức chặn tất cả các request đến các endpoint nghiệp vụ khác (trả về mã lỗi `HTTP 403 Forbidden` kèm thông báo tiếng Việt yêu cầu đổi mật khẩu) và chỉ cho phép truy cập duy nhất vào API `POST /api/auth/change-password` hoặc luồng đăng xuất.
  3. **Thiết lập mặc định hồ sơ năng lực:**
     - Đặt `Contractor.VerificationStatus = "Pending"`, yêu cầu tải lên Giấy phép kinh doanh để Admin thẩm định trước khi được phép nộp hồ sơ dự thầu.
     - Đặt `Contractor.Rating = 0` (thay vì 5.0 tối đa) để điểm tín nhiệm được tính toán khách quan dựa trên lịch sử thực hiện hợp đồng thực tế.
  4. **Ghi nhật ký kiểm toán:** Service ghi nhận sự kiện `Action = "IMPORT_CONTRACTORS"` vào `AuditLog` lưu số dòng nhập thành công, số dòng lỗi, danh sách email (tuyệt đối không ghi mật khẩu hay hash mật khẩu ra nhật ký).

---

#### 🔴 R4-1: Lỗ hổng IDOR xem/tải Hồ sơ dự thầu trái thẩm quyền & Rò rỉ điểm số, thứ hạng khi gói thầu đang Evaluating
- **Vị trí phát hiện:** `BidSubmissionsController` và `BidSubmissionService` (`GetSubmissionsByPackageAsync`, `GetSubmissionByIdAsync`, `GetSubmissionFileForDownloadAsync`, `GetMySubmissionsAsync`).
- **Vấn đề cần xử lý:**
  - `BidSubmissionsController` trước đây chỉ kiểm tra cờ chung `isInternalStaff = User.IsInRole("Evaluator") || User.IsInRole("Procurement")`. Cán bộ bên mời thầu hoặc giám khảo không được phân công chấm gói thầu vẫn xem được danh sách, giá thầu, tài liệu kỹ thuật và tải file đề xuất của mọi gói thầu khác.
  - Tại `GetSubmissionByIdAsync`, `GetSubmissionsByPackageAsync` và `GetMySubmissionsAsync`, các trường `TotalScore` và `Rank` bị trả về nguyên vẹn khi gói thầu đang ở trạng thái `BidPackageStatus.Evaluating`, khiến giám khảo và nhà thầu biết trước biến động điểm số và xếp hạng trước khi có quyết định phê duyệt trúng thầu (vi phạm N14 & N19).
- **Hướng xử lý & Giải pháp kỹ thuật:**
  1. Mở rộng chữ ký các phương thức trong `IBidSubmissionService` và `BidSubmissionService` nhận danh sách vai trò người dùng `IList<string> userRoles`.
  2. Bổ sung kiểm tra thẩm quyền chi tiết (ABAC):
     - Nếu có role `Admin`: toàn quyền truy cập.
     - Nếu có role `Procurement`: chỉ cho phép xem/tải nếu chính là người tạo gói thầu (`package.CreatedBy == userId`).
     - Nếu có role `Evaluator`: bắt buộc phải là thành viên trong Tổ chuyên gia của gói thầu đó (`_unitOfWork.Repository<BidPackageEvaluator>().Query().AnyAsync(e => e.BidPackageId == packageId && e.EvaluatorId == userId)`).
     - Nếu có role `Contractor`: chỉ được xem hồ sơ do chính doanh nghiệp mình nộp (`submission.ContractorId == contractor.Id`).
     - Người không thỏa mãn các điều kiện trên bị từ chối với lỗi `403 Forbidden`.
  3. Che giấu thông tin nhạy cảm tuyệt đối trong giai đoạn `Evaluating`:
     - Khi `package.Status == BidPackageStatus.Evaluating` và người gọi không phải `Admin` hoặc `Procurement` tạo gói: gán `dto.TotalScore = null` và `dto.Rank = null`.
     - Trong `GetMySubmissionsAsync`: ẩn `TotalScore = null` và `Rank = null` đối với nhà thầu cho đến khi gói thầu chính thức chuyển sang `Awarded` hoặc `Contracted`.

---

### NHÓM P1: LOGIC NGHIỆP VỤ ĐẤU THẦU & TÍNH TOÁN PHÁP LÝ TRỌNG YẾU

#### 🟠 R3-2: Lộ điểm chấm và thứ hạng giữa các Giám khảo trong giai đoạn đánh giá hồ sơ
- **Vị trí phát hiện:** `EvaluationController.GetRankings`, `EvaluationController.GetEvaluationSummary` và `EvaluationService.GetPackageRankingsAsync`.
- **Vấn đề cần xử lý:**
  - Khi gói thầu đang trong giai đoạn chấm thầu (`Status == BidPackageStatus.Evaluating`), endpoint `/rankings` và `/summary` cho phép tài khoản có vai trò `Evaluator` xem toàn bộ điểm số chi tiết của các giám khảo khác trong hội đồng, đồng thời hiển thị trước `TotalScore` và `Rank` tạm thời.
  - **Hệ quả / Rủi ro:** Vi phạm nguyên tắc bảo mật thông tin gói thầu và tính độc lập khách quan của các thành viên Tổ chuyên gia (Điều 16, Điều 19 Luật Đấu thầu 2023, Quy tắc N14, N19). Giám khảo có thể nhìn thấy điểm của đồng nghiệp để thiên vị hoặc điều chỉnh điểm số của mình nhằm thao túng kết quả chung cuộc.
- **Hướng xử lý & Giải pháp kỹ thuật:**
  1. Mở rộng chữ ký phương thức `GetPackageRankingsAsync` và `GetEvaluationSummaryAsync` nhận tham số `int currentUserId, bool isInternalStaff`.
  2. Xác minh giám khảo gọi API bắt buộc phải thuộc danh sách phân công chính thức trong bảng liên kết `BidPackageEvaluator` của gói thầu tương ứng.
  3. Khi gói thầu ở trạng thái `Evaluating` và người gọi là thành viên `Evaluator` (không phải `Admin` hoặc `Procurement` tạo gói):
     - **Che dấu điểm số:** Lọc danh sách `EvaluationScores` chỉ trả về bản ghi điểm do chính `currentUserId` đó chấm; toàn bộ điểm của các giám khảo khác bị loại bỏ khỏi payload phản hồi.
     - **Ẩn xếp hạng:** Gán `TotalScore = null` và `Rank = null`.
     - **Ẩn tóm tắt thống kê:** Trong `GetEvaluationSummaryAsync`, ẩn toàn bộ các trường `HighestScore`, `LowestScore`, `AverageScore` đối với vai trò Giám khảo.
  4. Khi gói thầu đã có quyết định phê duyệt trao thầu (`Status >= BidPackageStatus.Awarded`), toàn bộ bảng điểm và thứ hạng mới được công khai minh bạch cho hội đồng theo quy định.

---

#### 🟠 R3-3: Không thể thay thế Giám khảo Tổ chuyên gia khi gói thầu đang chấm điểm
- **Vị trí phát hiện:** `BidPackageService.AssignEvaluatorAsync` & `RemoveEvaluatorAsync`.
- **Vấn đề cần xử lý:**
  - Quy tắc N4 quy định số lượng thành viên Tổ chuyên gia luôn phải là số lẻ $\ge 3$. Trong quá trình thực hiện gói thầu, nếu một giám khảo gặp lý do bất khả kháng (ốm đau, xung đột lợi ích phát sinh) cần thay người, việc gọi tuần tự `RemoveEvaluatorAsync` (làm giảm số lượng xuống số chẵn hoặc $< 3$) rồi mới `AssignEvaluatorAsync` sẽ luôn bị hệ thống chặn lại và báo lỗi.
  - Nếu nới lỏng kiểm tra ở từng API đơn lẻ thì sẽ vi phạm quy định pháp lý về tính hợp lệ của Tổ chuyên gia.
- **Hướng xử lý & Giải pháp kỹ thuật:**
  1. **Bổ sung DTO:** Tạo `ReplaceEvaluatorRequest` gồm `OldEvaluatorId`, `NewEvaluatorId` và `Reason` (bắt buộc $\ge 5$ ký tự).
  2. **Thực thi nguyên tử trong 1 Transaction:** Xây dựng phương thức `ReplaceEvaluatorAsync` trong `BidPackageService`:
     - Kiểm tra trạng thái gói thầu: Cho phép khi gói thầu ở trạng thái `Open`, `Closed` hoặc `Evaluating`.
     - Kiểm tra xung đột lợi ích toàn diện cho giám khảo mới: Giám khảo mới phải có tài khoản đang kích hoạt (`IsActive`), vai trò `Evaluator`, không thuộc nhân sự của bất kỳ nhà thầu nào đang tham gia nộp thầu trong gói, và không phải là người tạo gói thầu (`CreatedBy`).
     - Thực hiện tráo đổi trong cùng một transaction: Xóa phân công của `OldEvaluatorId` trong `BidPackageEvaluator` và thêm phân công cho `NewEvaluatorId`, bảo đảm tổng số lượng thành viên luôn giữ nguyên là số lẻ $\ge 3$ tại mọi thời điểm commit.
     - **Bảo lưu dữ liệu lịch sử:** Điểm số do giám khảo cũ đã chấm trong bảng `EvaluationScore` được giữ nguyên vẹn để phục vụ thanh tra, kiểm toán pháp lý (Quy tắc N17). Tuy nhiên, khi tính toán tổng điểm và xếp hạng gói thầu, thuật toán chỉ quét qua các giám khảo hiện diện trong danh sách `assignedEvaluatorIds` mới nhất.
     - **Tự động tính toán lại:** Khi thay thế giám khảo trong giai đoạn `Evaluating`, hệ thống tự động gọi `RecalculatePackageRankingsAsync`: các hồ sơ thầu chưa được giám khảo mới cho điểm sẽ tự động chuyển về `Submitted` với `Rank = null` cho đến khi người mới hoàn tất việc chấm điểm.
     - Lưu vết sự kiện `Action = "REPLACE_EVALUATOR"` vào `AuditLog`.
  3. Bổ sung endpoint: `POST /api/bid-packages/{id}/evaluators/replace`.

---

#### 🟠 R3-4: Lỗ hổng cho phép chọn lại nhà thầu đã bị chấm dứt hợp đồng do vi phạm
- **Vị trí phát hiện:** `EvaluationService.FinalizeEvaluationAsync` và `EvaluationService.RevokeAwardAsync`.
- **Vấn đề cần xử lý:**
  - Khi một nhà thầu đã ký hợp đồng nhưng vi phạm nghiêm trọng dẫn đến việc hợp đồng bị chấm dứt (`ContractStatus.Terminated`), bên mời thầu thực hiện thu hồi quyết định trao thầu (`revoke-award`).
  - Hệ thống trước đây khôi phục toàn bộ hồ sơ về `Evaluated`, cho phép người dùng vô tình hoặc cố ý chọn lại chính nhà thầu vi phạm đó trong lần trao thầu tiếp theo (`FinalizeEvaluationAsync`).
  - **Hệ quả / Rủi ro:** Vi phạm nghiêm trọng Luật Đấu thầu 2023 về cấm giao kết hợp đồng với nhà thầu đã bị chấm dứt hợp đồng do vi phạm nghĩa vụ thực hiện (Quy tắc N16).
- **Hướng xử lý & Giải pháp kỹ thuật:**
  1. **Tách biệt trường lý do:** Thêm cột `DisqualificationReason` (nvarchar(1000)) vào thực thể `BidSubmission`. Tạo và thực thi Migration EF Core `20261015093345_AddDisqualificationReasonToBidSubmission`.
  2. **Chặn lựa chọn tại `FinalizeEvaluationAsync`:**
     - Trước khi lưu kết quả trao thầu, kiểm tra xem nhà thầu được chọn có bất kỳ hợp đồng nào thuộc cùng gói thầu đang ở trạng thái `ContractStatus.Terminated` hay không.
     - Nếu có, lập tức trả về lỗi từ chối kèm thông báo căn cứ pháp lý.
  3. **Tự động truất quyền tại `RevokeAwardAsync`:**
     - Khi thu hồi trao thầu vì lý do hợp đồng bị chấm dứt (`Terminated`), hệ thống tự động chuyển trạng thái hồ sơ của nhà thầu vi phạm thành `BidSubmissionStatus.Disqualified`.
     - Lưu lý do vi phạm vào trường `DisqualificationReason`, xóa `Rank = null`, bảo đảm nhà thầu này bị loại trừ hoàn toàn khỏi danh sách xét thầu tiếp theo.

---

#### 🟠 R4-2: Xung đột lợi ích (Điều 6 Luật Đấu thầu 2023), làm gãy Hội đồng chấm thầu khi đổi Role & Thiếu AuditLog
- **Vị trí phát hiện:** `UserService.AssignRolesAsync`, `UserService.ToggleUserStatusAsync` và `UsersController.AssignRoles`.
- **Vấn đề cần xử lý:**
  - `AssignRolesAsync` cho phép gán vai trò `Contractor` song song với các vai trò nội bộ `["Admin", "Procurement", "Evaluator"]`, hoặc gán vai trò nội bộ cho người dùng đã liên kết hồ sơ trong bảng `Contractor`, vi phạm nghiêm trọng Điều 6 & Điều 16 Luật Đấu thầu 2023.
  - Cho phép Admin tự gỡ vai trò `Admin` của chính mình (`id == currentUserId`).
  - Cho phép gỡ vai trò `Evaluator` hoặc khóa tài khoản (`ToggleUserStatusAsync`) của giám khảo đang có phân công chấm thầu trong gói thầu ở trạng thái `Open` hoặc `Evaluating`, gây bế tắc (deadlock) quy trình chấm thầu do quy tắc N6 đòi hỏi 100% giám khảo active phải hoàn thành chấm điểm.
  - Thao tác gán quyền và khóa tài khoản không tự động lọt vào `AuditSaveChangesInterceptor` (do không thuộc 7 thực thể cốt lõi) và không có log nghiệp vụ thủ công.
- **Hướng xử lý & Giải pháp kỹ thuật:**
  1. Trong `UserService.AssignRolesAsync`:
     - Kiểm tra nếu danh sách role mới chứa `Contractor` đồng thời với bất kỳ role nào trong `Admin`, `Procurement`, `Evaluator` $\rightarrow$ Từ chối với lỗi `400 BadRequest`.
     - Kiểm tra nếu user đã có bản ghi trong bảng `Contractors` mà danh sách role mới chứa bất kỳ role nội bộ nào $\rightarrow$ Từ chối với lỗi `400 BadRequest`.
     - Chặn Admin tự tước quyền `Admin` của chính mình (`currentUserId == id`).
  2. Bảo vệ tính toàn vẹn của Hội đồng chấm thầu:
     - Trong `AssignRolesAsync` (nếu gỡ role `Evaluator`) và `ToggleUserStatusAsync` (nếu khóa tài khoản `isActive == false`): kiểm tra xem user có đang là thành viên active (`IsActive == true`) trong `BidPackageEvaluator` của bất kỳ gói thầu nào có trạng thái `Open` hoặc `Evaluating`. Nếu có $\rightarrow$ Chặn thao tác, yêu cầu bên mời thầu thực hiện thủ tục thay thế giám khảo (`replaceEvaluator`) trước khi gỡ quyền/khóa tài khoản.
  3. Ghi vết kiểm toán: Thêm bản ghi `AuditLog` với `Action = "ASSIGN_USER_ROLES"` và `Action = "TOGGLE_USER_STATUS"` kèm thông tin chi tiết.

---

#### 🟠 R4-3: Cấp tài khoản Tổ chuyên gia không bật `MustChangePassword`, lộ mật khẩu tĩnh & Tách rời `SaveChangesAsync`
- **Vị trí phát hiện:** `EvaluatorProposalService.ApproveProposalAsync` và `CreateProposalAsync`.
- **Vấn đề cần xử lý:**
  - Khi Admin phê duyệt đề xuất bổ sung giám khảo mới (`ApproveProposalAsync`), Admin truyền mật khẩu tĩnh (`request.Password`). Bản ghi `User` được tạo với `MustChangePassword = false` (mặc định), không có email gửi cho chuyên gia; mật khẩu tĩnh được chuyển giao thủ công qua kênh không an toàn, vi phạm tính độc lập bảo mật tài khoản (Rule N13).
  - Trong `CreateProposalAsync`, phương thức gọi `_unitOfWork.SaveChangesAsync()` 2 lần tách rời (lần 1 lưu Proposal, lần 2 lưu Notification/AuditLog) mà không có transaction, vi phạm tính nguyên tử.
- **Hướng xử lý & Giải pháp kỹ thuật:**
  1. Trong `EvaluatorProposalService.ApproveProposalAsync`:
     - Nếu không truyền mật khẩu (`string.IsNullOrWhiteSpace(request.Password)`): tự sinh mật khẩu ngẫu nhiên bảo mật 12 ký tự qua `RandomNumberGenerator`.
     - Luôn thiết lập `newUser.MustChangePassword = true` cho tài khoản giám khảo mới được tạo.
     - Tích hợp `IEmailService.SendEmailAsync` gửi thông tin đăng nhập và mật khẩu khởi tạo trực tiếp tới `proposal.Email` của giám khảo, kèm thông báo yêu cầu đổi mật khẩu ngay lần đầu đăng nhập.
  2. Trong `CreateProposalAsync`: Gộp tất cả các thao tác thêm thực thể (Proposal, Notification, AuditLog) và thực thi trong một transaction duy nhất kèm cơ chế dọn dẹp file tự động nếu rollback.

---

#### 🟠 R4-4: Token JWT cũ bị kẹt Claim `"MustChangePassword"` sau khi đổi mật khẩu & Middleware chặn `/api/auth/refresh-token`
- **Vị trí phát hiện:** `MustChangePasswordMiddleware`, `Program.cs` (`OnTokenValidated`) và `AuthService.ChangePasswordAsync`.
- **Vấn đề cần xử lý:**
  - Khi tài khoản có cờ `MustChangePassword = true` đăng nhập, token JWT chứa claim `"MustChangePassword": "true"`. Sau khi đổi mật khẩu thành công (DB đã cập nhật `false`), nếu người dùng tiếp tục dùng token cũ hoặc làm mới phiên, `MustChangePasswordMiddleware` lại chặn luôn cả endpoint `/api/auth/refresh-token` (HTTP 403), khiến người dùng bị kẹt hoàn toàn và buộc phải đăng xuất ra đăng nhập lại.
- **Hướng xử lý & Giải pháp kỹ thuật:**
  1. Trong `MustChangePasswordMiddleware`: Bổ sung `/api/auth/refresh-token` và `/api/auth/me` vào danh sách ngoại lệ được phép đi qua (`allowedPaths`).
  2. Trong `Program.cs` (`OnTokenValidated`): Khi token được xác thực, kiểm tra `user.MustChangePassword == false` trong CSDL, nếu có claim `"MustChangePassword"` trong identity thì tự động xóa bỏ claim này (`identity.RemoveClaim`).
  3. Trong `AuthService.ChangePasswordAsync`: Thay đổi kiểu trả về thành `ApiResponse<LoginResponse>`, tự động sinh và trả về luôn bộ Token mới (`AccessToken` sạch claim và `RefreshToken` mới) ngay sau khi đổi mật khẩu thành công.

---

#### 🟠 R4-5: Tải GPKD mới không reset trạng thái về `"Pending"` & Bế tắc không cho nộp/xét duyệt lại khi hồ sơ bị `"Rejected"`
- **Vị trí phát hiện:** `ContractorService.UpdateCurrentContractorProfileAsync`, `ProcuringEntityService` và `ProcuringEntitiesController`.
- **Vấn đề cần xử lý:**
  - Khi nhà thầu đã được duyệt (`VerificationStatus == "Approved"`) tải lên file GPKD mới, hệ thống cập nhật file nhưng vẫn giữ nguyên `VerificationStatus = "Approved"`, tạo kẽ hở thay đổi pháp nhân không qua thẩm định.
  - Khi nhà thầu bị từ chối (`VerificationStatus == "Rejected"`), sau khi họ tải lên GPKD mới, trạng thái vẫn giữ nguyên `"Rejected"`, không chuyển về `"Pending"`, khiến quy trình thẩm định bị bế tắc.
  - Bên mời thầu hoàn toàn không có API để cập nhật lại thông tin và quyết định thành lập / bổ nhiệm khi hồ sơ bị `"Rejected"`.
- **Hướng xử lý & Giải pháp kỹ thuật:**
  1. Trong `ContractorService.UpdateCurrentContractorProfileAsync`:
     - Nếu nhà thầu tải lên file GPKD mới (`request.BusinessLicenseFile != null`) hoặc đang ở trạng thái `"Rejected"`: tự động chuyển `contractor.VerificationStatus = "Pending"`.
     - Gửi `Notification` đến tất cả các tài khoản `Admin` và ghi nhận sự kiện `Action = "UPDATE_CONTRACTOR_LICENSE"` vào `AuditLog`.
  2. Bổ sung DTO `UpdateProcuringEntityProfileRequest` và cập nhật `IProcuringEntityService`, `ProcuringEntityService`:
     - Triển khai phương thức `UpdateCurrentProfileAsync(int userId, UpdateProcuringEntityProfileRequest request)`.
     - Cho phép Bên mời thầu cập nhật thông tin và tệp quyết định thành lập; tự động chuyển `VerificationStatus = "Pending"` khi hồ sơ đang ở trạng thái `"Rejected"`.
     - Bổ sung endpoint `PUT /api/procuring-entities/me` với phân quyền `[Authorize(Roles = "Procurement")]` và `[Consumes("multipart/form-data")]`.

---

### NHÓM P2: KIỂM TOÁN, TỰ ĐỘNG HÓA & BÁO CÁO QUẢN TRỊ

#### 🟡 R3-5: File scan PDF hợp đồng bị bỏ lại thành tệp mồ côi khi thu hồi trao thầu
- **Vị trí phát hiện:** `EvaluationService.RevokeAwardAsync`.
- **Vấn đề cần xử lý:**
  - Khi thu hồi trao thầu, hệ thống xóa các bản ghi hợp đồng `Draft` trong cơ sở dữ liệu (`_unitOfWork.Repository<Contract>().Delete(draft)`), nhưng không xóa tệp tin PDF hợp đồng đã ký quét scan lưu trên ổ đĩa vật lý (`ScannedFilePath`).
  - **Hệ quả / Rủi ro:** Rác bộ nhớ lưu trữ, lộ lọt dữ liệu dự thảo hợp đồng cũ (Quy tắc N17).
- **Hướng xử lý & Giải pháp kỹ thuật:**
  - Trước khi thực hiện lệnh xóa bản ghi CSDL, gom toàn bộ đường dẫn `draft.ScannedFilePath` hợp lệ vào một danh sách tạm thời.
  - Sau khi giao dịch CSDL đã commit thành công (`await _unitOfWork.SaveChangesAsync()`), duyệt qua danh sách và gọi `_fileStorageService.DeleteFile(filePath)` để dọn sạch hoàn toàn các tệp tin scan trên đĩa cứng.

---

#### 🟡 R3-6: Trùng lặp bản ghi kiểm toán giữa SaveChangesInterceptor và Service
- **Vị trí phát hiện:** `AuditSaveChangesInterceptor` và các phương thức nghiệp vụ trong `BidPackageService`.
- **Vấn đề cần xử lý:**
  - `AuditSaveChangesInterceptor` được thiết kế để tự động bắt các thao tác CUD dữ liệu trên 7 entity cốt lõi (`BidPackage`, `BidSubmission`, `EvaluationScore`, `Contract`, `ContractMilestone`, `Acceptance`, `ProgressUpdate`).
  - Khi đổi trạng thái gói thầu thông thường, interceptor đã ghi nhận một bản ghi `Action = "UPDATE"`. Tuy nhiên, service lại ghi thêm một bản ghi `Action = "CHANGE_BID_PACKAGE_STATUS"` với nội dung tương tự, gây nhiễu bảng `AuditLog` và tăng kích thước CSDL không cần thiết.
- **Hướng xử lý & Giải pháp kỹ thuật:**
  - Thống nhất nguyên tắc phân định trách nhiệm rõ ràng:
    - **Interceptor:** Chịu trách nhiệm hoàn toàn về các thay đổi dữ liệu bảng (Data-level mutations: `CREATE`, `UPDATE`, `DELETE`) với đầy đủ `OldValues` và `NewValues`.
    - **Service:** Chỉ ghi nhận thủ công đối với các **sự kiện nghiệp vụ chuyên biệt (Business Domain Events)** có ý nghĩa pháp lý cao kèm lý do giải trình: `IMPORT_CONTRACTORS`, `REVOKE_AWARD`, `DISQUALIFY_SUBMISSION`, `REPLACE_EVALUATOR`, `EXTEND_BID_PACKAGE_DEADLINE`.
  - Trong `BidPackageService.ChangeStatusAsync`, loại bỏ ghi trùng `CHANGE_BID_PACKAGE_STATUS`, chỉ ghi thủ công khi hành động đó thực sự là `EXTEND_BID_PACKAGE_DEADLINE` kèm lý do gia hạn của Bên mời thầu.

---

#### 🟡 R3-7: Địa chỉ IP kiểm toán lấy từ header `X-Forwarded-For` chưa được kiểm chứng
- **Vị trí phát hiện:** `AuditSaveChangesInterceptor.GetCurrentIpAddress`.
- **Vấn đề cần xử lý:**
  - Phương thức `GetCurrentIpAddress` đọc trực tiếp giá trị chuỗi từ header `httpContext.Request.Headers["X-Forwarded-For"]`.
  - **Hệ quả / Rủi ro:** Kẻ tấn công có thể dễ dàng chèn header giả mạo (ví dụ `X-Forwarded-For: 127.0.0.1` hoặc IP của người khác) vào gói tin HTTP để làm sai lệch nhật ký điều tra an ninh số trong CSDL (vi phạm Quy tắc N18).
- **Hướng xử lý & Giải pháp kỹ thuật:**
  1. Cấu hình middleware chuẩn của ASP.NET Core trong `Program.cs`:
     ```csharp
     builder.Services.Configure<ForwardedHeadersOptions>(options =>
     {
         options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
         options.KnownNetworks.Clear();
         options.KnownProxies.Clear();
         // Chỉ tin cậy các mạng proxy nội bộ / loopback
         options.KnownNetworks.Add(new IPNetwork(System.Net.IPAddress.Loopback, 8));
         options.KnownNetworks.Add(new IPNetwork(System.Net.IPAddress.IPv6Loopback, 128));
     });
     // Kích hoạt ngay sau Exception Middleware
     app.UseForwardedHeaders();
     ```
  2. Tại `AuditSaveChangesInterceptor.GetCurrentIpAddress()`: Xóa bỏ việc đọc trực tiếp header thô, chỉ lấy duy nhất giá trị từ `httpContext.Connection?.RemoteIpAddress?.ToString()`, vốn đã được ASP.NET Core xác thực an toàn thông qua danh sách proxy tin cậy.

---

#### 🟡 R3-8: Lưu nhầm lý do truất quyền vào trường `SelectionReason`
- **Vị trí phát hiện:** `EvaluationService.DisqualifySubmissionAsync` và các DTO xếp hạng.
- **Vấn đề cần xử lý:**
  - Khi truất quyền nhà thầu (`DisqualifySubmissionAsync`), lý do loại hồ sơ bị ghi đè vào thuộc tính `SelectionReason`.
  - **Hệ quả / Rủi ro:** Gây nhập nhằng về ngữ nghĩa dữ liệu, dễ dẫn đến việc hiển thị sai lệch trên giao diện người dùng và báo cáo thẩm định (nhầm lẫn giữa lý do trúng thầu và lý do bị loại).
- **Hướng xử lý & Giải pháp kỹ thuật:**
  - Bổ sung trường `DisqualificationReason` độc lập vào thực thể `BidSubmission`.
  - Cập nhật các DTO: `BidSubmissionDto`, `BidSubmissionSummaryDto`, `SubmissionRankingDto` chứa thuộc tính `DisqualificationReason`.
  - Cập nhật `DisqualifySubmissionAsync`: gán lý do vào `DisqualificationReason`, đồng thời đặt `SelectionReason = null` và `Rank = null`.

---

#### 🟡 R3-9: Điểm uy tín (`Rating`) nhà thầu chỉ được tính khi gọi API thủ công
- **Vị trí phát hiện:** `ContractService.ChangeStatusAsync`, `ContractService.ApproveMilestoneAcceptanceAsync` và `ContractorService.RecalculateContractorRatingAsync`.
- **Vấn đề cần xử lý:**
  - Điểm tín nhiệm nhà thầu (`Rating` từ 1.0 đến 5.0) là chỉ số phản ánh năng lực thực tế dựa trên số hợp đồng bị hủy (`Terminated`) và số biên bản nghiệm thu mốc bị từ chối (`Rejected`).
  - Trước đây, điểm số này chỉ được cập nhật khi Admin bấm gọi API thủ công. Nếu hợp đồng bị chấm dứt hoặc mốc công việc bị từ chối nghiệm thu, điểm của nhà thầu vẫn giữ nguyên, làm sai lệch hồ sơ năng lực khi tham gia các gói thầu sau.
- **Hướng xử lý & Giải pháp kỹ thuật:**
  1. Inject interface `IContractorService` vào `ContractService`.
  2. Tự động kích hoạt gọi `RecalculateContractorRatingAsync(contract.ContractorId)` ngay sau khi CSDL commit thành công trong các trường hợp:
     - Trong `ChangeStatusAsync`: Khi trạng thái hợp đồng chuyển sang `ContractStatus.Terminated` (vi phạm bị chấm dứt) hoặc `ContractStatus.Completed` (hoàn thành xuất sắc).
     - Trong `ApproveMilestoneAcceptanceAsync`: Khi nghiệm thu mốc bị từ chối (`request.IsApproved == false` $\rightarrow$ `AcceptanceStatus.Rejected`) hoặc khi hợp đồng tự động hoàn tất nghiệm thu 100%.

---

#### 🟡 R3-10: Thiếu các báo cáo thống kê chuyên sâu cấp quản trị
- **Vị trí phát hiện:** `ReportService` và `ReportsController`.
- **Vấn đề cần xử lý:**
  - Hệ thống chỉ có dashboard chung, thiếu các báo cáo phân tích theo chu kỳ năm, phân loại gói thầu, tỷ lệ trúng/trượt thầu của từng nhà thầu và báo cáo cảnh báo hợp đồng sắp hết hạn/chậm tiến độ mốc.
- **Hướng xử lý & Giải pháp kỹ thuật:**
  1. Thiết kế 3 bộ DTO mới chuẩn hóa:
     - `ProcurementSummaryReportDto`: Thống kê tổng hợp số gói thầu, tổng dự toán, tổng giá trị hợp đồng ký kết, số tiền tiết kiệm và tỷ lệ tiết kiệm chi phí; kèm cơ cấu theo loại gói (`Goods`, `Construction`, `Service`) và theo trạng thái vòng đời.
     - `ContractorPerformanceReportDto`: Thống kê tỷ lệ trúng thầu (`WinRate`), số gói tham gia, số gói trúng, số gói trượt, số gói bị truất quyền và tổng giá trị trúng thầu tích lũy của từng nhà thầu.
     - `ContractProgressReportDto`: Đo lường tỷ lệ hoàn thành mốc đúng tiến độ (`OnTimeRate`), danh sách hợp đồng đang hoạt động sắp đến hạn (`daysAhead`) và các hợp đồng đã quá hạn (`IsOverdue`) kèm tỷ lệ giải ngân thực tế.
  2. Hiện thực hóa các phương thức trong `ReportService` sử dụng truy vấn tối ưu `.AsNoTracking()`.
  3. Bổ sung 3 endpoint RESTful tại `ReportsController` với bảo vệ phân quyền `[Authorize(Roles = "Admin,Procurement")]`:
     - `GET /api/reports/procurement-summary?year=...&type=...`
     - `GET /api/reports/contractor-performance`
     - `GET /api/reports/contracts-progress?daysAhead=30`

---

### NHÓM P3: CHUẨN HÓA MÃ NGUỒN & TÀI LIỆU KỸ THUẬT

#### 🟢 R3-11: Chuẩn hóa hằng số trạng thái & hoàn thiện tài liệu hướng dẫn
- **Vấn đề cần xử lý:**
  - Chuỗi literal `"Rejected"` còn sót rải rác trong `BidPackageService.cs` thay vì sử dụng hằng số tập trung `BidSubmissionStatus.Rejected`.
  - Tài liệu `README.md` và `REVIEW_AND_AGENT_RULES.md` chưa phản ánh đầy đủ các API mới phát triển của Vòng 3.
- **Hướng xử lý & Giải pháp kỹ thuật:**
  1. Thay thế toàn bộ literal `"Rejected"` bằng `BidSubmissionStatus.Rejected`.
  2. Bổ sung mục **Bước 10 (Các tính năng & API Quản trị Nâng cao - Vòng 3)** vào `README.md`.
  3. Cập nhật mục **L. Trạng thái Hoàn Thành Backlog Vòng 3** trong `REVIEW_AND_AGENT_RULES.md`.

---

## 3. BẢNG TỔNG HỢP QUY TẮC BẤT BIẾN CHO HỆ THỐNG (RULES N1 – N19)

Mọi sửa đổi trong mã nguồn của hệ thống bắt buộc phải tuân thủ nghiêm ngặt 19 quy tắc bất biến sau:

| Mã | Tên Quy Tắc | Mô Tả Yêu Cầu Kỹ Thuật & Căn Cứ Nghiệp Vụ |
|---|---|---|
| **N1** | Phân quyền RBAC nghiêm ngặt | Mọi API nghiệp vụ phải khai báo `[Authorize(Roles = "...")]`. Không có endpoint nghiệp vụ nào được mở ẩn danh (`AllowAnonymous`) ngoại trừ Login, Register và tra cứu danh mục công khai. |
| **N2** | Bảo mật Hồ sơ Thầu (Sealed-Bid) | Tệp đề xuất tài chính và kỹ thuật của nhà thầu không được lưu trong thư mục web công khai (`wwwroot`). Chỉ được tải tệp khi gói thầu đã đóng thầu và người yêu cầu có thẩm quyền. |
| **N3** | Phòng vệ lỗ hổng IDOR Hợp đồng | Mọi thao tác sửa đổi điều khoản, thêm/sửa/xóa mốc, tải bản scan và nghiệm thu mốc thanh toán bắt buộc phải xác thực quyền sở hữu: người gọi phải là `Admin` hoặc là chính cán bộ `Procurement` đã tạo gói thầu (`BidPackage.CreatedBy == userId`). |
| **N4** | Quy tắc Hội đồng Chấm thầu Lẻ | Tổ chuyên gia đánh giá gói thầu bắt buộc phải có số lượng thành viên là số lẻ và tối thiểu từ 3 người trở lên ($\ge 3$) theo Luật Đấu thầu. |
| **N5** | Tiêu chí Giá tự động duy nhất | Mỗi gói thầu chỉ có tối đa 1 tiêu chí Giá (`IsPriceCriteria == true`). Tiêu chí này tính điểm tự động theo công thức luật định, nghiêm cấm mọi hành vi can thiệp hoặc cho điểm thủ công. |
| **N6** | Điều kiện Đủ điểm Chấm thầu | Hồ sơ dự thầu chỉ được chuyển sang trạng thái `Evaluated` khi **toàn bộ** giám khảo được phân công chính thức trong Tổ chuyên gia đều đã hoàn thành việc chấm điểm tất cả tiêu chí kỹ thuật. |
| **N7** | Cỗ máy Trạng thái 1 chiều (State Machine) | Tuyệt đối không cho phép quay lui trạng thái gói thầu về `Open` hoặc `Draft` sau khi đã mở thầu chấm điểm, trừ trường hợp có quyết định gia hạn thầu chính thức. |
| **N8** | Gia hạn thầu có thời hạn tương lai | Chỉ cho phép chuyển gói thầu từ `Closed` sang `Open` khi cung cấp tham số `NewDeadline` có giá trị lớn hơn thời điểm hiện tại (`UtcDateTime > DateTime.UtcNow`), ghi nhận AuditLog `EXTEND_BID_PACKAGE_DEADLINE`. |
| **N9** | Điều kiện Kích hoạt Hợp đồng | Hợp đồng chỉ được phép chuyển từ `Draft` sang `Active` khi: (1) Đã tải lên file scan hợp đồng có chữ ký, con dấu pháp lý (.pdf) và (2) Tổng giá trị các mốc thanh toán đúng bằng 100% giá trị hợp đồng. |
| **N10** | Thẩm định Năng lực Nhà thầu | Tài khoản nhà thầu tạo mới hoặc import hàng loạt bắt buộc ở trạng thái `Pending`. Chỉ sau khi Admin duyệt hồ sơ năng lực thì nhà thầu mới được nộp hồ sơ thầu. |
| **N11** | Chống Tấn Công Formula Injection | Khi xuất dữ liệu ra file CSV hoặc Excel, mọi chuỗi dữ liệu người dùng bắt đầu bằng các ký tự nguy hiểm (`=`, `+`, `-`, `@`, `\t`) bắt buộc phải được tiền tố bằng dấu nháy đơn `'` (CWE-1236). |
| **N12** | Quản lý File Đảm bảo Tính Toàn vẹn | Khi cập nhật hoặc xóa file đính kèm, chỉ xóa file vật lý cũ sau khi giao dịch CSDL đã commit thành công. Nếu upload lỗi, phải xóa ngay file tạm mới sinh để chống rác ổ đĩa. |
| **N13** | Mật khẩu Ngẫu nhiên & Đổi mật khẩu lần đầu | Nghiêm cấm tạo tài khoản với mật khẩu cố định. Tài khoản import hàng loạt phải sinh mật khẩu ngẫu nhiên an toàn, kích hoạt cờ `MustChangePassword = true` và bị middleware chặn toàn bộ API cho đến khi đổi mật khẩu mới. |
| **N14** | Độc lập Chấm điểm Tuyệt đối | Trong giai đoạn `Evaluating`, giám khảo chỉ được xem điểm của chính mình tại **mọi** endpoint (`/scores`, `/rankings`, `/summary`); hệ thống phải ẩn hoàn toàn `TotalScore` và `Rank`. |
| **N15** | Thay thế Giám khảo Nguyên tử | Thay thế thành viên Tổ chuyên gia phải thực hiện qua API `POST /evaluators/replace` trong 1 transaction duy nhất, bảo đảm số lượng thành viên luôn là số lẻ $\ge 3$ tại mọi thời điểm. |
| **N16** | Chặn Nhà thầu có HĐ Terminated | Nhà thầu đã có hợp đồng bị chấm dứt do vi phạm (`Terminated`) trong cùng gói thầu tuyệt đối không được phép chọn lại trong các lần trao thầu tiếp theo. |
| **N17** | Không Xóa Cứng Dữ Liệu Pháp Lý | Không xóa cứng hồ sơ thầu, bảng điểm hay hợp đồng có hiệu lực. Khi bắt buộc phải xóa hợp đồng Draft trong luồng thu hồi trao thầu, phải dọn dẹp sạch sẽ tệp scan PDF trên đĩa cứng. |
| **N18** | Chống Giả Mạo IP Client | Tuyệt đối không đọc trực tiếp header `X-Forwarded-For` chưa kiểm chứng. Phải chuẩn hóa qua `UseForwardedHeaders` với danh sách mạng proxy tin cậy và đọc từ `RemoteIpAddress`. |
| **N19** | Kiểm Thử Quyền Riêng Tư Dữ Liệu | Mọi endpoint trả về điểm số, xếp hạng hoặc hợp đồng phải bảo đảm nguyên tắc cô lập: "Người dùng A không được phép đọc dữ liệu mật của người dùng B". |
| **N20** | Kiểm soát Truy cập HSDT Chi tiết (Fine-grained IDOR) | Cán bộ nội bộ (`Evaluator`, `Procurement`) chỉ được phép xem danh sách, xem chi tiết và tải tệp HSDT khi và chỉ khi: là thành viên active trong Tổ chuyên gia của gói đó (`BidPackageEvaluator`), hoặc là người khởi tạo gói thầu (`CreatedBy == userId`), hoặc là `Admin`. Tuyệt đối không dùng cờ generic `isInternalStaff` mà không gắn liền với `packageId` cụ thể. |
| **N21** | Bảo vệ Điểm số & Xếp hạng Tuyệt đối trước Trao thầu | Trong thời gian gói thầu có trạng thái `Evaluating`, trường `TotalScore` và `Rank` phải được ẩn (`null`) ở tất cả các endpoint trả về dữ liệu nộp thầu (`/submissions`, `/submissions/{id}`, `/my-submissions`) đối với cả Giám khảo và Nhà thầu. Chỉ công khai điểm số khi gói thầu đã chuyển sang `Awarded` hoặc `Contracted`. |
| **N22** | Ngăn ngừa Xung đột Lợi ích (Điều 6 Luật Đấu thầu) | Tuyệt đối không cho phép tài khoản có vai trò `Contractor` (hoặc liên kết hồ sơ nhà thầu) được cấp các quyền `Evaluator`, `Procurement` hoặc `Admin`. Không cho phép gỡ vai trò hoặc khóa tài khoản của giám khảo đang có phân công chấm thầu active tại các gói thầu đang `Open` hoặc `Evaluating` (bắt buộc phải thực hiện thủ tục thay thế giám khảo `replaceEvaluator` trước). |
| **N23** | Cấp phát Tài khoản Giám khảo An toàn | Khi phê duyệt đề xuất bổ sung giám khảo mới, bắt buộc phải kích hoạt cờ `MustChangePassword = true`, tự sinh mật khẩu ngẫu nhiên bảo mật 12 ký tự nếu không truyền vào, và gửi thông tin qua email trực tiếp cho chuyên gia. |
| **N24** | Tính Nhất quán Thẩm định Hồ sơ Pháp lý | Mọi hành vi cập nhật Giấy phép kinh doanh (đối với Nhà thầu) hoặc Quyết định thành lập / bổ nhiệm (đối với Bên mời thầu) đều phải tự động đưa `VerificationStatus` về `"Pending"`, phát sinh `Notification` cho Admin và ghi nhận `AuditLog` để thẩm định lại. |
| **N25** | Bảo toàn Lịch sử Kỷ luật HSDT | Hồ sơ dự thầu đã bị truất quyền (`Disqualified`) tuyệt đối không được phép rút hồ sơ (`Withdraw`). Thao tác rút hồ sơ chỉ có hiệu lực duy nhất khi HSDT đang ở trạng thái `Submitted`. |
| **N26** | Chuẩn hóa Tải Tệp An toàn & Chống Cạn kiệt RAM | 100% các API tải tệp đính kèm trong hệ thống phải đi qua hàm chuẩn hóa an toàn `_fileStorageService.GetPhysicalFilePath(relativePath)` để chống Path Traversal (`../`), và trả về kết quả bằng luồng `PhysicalFile`, cấm sử dụng `File.ReadAllBytesAsync` gây tràn bộ nhớ. |

---

## 4. DANH SÁCH CÁC ENDPOINT API MỚI BỔ SUNG

| Phương Thức | Tuyến Đường (Route) | Quyền Hạn (Roles) | Mục Đích Sử Dụng |
|---|---|---|---|
| `POST` | `/api/auth/change-password` | Bắt buộc Token | Đổi mật khẩu bắt buộc cho người dùng (tự động cấp lại token mới sạch claim `MustChangePassword`). |
| `POST` | `/api/bid-packages/{id}/evaluators/replace` | `Admin,Procurement` | Thay thế giám khảo Tổ chuyên gia nguyên tử trong 1 transaction, giữ số lẻ $\ge 3$. |
| `POST` | `/api/evaluations/submissions/{id}/disqualify` | `Admin,Procurement` | Truất quyền dự thầu của nhà thầu vi phạm kèm lý do pháp lý độc lập. |
| `POST` | `/api/evaluations/packages/{id}/revoke-award` | `Admin,Procurement` | Thu hồi quyết định trao thầu, dọn file scan mồ côi, truất quyền nhà thầu bị hủy HĐ. |
| `PUT` | `/api/procuring-entities/me` | `Procurement` | Cập nhật hồ sơ Bên mời thầu (thông tin, quyết định thành lập / bổ nhiệm) và tự động chuyển về Pending khi hồ sơ đang bị Rejected. |
| `POST` | `/api/contractors/import-excel` | `Admin` | Nhập danh sách nhà thầu từ file Excel (.xlsx), sinh mật khẩu ngẫu nhiên an toàn. |
| `GET` | `/api/contractors/template-excel` | `Admin,Procurement` | Tải về file Excel mẫu chuẩn hóa phục vụ nhập dữ liệu nhà thầu. |
| `GET` | `/api/reports/procurement-summary` | `Admin,Procurement` | Báo cáo thống kê mua sắm, dự toán, giá trị ký kết và tiết kiệm theo năm/loại gói. |
| `GET` | `/api/reports/contractor-performance` | `Admin,Procurement` | Báo cáo tỷ lệ trúng/trượt thầu, số lượng tham gia và giá trị trúng thầu của các nhà thầu. |
| `GET` | `/api/reports/contracts-progress` | `Admin,Procurement` | Báo cáo tỷ lệ mốc đúng hạn, danh sách hợp đồng sắp đến hạn và hợp đồng quá hạn. |
| `GET` | `/api/reports/contractors/export-excel` | `Admin,Procurement` | Xuất danh sách toàn bộ nhà thầu ra file Excel định dạng .xlsx. |
| `GET` | `/api/reports/contracts/export-excel` | `Admin,Procurement` | Xuất danh sách toàn bộ hợp đồng ra file Excel định dạng .xlsx. |

---

## 5. KẾT QUẢ BIÊN DỊCH & NGHIỆM THU KỸ THUẬT

### Trạng thái biên dịch Solution (.NET 8 SDK)
```text
Command: dotnet build ProcurementSystem.slnx
Time: 16/10/2026 17:28:45 (UTC+7)

Determining projects to restore...
All projects are up-to-date for restore.
ProcurementSystem.Core -> D:\Thuc_Tap_backend\Procurement & Contract Tracking System\ProcurementSystem.Core\bin\Debug\net8.0\ProcurementSystem.Core.dll
ProcurementSystem.Infrastructure -> D:\Thuc_Tap_backend\Procurement & Contract Tracking System\ProcurementSystem.Infrastructure\bin\Debug\net8.0\ProcurementSystem.Infrastructure.dll
ProcurementSystem.API -> D:\Thuc_Tap_backend\Procurement & Contract Tracking System\ProcurementSystem.API\bin\Debug\net8.0\ProcurementSystem.API.dll

Build succeeded.
    0 Warning(s)
    0 Error(s)

Time Elapsed: 00:00:04.82
```

### Kết luận nghiệm thu
- Toàn bộ các yêu cầu chức năng, bảo mật và nghiệp vụ từ **Vòng 1, Vòng 2, Vòng 3 đến Vòng 4 (P0 & P1: R4-1 → R4-5)** đã được hiện thực hóa trọn vẹn, đồng bộ giữa các tầng Presentation (API), Core (Domain), Infrastructure (Data/Services) và CSDL.
- Hệ thống đáp ứng chuẩn hóa mô hình Clean Architecture, tuân thủ nghiêm ngặt Luật Đấu thầu 2023 (đặc biệt Điều 6 về ngăn ngừa xung đột lợi ích), bảo đảm an ninh phòng chống các lỗ hổng OWASP Top 10 (IDOR HSDT, Broken Access Control, Formula Injection, IP Spoofing, Sensitive Data Exposure).
- Các hạng mục P2 và P3 còn lại (R4-6 đến R4-10) được bàn giao cho teammate phụ trách theo đúng kế hoạch phân công.
- Báo cáo và tài liệu kỹ thuật (`README.md`, `REVIEW_AND_AGENT_RULES.md`, `TONG_HOP_CONG_VIEC_HOAN_THANH.md`, `BACKLOG_R4.md`) đã được cập nhật đầy đủ, sẵn sàng cho công tác kiểm thử, vận hành và báo cáo thực tập tốt nghiệp.
