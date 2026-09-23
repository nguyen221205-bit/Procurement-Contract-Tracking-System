# KẾ HOẠCH PHÂN CHIA CÔNG VIỆC KỸ THUẬT HÔM NAY
**Dự án:** Hệ thống Quản lý Đấu Thầu & Theo Dõi Hợp Đồng (*Procurement & Contract Tracking System*)  
**Ngày thực hiện:** 23/09/2026 (Thứ Tư - Tuần 5 - Ngày 16)  
**Nhóm thực hiện:** 2 thành viên (Dev 1 & Dev 2)  
**Mục tiêu tổng quát:** Chuẩn hóa & Tích hợp Toàn diện Swagger XML Documentation, Nâng cấp Bộ Sinh Dữ Liệu Mẫu Toàn Vẹn (Extended Data Seeder), Xác thực Hoàn thiện Luồng Nghiệp vụ Nghiệm thu - Giải ngân và Rà soát Kiến trúc Mã Nguồn Sẵn Sàng Bàn Giao.

---

## 📌 PHẦN 1: DEV 1 (Track 1 - Phạm Minh Tài)
> **Phạm vi trọng tâm:** Cấu hình Swagger XML Documentation hạ tầng, Xây dựng Bộ Sinh Dữ Liệu Mẫu Toàn Vẹn Hệ Thống (Extended Data Seeder) và Rà soát Mã Nguồn theo Chuẩn Clean Architecture.

### 1.1. Cấu hình & Tích hợp Swagger XML Documentation Toàn Hệ Thống
- **Cấu hình tệp dự án `ProcurementSystem.API.csproj`**:
  - Bật cờ sinh tệp tài liệu XML: `<GenerateDocumentationFile>true</GenerateDocumentationFile>`.
  - Bổ sung cấu hình bỏ qua cảnh báo thiếu comment không bắt buộc: `<NoWarn>$(NoWarn);1591</NoWarn>`.
- **Cập nhật `Program.cs`**:
  - Đăng ký `options.IncludeXmlComments(xmlPath)` trong khối cấu hình `AddSwaggerGen` để giao diện Swagger UI tự động đọc và hiển thị tài liệu mô tả cho tất cả Endpoints.
- **Bổ sung chú thích XML chuẩn mực cho các Controllers thuộc Track 1**:
  - `BidPackagesController.cs`: Mô tả các API tạo gói thầu, lọc gói thầu, cập nhật trạng thái thầu.
  - `EvaluationController.cs`: Mô tả các API tạo tiêu chí đánh giá, chấm điểm có trọng số, bảng xếp hạng thứ hạng và xuất dữ liệu trúng thầu (`AwardedBid`).
  - `ReportsController.cs`: Mô tả chi tiết các nhóm chỉ số KPI trong `ProcurementDashboardDto` (gói thầu, hồ sơ, nhà thầu, tài chính, tiết kiệm và giải ngân).
  - Khai báo đầy đủ các mã trạng thái phản hồi: `HTTP 200 OK`, `HTTP 201 Created`, `HTTP 400 Bad Request`, `HTTP 401 Unauthorized`, `HTTP 403 Forbidden`, `HTTP 404 Not Found`.

### 1.2. Nâng cấp Bộ Sinh Dữ Liệu Mẫu Toàn Vẹn Hệ Thống (Extended Data Seeder)
- **Mở rộng `DataSeeder.cs` trong `ProcurementSystem.Infrastructure/Seeders/`**:
  - Tự động sinh đầy đủ tài khoản người dùng cho cả 4 vai trò: `Admin`, `Procurement` (Chuyên viên mua sắm), `Evaluator` (Giám khảo chấm thầu), và 2 `Contractor` (Nhà thầu công nghệ & phần mềm).
  - Tự động sinh các gói thầu mẫu bao quát toàn bộ các trạng thái nghiệp vụ trong thực tế:
    - **Gói thầu 1 (Trạng thái `Open`)**: Gói thầu mua sắm thiết bị máy chủ, đang mở thầu công khai cho các nhà thầu nộp hồ sơ.
    - **Gói thầu 2 (Trạng thái `Evaluating`)**: Gói thầu phần mềm ERP, đã đóng thầu và đang trong pha hội đồng chấm điểm thẩm định theo bộ tiêu chí có trọng số.
    - **Gói thầu 3 (Trạng thái `Contracted`)**: Gói thầu hạ tầng đám mây Cloud, đã hoàn tất chấm điểm, xếp hạng và phê duyệt trúng thầu, đã ký hợp đồng kinh tế và đã thiết lập các mốc thanh toán nghiệm thu giải ngân.
  - Đảm bảo khi khởi chạy server hoặc triển khai trên máy tính bất kỳ, hệ thống lập tức có sẵn dữ liệu chuẩn mực để demo, kiểm thử liên thông và báo cáo bảo vệ thực tập mà không cần nhập liệu thủ công.

### 1.3. Rà soát & Tối ưu Mã Nguồn theo Chuẩn Clean Architecture (Code Review & Refactoring)
- Rà soát toàn bộ các tầng Controller, Service, Repository, DTOs:
  - Chuẩn hóa định dạng phản hồi nhất quán qua `ApiResponse<T>`.
  - Tối ưu hóa cơ chế `AsNoTracking()` trên các câu truy vấn đọc dữ liệu quy mô lớn.
  - Kiểm tra và đảm bảo toàn bộ Solution biên dịch đạt **0 Warning, 0 Error**.

---

## 📌 PHẦN 2: DEV 2 (Track 2 - Teammate)
> **Phạm vi trọng tâm:** Chuẩn hóa Chú thích XML cho Phân hệ Hợp đồng - Nhà thầu, Xác thực Hoàn thiện Luồng Nghiệp vụ Nghiệm thu - Giải ngân và Đóng gói Bộ Sưu Tập Kiểm thử Bàn giao.

### 2.1. Chuẩn hóa Chú thích XML Documentation cho Phân hệ Hợp đồng & Nhà thầu
- **Bổ sung chú thích XML (`<summary>`, `<param>`, `<response code="...">`) cho các Controllers thuộc Track 2**:
  - `ContractsController.cs`: Mô tả chi tiết các API tạo hợp đồng, tra cứu lịch sử hợp đồng theo nhà thầu, báo cáo tiến độ mốc và phê duyệt nghiệm thu mốc thanh toán.
  - `ContractorsController.cs`: Mô tả chi tiết API tra cứu hồ sơ năng lực nhà thầu, xác thực mã số thuế.
  - `BidSubmissionsController.cs`: Mô tả API nộp hồ sơ dự thầu, xem chi tiết hồ sơ và API tải tệp đề xuất kỹ thuật chống IDOR.
- **Mô tả chi tiết các trường dữ liệu trong các DTOs**:
  - `CreateContractRequest`, `CreateMilestoneRequest`, `ApproveMilestoneAcceptanceRequest`, `ProgressUpdateDto`.
  - Khai báo rõ ràng schema phản hồi lỗi và thông điệp hướng dẫn người dùng.

### 2.2. Xác thực và Kiểm thử Hoàn thiện Luồng Nghiệp vụ Nghiệm thu - Giải ngân
- **Kiểm thử thực tế chuỗi API nghiệp vụ mốc thanh toán**:
  - Báo cáo tiến độ thực hiện mốc thanh toán (`POST /api/contracts/milestones/{id}/progress`).
  - Phê duyệt nghiệm thu mốc thanh toán (`PUT /api/contracts/milestones/{id}/accept`).
  - Kiểm tra số tiền giải ngân thực tế (`TotalDisbursedAmount`) được cập nhật chuẩn xác khi mốc chuyển sang trạng thái `Completed`.
  - Xác nhận ràng buộc: Tổng giá trị các mốc không vượt quá giá trị Hợp đồng (`Contract.Value`).

### 2.3. Đóng gói Tài liệu Hướng dẫn API & Xuất bản Postman Collection v2
- Xuất bản tệp `Procurement_API_v2_Postman_Collection.json` đầy đủ các Endpoints của hệ thống kèm kịch bản mẫu và biến môi trường (`baseUrl`, `adminToken`, `contractorToken`).
- Soạn thảo tài liệu tóm tắt hướng dẫn cài đặt và tích hợp API phục vụ giai đoạn bàn giao sản phẩm.

---

## 🤝 QUY TRÌNH PHỐI HỢP & QUẢN LÝ MÃ NGUỒN (GIT WORKFLOW)

1. **Nhánh làm việc:**
   - Dev 1: `feature/swagger-docs-seeder` (tách từ `dev`).
   - Dev 2: `feature/contracts-xml-docs` (tách từ `dev`).
2. **Quy tắc Commit:**
   - Sử dụng Conventional Commits: `feat(...)`, `docs(...)`, `test(...)`, `refactor(...)`.
3. **Quy trình Hợp nhất (Merge):**
   - Trước khi tạo Pull Request, thực hiện `git pull origin dev` và chạy `dotnet build` bảo đảm **0 Warning, 0 Error**.
4. **Mốc thời gian dự kiến (Deadline):**
   - **17:00:** Hoàn thành các tính năng và chú thích tài liệu riêng.
   - **17:30:** Hợp nhất mã nguồn vào nhánh `dev` chung và đối soát toàn diện trên giao diện Swagger UI.
