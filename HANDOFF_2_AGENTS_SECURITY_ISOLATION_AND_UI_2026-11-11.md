# Kế hoạch Bàn giao & Phân chia Nhiệm vụ Hai Agent (Agent A & Agent B)

- **Ngày lập**: 11/11/2026
- **Kho lưu trữ (Repo)**: `D:\Thuc_Tap_backend\Procurement & Contract Tracking System`
- **Mốc nền tảng (Baseline)**: Nhánh `dev`, commit `18d498e` (đã đồng bộ với `origin/dev`, sạch sẽ hoàn toàn).
- **Quy tắc cốt lõi**: Phân tách quyền sở hữu tệp tuyệt đối (Zero File Overlap). Không được phép chỉnh sửa tệp thuộc quyền sở hữu của Agent còn lại nhằm triệt tiêu hoàn toàn nguy cơ xung đột mã nguồn (Merge Conflict).

---

## 1. MỤC TIÊU & PHẠM VI TỔNG QUAN

Toàn bộ công việc được chia tách thành hai phân hệ độc lập, rõ ràng về ranh giới kiến trúc và tệp nguồn:

1. **Agent A (Phân hệ Làm rõ Hồ sơ, Bảo mật Tải tệp & Tự động hóa Vận hành)**:
   - Khắc phục triệt để lỗ hổng IDOR và Tải tệp tùy ý trong phân hệ Làm rõ hồ sơ thầu.
   - Xóa bỏ tình trạng khóa cứng thẩm quyền theo người tạo gói (`CreatedBy`), chuyển sang quản trị thẩm quyền theo cấp Bên mời thầu (`ProcuringEntityMember`) và thông báo đúng đối tượng phụ trách.
   - Nâng cấp kịch bản PowerShell tự động hóa sao lưu định kỳ để tự động nhận diện chuỗi kết nối từ cấu hình hệ thống.
   - Xây dựng bộ kiểm thử xUnit riêng biệt chứng minh tính bảo mật và phân quyền đúng đắn.

2. **Agent B (Phân hệ Báo cáo Quản trị, Cách ly Dữ liệu Đa đơn vị & Giao diện Phê duyệt)**:
   - Khắc phục lỗ hổng rò rỉ dữ liệu đa đơn vị trong các API thống kê báo cáo và xuất tệp Excel của phân hệ Mua sắm & Hợp đồng.
   - Bổ sung cơ chế lọc dữ liệu theo `ProcuringEntityId` đối với vai trò Bên mời thầu (`Procurement`), giữ nguyên quyền xem tổng hợp toàn quốc cho Quản trị viên (`Admin`).
   - Đồng bộ giao diện Bảng điều khiển (Dashboard) trên Frontend, cho phép vai trò `Procurement` truy cập dữ liệu phân tích riêng của đơn vị mình.
   - Xây dựng trang giao diện Quản trị viên tập trung phê duyệt hồ sơ đề nghị cấp thẩm quyền ký (`PendingSigningAuthoritiesPage`), kết nối với API sẵn có.
   - Xây dựng bộ kiểm thử xUnit riêng biệt chứng minh tính cách ly dữ liệu giữa các đơn vị.

---

## 2. MA TRẬN PHÂN QUYỀN SỞ HỮU TỆP (RANH GIỚI BẮT BUỘC)

Tuyệt đối tuân thủ ma trận phân chia dưới đây. Mỗi tệp chỉ do duy nhất một Agent sở hữu và chỉnh sửa:

| Đường dẫn tệp / thư mục | Agent sở hữu duy nhất | Ghi chú phạm vi |
| :--- | :--- | :--- |
| **ProcurementSystem.API/Controllers/ClarificationsController.cs** | **Agent A** | Sửa phương thức tải tệp an toàn, kiểm tra quyền xem |
| **ProcurementSystem.Core/Interfaces/IClarificationService.cs** | **Agent A** | Mở rộng chữ ký phương thức nghiệp vụ làm rõ |
| **ProcurementSystem.Infrastructure/Services/ClarificationService.cs** | **Agent A** | Chuẩn hóa thẩm quyền `ProcuringEntityMember` |
| **frontend/src/api/clarificationApi.js** | **Agent A** | Khớp tham số API tải tệp đính kèm |
| **frontend/src/pages/ClarificationPage.jsx** | **Agent A** | Cập nhật giao diện tải tệp nếu cần |
| **scripts/database-backup-cron.ps1** | **Agent A** | Nạp chuỗi kết nối tự động từ appsettings.json |
| **tests/ProcurementSystem.Tests/ClarificationSecurityAndAuthorityTests.cs** | **Agent A** | Tệp kiểm thử xUnit mới độc lập |
| **docs/RESULT_AGENT_A_CLARIFICATION_SECURITY_2026-11-11.md** | **Agent A** | Báo cáo nghiệm thu kết quả của Agent A |
| **ProcurementSystem.API/Controllers/ReportsController.cs** | **Agent B** | Truyền thông tin định danh và phân quyền báo cáo |
| **ProcurementSystem.Core/Interfaces/IReportService.cs** | **Agent B** | Cập nhật tham số nhận diện đơn vị trong báo cáo |
| **ProcurementSystem.Infrastructure/Services/ReportService.cs** | **Agent B** | Lọc dữ liệu theo `ProcuringEntityId` cho Procurement |
| **frontend/src/api/reportApi.js** | **Agent B** | Cập nhật hàm gọi API báo cáo |
| **frontend/src/App.jsx** | **Agent B** | Mở tuyến đường `/dashboard` và thêm trang mới |
| **frontend/src/components/common/Sidebar.jsx** | **Agent B** | Hiển thị menu Dashboard và Thẩm quyền ký |
| **frontend/src/pages/DashboardPage.jsx** | **Agent B** | Hiển thị thông tin đơn vị cho cán bộ Procurement |
| **frontend/src/pages/PendingSigningAuthoritiesPage.jsx** | **Agent B** | Tạo mới màn hình duyệt ủy quyền ký cho Admin |
| **tests/ProcurementSystem.Tests/ReportDataIsolationTests.cs** | **Agent B** | Tệp kiểm thử xUnit mới độc lập |
| **docs/RESULT_AGENT_B_REPORTS_ISOLATION_AND_UI_2026-11-11.md** | **Agent B** | Báo cáo nghiệm thu kết quả của Agent B |

---

## 3. BẢNG ĐẶC TẢ CHI TIẾT DÀNH CHO AGENT A

### 3.1. Vấn đề 1: Lỗ hổng Tải Tệp Tùy ý / IDOR trong Làm rõ Hồ sơ
- **Tệp nguồn**: [ClarificationsController.cs](<file:///D:/Thuc_Tap_backend/Procurement & Contract Tracking System/ProcurementSystem.API/Controllers/ClarificationsController.cs#L179-L210>)
- **Hiện trạng lỗi**:
  - Endpoint `GET api/clarifications/attachments/download?filePath=...` nhận trực tiếp đường dẫn tương đối từ query string mà không xác thực mối liên kết với bảng `BidClarification`.
  - Người dùng có thể truyền `filePath=contracts/1/...` hoặc `filePath=submissions/...` để tải trái phép tài liệu của đơn vị hoặc nhà thầu khác.
- **Yêu cầu triển khai**:
  - Thay đổi chữ ký hoặc logic tiếp nhận: Tiếp nhận `clarificationId` (hoặc kiểm tra `filePath` phải khớp chính xác với trường `AttachmentPath` của một bản ghi `BidClarification` cụ thể trong cơ sở dữ liệu).
  - Kiểm tra quyền truy cập:
    + Nếu làm rõ là loại công khai (`Type == ClarificationType.Package`), mọi người dùng hợp lệ đều được tải.
    + Nếu làm rõ là loại riêng tư (`Type == ClarificationType.Submission`), chỉ cho phép: Nhà thầu sở hữu hồ sơ dự thầu đó, Chuyên gia chấm thầu thuộc Tổ chuyên gia của gói thầu, Cán bộ thuộc Bên mời thầu sở hữu gói thầu, và Quản trị viên hệ thống (`Admin`).
    + Nếu không thỏa mãn, trả về HTTP 403 Forbidden.
  - Đồng bộ hàm gọi tải tệp trong [clarificationApi.js](<file:///D:/Thuc_Tap_backend/Procurement & Contract Tracking System/frontend/src/api/clarificationApi.js>) và [ClarificationPage.jsx](<file:///D:/Thuc_Tap_backend/Procurement & Contract Tracking System/frontend/src/pages/ClarificationPage.jsx>) để giao diện người dùng hoạt động trơn tru.

### 3.2. Vấn đề 2: Khóa Cứng Thẩm quyền Làm rõ theo Người Tạo Gói Thầu (`CreatedBy`)
- **Tệp nguồn**: [ClarificationService.cs](<file:///D:/Thuc_Tap_backend/Procurement & Contract Tracking System/ProcurementSystem.Infrastructure/Services/ClarificationService.cs#L57-L75>), [L169](<file:///D:/Thuc_Tap_backend/Procurement & Contract Tracking System/ProcurementSystem.Infrastructure/Services/ClarificationService.cs#L169>), [L230-L238](<file:///D:/Thuc_Tap_backend/Procurement & Contract Tracking System/ProcurementSystem.Infrastructure/Services/ClarificationService.cs#L230-L238>), [L350-L355](<file:///D:/Thuc_Tap_backend/Procurement & Contract Tracking System/ProcurementSystem.Infrastructure/Services/ClarificationService.cs#L350-L355>)
- **Hiện trạng lỗi**:
  - `GetClarificationsAsync`: Kiểm tra cứng `package.CreatedBy == userId`. Cán bộ quản lý hoặc chuyên viên khác cùng thuộc Bên mời thầu không thể xem các giải trình HSDT riêng tư.
  - `CreateSubmissionClarificationAsync`: Chỉ người tạo gói thầu hoặc chuyên gia chấm thầu mới được yêu cầu nhà thầu làm rõ HSDT. Cán bộ phụ trách mới tiếp nhận gói thầu không có quyền thao tác.
  - `AnswerClarificationAsync`: Bắt buộc `clarification.BidPackage.CreatedBy != currentUserId` thì ném ngoại lệ từ chối. Khi người tạo gói nghỉ việc, không ai trong đơn vị có thể trả lời câu hỏi làm rõ HSMT.
  - `CreatePackageClarificationAsync` (dòng 169): Danh sách nhận thông báo chỉ có `package.CreatedBy` và `Admin`.
- **Yêu cầu triển khai**:
  - Thay thế kiểm tra `package.CreatedBy == userId` bằng hàm kiểm tra tư cách thành viên đang hoạt động (`IsActive == true`) của Bên mời thầu sở hữu gói thầu (`package.ProcuringEntityId`) với vai trò nội bộ hợp lệ (`Manager` hoặc `Officer`).
  - Cho phép bất kỳ cán bộ `Manager` hoặc `Officer` hợp lệ nào của Bên mời thầu sở hữu gói thầu đều có quyền:
    + Xem danh sách giải trình HSDT của các nhà thầu.
    + Yêu cầu nhà thầu làm rõ HSDT.
    + Trả lời câu hỏi làm rõ HSMT của nhà thầu.
  - Danh sách gửi thông báo làm rõ HSMT mới cần gửi đến tất cả cán bộ quản lý và chuyên viên đang hoạt động của Bên mời thầu sở hữu gói thầu đó.

### 3.3. Vấn đề 3: Kịch bản Tự động hóa Sao lưu Tự nhận diện Chuỗi kết nối
- **Tệp nguồn**: [database-backup-cron.ps1](<file:///D:/Thuc_Tap_backend/Procurement & Contract Tracking System/scripts/database-backup-cron.ps1#L20-L40>)
- **Hiện trạng lỗi**: Đang sử dụng tài khoản cố định `sa/123` trong tham số mặc định, gây lỗi nếu chạy trên môi trường thực tế với mật khẩu quản trị khác biệt.
- **Yêu cầu triển khai**:
  - Bổ sung khối lệnh kiểm tra biến môi trường `PROCUREMENT_DB_CONN`.
  - Nếu không có biến môi trường, tự động đọc chuỗi kết nối từ `ProcurementSystem.API/appsettings.Development.json` hoặc `appsettings.json`.
  - Phân tích cú pháp chuỗi kết nối để trích xuất `Server`, `Database`, `User Id` và `Password` khi các tham số dòng lệnh không được truyền tường minh.

### 3.4. Kiểm thử Độc lập của Agent A
- Tạo tệp `tests/ProcurementSystem.Tests/ClarificationSecurityAndAuthorityTests.cs`.
- Tối thiểu 5 kịch bản kiểm thử:
  1. Người dùng không có quyền tải tệp đính kèm giải trình riêng tư bị từ chối với mã 403.
  2. Nhà thầu sở hữu hồ sơ và Cán bộ cùng đơn vị mời thầu tải tệp thành công.
  3. Cán bộ quản lý cùng Bên mời thầu (không phải người tạo ban đầu) có quyền xem và trả lời làm rõ HSMT.
  4. Cán bộ thuộc Bên mời thầu khác bị từ chối truy cập giải trình riêng tư của gói thầu.
  5. Thông báo câu hỏi làm rõ mới được phân phối đúng đến các thành viên hoạt động trong đơn vị.

---

## 4. BẢNG ĐẶC TẢ CHI TIẾT DÀNH CHO AGENT B

### 4.1. Vấn đề 1: Khắc phục Rò rỉ Dữ liệu Đa Đơn vị trong Báo cáo Quản trị
- **Tệp nguồn**: 
  - [ReportsController.cs](<file:///D:/Thuc_Tap_backend/Procurement & Contract Tracking System/ProcurementSystem.API/Controllers/ReportsController.cs#L45-L125>)
  - [IReportService.cs](<file:///D:/Thuc_Tap_backend/Procurement & Contract Tracking System/ProcurementSystem.Core/Interfaces/IReportService.cs>)
  - [ReportService.cs](<file:///D:/Thuc_Tap_backend/Procurement & Contract Tracking System/ProcurementSystem.Infrastructure/Services/ReportService.cs#L20-L100>)
- **Hiện trạng lỗi**:
  - Các endpoint `/api/reports/dashboard`, `/contracts/export-excel`, `/contracts/export`, `/procurement-summary`, `/contracts-progress` mở cho vai trò `Procurement`, nhưng truy vấn toàn bộ dữ liệu hệ thống mà không lọc theo đơn vị của người dùng.
  - Cán bộ thuộc Bên mời thầu này có thể xem toàn bộ hợp đồng, số tiền tiết kiệm, danh tính nhà thầu của Bên mời thầu khác.
- **Yêu cầu triển khai**:
  - Trong `ReportsController.cs`: Lấy `currentUserId` và danh sách vai trò từ `User.Claims`.
  - Mở rộng giao diện `IReportService` và cài đặt trong `ReportService`: Bổ sung tham số `int? requestingUserId = null, bool isAdmin = false` cho các phương thức:
    + `GetDashboardMetricsAsync`
    + `ExportContractsExcelAsync` và `ExportContractsCsvAsync`
    + `GetProcurementSummaryReportAsync`
    + `GetContractProgressReportAsync`
  - Logic nghiệp vụ trong `ReportService`:
    + Nếu `isAdmin == true`: Giữ nguyên quyền truy vấn toàn bộ dữ liệu trên phạm vi toàn quốc.
    + Nếu là người dùng vai trò `Procurement`: Xác định `ProcuringEntityId` mà người dùng đang là thành viên có tư cách hoạt động (`IsActive == true`). Toàn bộ các câu truy vấn gói thầu, hợp đồng, mốc thanh toán, số liệu giải ngân bắt buộc phải lọc theo điều kiện: `p.ProcuringEntityId == userEntityId`.
    + Trường hợp người dùng không thuộc đơn vị mời thầu nào hợp lệ, trả về dữ liệu rỗng an toàn thay vì làm lộ dữ liệu của đơn vị khác.

### 4.2. Vấn đề 2: Đồng bộ Giao diện Bảng Điều khiển (Dashboard) trên Frontend
- **Tệp nguồn**:
  - [App.jsx](<file:///D:/Thuc_Tap_backend/Procurement & Contract Tracking System/frontend/src/App.jsx#L40-L45>)
  - [Sidebar.jsx](<file:///D:/Thuc_Tap_backend/Procurement & Contract Tracking System/frontend/src/components/common/Sidebar.jsx#L20-L25>)
  - [DashboardPage.jsx](<file:///D:/Thuc_Tap_backend/Procurement & Contract Tracking System/frontend/src/pages/DashboardPage.jsx>)
- **Yêu cầu triển khai**:
  - Cập nhật cấu hình bảo vệ tuyến đường trong `App.jsx`: Cho phép cả `Admin` và `Procurement` truy cập tuyến đường `/dashboard`.
  - Cập nhật thanh điều hướng trong `Sidebar.jsx`: Hiển thị mục menu Bảng điều khiển (Dashboard) cho cả người dùng có vai trò `Procurement`.
  - Trong `DashboardPage.jsx`: Hiển thị rõ ràng phạm vi dữ liệu đang xem. Nếu là tài khoản `Procurement`, hiển thị huy hiệu hoặc tiêu đề tên Bên mời thầu của người dùng để khẳng định số liệu hiển thị là số liệu riêng biệt của đơn vị.

### 4.3. Vấn đề 3: Xây dựng Màn hình Duyệt Thẩm định Ủy quyền Ký tập trung cho Admin
- **Tệp nguồn**:
  - API Client: [procuringEntityApi.js](<file:///D:/Thuc_Tap_backend/Procurement & Contract Tracking System/frontend/src/api/procuringEntityApi.js#L30-L35>) (đã có `getPendingSigningAuthorities`, `verifySigningAuthority`, `revokeSigningAuthority`)
  - Trang giao diện mới: `frontend/src/pages/PendingSigningAuthoritiesPage.jsx`
  - Cập nhật tuyến đường: [App.jsx](<file:///D:/Thuc_Tap_backend/Procurement & Contract Tracking System/frontend/src/App.jsx>)
  - Cập nhật thanh điều hướng: [Sidebar.jsx](<file:///D:/Thuc_Tap_backend/Procurement & Contract Tracking System/frontend/src/components/common/Sidebar.jsx>)
- **Yêu cầu triển khai**:
  - Tạo trang `PendingSigningAuthoritiesPage.jsx` dành riêng cho vai trò `Admin`:
    + Hiển thị danh sách các hồ sơ đề nghị cấp thẩm quyền ký đang ở trạng thái chờ thẩm định (`Pending`) từ tất cả các Bên mời thầu trên toàn quốc.
    + Thể hiện đầy đủ các trường thông tin: Tên Bên mời thầu, Họ tên người được ủy quyền, Chức danh, Số quyết định ủy quyền, Hạn mức ký tối đa (VNĐ), Ngày bắt đầu, Ngày hết hạn, và liên kết xem tệp quyết định bổ nhiệm đính kèm.
    + Cung cấp 2 nút hành động trực tiếp: "Thẩm định Chấp thuận" (Approve) và "Từ chối / Yêu cầu bổ sung" (Reject) kèm hộp thoại nhập lý do hoặc ghi chú thẩm định.
  - Đăng ký tuyến đường `/admin/signing-authorities` trong `App.jsx` có bảo vệ vai trò `Admin`.
  - Thêm mục "Thẩm định Ủy quyền Ký" vào thanh điều hướng Quản trị trong `Sidebar.jsx`.

### 4.4. Kiểm thử Độc lập của Agent B
- Tạo tệp `tests/ProcurementSystem.Tests/ReportDataIsolationTests.cs`.
- Tối thiểu 4 kịch bản kiểm thử:
  1. Quản trị viên (`Admin`) truy xuất Dashboard nhận được dữ liệu hợp nhất toàn hệ thống.
  2. Cán bộ Bên mời thầu A truy xuất Dashboard chỉ nhận được các gói thầu và hợp đồng thuộc Đơn vị A.
  3. Xuất tệp Excel danh sách hợp đồng bởi Cán bộ Đơn vị A không chứa bất kỳ dòng dữ liệu hợp đồng nào của Đơn vị B.
  4. Người dùng không thuộc Bên mời thầu nào truy xuất báo cáo nhận được số liệu rỗng (0 hợp đồng, 0 ngân sách).

---

## 5. QUY TRÌNH PHỐI HỢP & NGHIỆM THU

1. **Thực thi Song song**:
   - Hai Agent làm việc hoàn toàn trên các tệp đã được phân công theo bảng ma trận tại Mục 2.
   - Tuyệt đối không chạm vào tệp của nhau.
2. **Kiểm tra Chất lượng Độc lập**:
   - Sau khi hoàn thành phần việc của mình, mỗi Agent phải tự chạy kiểm thử cục bộ:
     + Backend: `dotnet test` (yêu cầu toàn bộ các bài kiểm thử hiện có và kiểm thử mới đều đạt kết quả Pass).
     + Frontend: `npm test` và `npm run build` (đảm bảo không phát sinh lỗi biên dịch và không vi phạm quy chuẩn).
3. **Lập Báo cáo Nghiệm thu**:
   - Agent A xuất báo cáo tại: `docs/RESULT_AGENT_A_CLARIFICATION_SECURITY_2026-11-11.md`.
   - Agent B xuất báo cáo tại: `docs/RESULT_AGENT_B_REPORTS_ISOLATION_AND_UI_2026-11-11.md`.
4. **Hợp nhất Cuối cùng**:
   - Hai nhánh công việc được đưa về commit độc lập và hợp nhất tuần tự vào nhánh `dev`.
