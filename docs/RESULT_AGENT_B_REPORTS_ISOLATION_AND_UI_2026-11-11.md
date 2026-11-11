# Báo cáo Kết quả Triển khai Agent B: Cô lập Dữ liệu Báo cáo Đa đơn vị, Dashboard Procurement & Màn hình Thẩm định Ủy quyền Ký

- **Ngày thực hiện**: 11/11/2026
- **Vai trò**: Agent B (Phân hệ Báo cáo Quản trị, Cách ly Dữ liệu Đa đơn vị & Giao diện Phê duyệt)
- **Nhánh làm việc**: `feat/agent-b-reports-isolation-and-ui` (tách từ `origin/dev` tại commit `18d498e`)
- **Tham chiếu đặc tả**: `HANDOFF_2_AGENTS_SECURITY_ISOLATION_AND_UI_2026-11-11.md` (Mục 2 & Mục 4)

---

## 1. Tóm tắt Phạm vi & Tệp Sở hữu (Zero File Overlap)

Toàn bộ thay đổi chỉ tác động đúng 9 tệp mã nguồn/kiểm thử thuộc phân công của **Agent B**, tuyệt đối không chỉnh sửa tệp của Agent A (`Clarifications*`, `scripts/database-backup-cron.ps1`) hay các phân hệ khác:

| STT | Tệp thực hiện | Loại tác động | Nội dung chính |
|---:|---|---|---|
| 1 | `ProcurementSystem.Core/Interfaces/IReportService.cs` | Chỉnh sửa | Bổ sung `int? requestingUserId = null, bool isAdmin = false` cho 5 phương thức báo cáo/Dashboard/xuất hợp đồng. |
| 2 | `ProcurementSystem.API/Controllers/ReportsController.cs` | Chỉnh sửa | Trích xuất `ClaimTypes.NameIdentifier` và `User.IsInRole("Admin")` từ JWT Claims; trả `401` nếu thiếu định danh; truyền xuống `IReportService`. |
| 3 | `ProcurementSystem.Infrastructure/Services/ReportService.cs` | Chỉnh sửa | Cài đặt `ResolveReportScopeAsync` & `BuildScopedContractsQuery`: Admin xem toàn hệ thống; Procurement chỉ xem dữ liệu thuộc `ProcuringEntityId` mà user đang là thành viên `MemberStatus.Active`; user không có đơn vị hợp lệ trả dữ liệu rỗng an toàn. |
| 4 | `frontend/src/api/reportApi.js` | Chỉnh sửa | Rà soát & ghi chú cơ chế cô lập theo JWT claims phía Backend, không truyền tham số giả mạo từ client. |
| 5 | `frontend/src/App.jsx` | Chỉnh sửa | Mở route `/dashboard` cho `[ROLES.ADMIN, ROLES.PROCUREMENT]`; bổ sung route `/admin/signing-authorities` bảo vệ bởi `[ROLES.ADMIN]`; giữ nguyên `/backup-restore`. |
| 6 | `frontend/src/components/common/Sidebar.jsx` | Chỉnh sửa | Mở mục menu **Tổng quan** (`/dashboard`) cho `Procurement`; bổ sung mục **Thẩm định Ủy quyền Ký** (`/admin/signing-authorities`) cho `Admin`. |
| 7 | `frontend/src/pages/DashboardPage.jsx` | Chỉnh sửa | Tải hồ sơ đơn vị hiện tại (`procuringEntityApi.getMyProfile()`) khi tài khoản là `Procurement`; hiển thị huy hiệu tên Bên mời thầu, MST và biểu ngữ khẳng định dữ liệu cô lập riêng theo đơn vị. |
| 8 | `frontend/src/pages/PendingSigningAuthoritiesPage.jsx` | Tạo mới | Màn hình tập trung cho `Admin` duyệt hồ sơ ủy quyền ký đang chờ (`PendingVerification`) qua `getPendingSigningAuthorities` và `verifySigningAuthority`. |
| 9 | `tests/ProcurementSystem.Tests/ReportDataIsolationTests.cs` | Tạo mới | Bộ 5 kiểm thử tự động xUnit + SQLite In-Memory + ClosedXML + Moq kiểm chứng cô lập đa đơn vị và truyền claims Controller. |

---

## 2. Chi tiết Thiết kế Kỹ thuật & Lưu ý Đối chiếu Schema

### 2.1. Cô lập Dữ liệu Báo cáo Quản trị (`ReportService.cs`)
- **Nguồn xác thực tư cách đơn vị**: Truy vấn bảng `ProcuringEntityMembers` với điều kiện `m.UserId == requestingUserId && m.Status == MemberStatus.Active` (vì thực thể `ProcuringEntityMember` sử dụng enum `MemberStatus Status`, không sử dụng cột boolean `IsActive`). Thành viên đã bị thu hồi (`Revoked`) hoặc tạm khóa (`Suspended`) tự động mất quyền truy xuất ngay lập tức.
- **Chính sách Fail-Closed**: Nếu `!isAdmin` và người dùng chưa thuộc bất kỳ `ProcuringEntity` đang hoạt động nào (hoặc `requestingUserId == null`), toàn bộ 5 phương thức trả về số liệu `0` và danh sách rỗng an toàn:
  1. `GetDashboardMetricsAsync`: Lọc `BidPackage`, `BidSubmission`, `Contract` (`RecentContracts`), `ContractMilestone` và số `TotalContractors` (nhà thầu đã nộp hồ sơ hoặc có hợp đồng với đơn vị).
  2. `ExportContractsCsvAsync` & `ExportContractsExcelAsync`: Chỉ xuất hợp đồng thuộc gói thầu của đơn vị được phép; khi không thuộc đơn vị nào chỉ xuất dòng tiêu đề cột.
  3. `GetProcurementSummaryReportAsync`: Lọc gói thầu và hợp đồng liên kết theo `ProcuringEntityId`.
  4. `GetContractProgressReportAsync`: Lọc cả danh sách mốc nghiệm thu (`ContractMilestones`) lẫn hợp đồng đang hoạt động sắp hết hạn/quá hạn.

### 2.2. Màn hình Thẩm định Ủy quyền Ký (`PendingSigningAuthoritiesPage.jsx`)
- Tích hợp `GET /api/signing-authorities/pending` và `PUT /api/signing-authorities/{id}/verify` sẵn có tại [procuringEntityApi.js](file:///d:/Thuc%20Tap/DuAnTT/frontend/src/api/procuringEntityApi.js).
- **Đối chiếu mô hình dữ liệu thực tế**: Thực thể `SigningAuthority` và `SigningAuthorityDto` hiện lưu trữ `AuthorityTypeName` (Loại thẩm quyền), `Scope` (`EntityWide` / `Contract:{id}`), `DecisionNumber`, `LegalBasis`, `ValidFrom`, `ValidTo`, `DocumentUrl`, `RequestedByName`, `RequestedAt` (hiện chưa có trường *Chức danh* hay *Hạn mức ký tối đa VNĐ*). Do hai tệp này nằm ngoài Ma trận sở hữu tệp của Agent B, giao diện hiển thị đầy đủ tất cả trường thực có từ `SigningAuthorityDto` kèm kiểm tra an toàn URL (`http://`, `https://`, `/`) cho liên kết `documentUrl` và hộp thoại xác nhận **Thẩm định Chấp thuận** / **Từ chối / Yêu cầu bổ sung** (bắt buộc nhập ghi chú khi từ chối).

---

## 3. Bằng chứng Kiểm thử Độc lập

### 3.1. Kiểm thử Backend (.NET 8 xUnit)
1. **Chạy riêng bộ kiểm thử mới `ReportDataIsolationTests`**:
   ```text
   Test run for d:\Thuc Tap\DuAnTT\tests\ProcurementSystem.Tests\bin\Debug\net8.0\ProcurementSystem.Tests.dll (.NETCoreApp,Version=v8.0)
   VSTest version 17.14.1 (x64)
   Passed! - Total tests: 5, Passed: 5, Duration: 4,3902 Seconds
   ```
   - `Admin_GetDashboardAndReports_ReceivesAllEntitiesConsolidatedData`: **PASS** (hợp nhất đủ 3 gói thầu, 2 hợp đồng từ Đơn vị A & B).
   - `ProcurementUserA_GetDashboardAndReports_OnlyReceivesEntityAData`: **PASS** (chỉ trả 2 gói thầu, 1 hợp đồng `HD-UNIT-A-001`, 0 rò rỉ `HD-UNIT-B-999`).
   - `ProcurementUserA_ExportContractsExcelAndCsv_ExcludesEntityBContracts`: **PASS** (đọc lại `.xlsx` bằng `ClosedXML` xác nhận đúng 1 dòng dữ liệu Đơn vị A, không chứa dữ liệu Đơn vị B trong cả Excel và CSV).
   - `UserWithoutActiveEntity_GetsEmptyReportDataAndZeroFinancials`: **PASS** (kiểm chứng cả tài khoản tự do lẫn thành viên `MemberStatus.Revoked` đều nhận giá trị 0 và file xuất chỉ có dòng tiêu đề).
   - `ReportsController_ForwardsUserIdAndAdminFlagFromClaims_AndRejectsMissingClaim`: **PASS** (xác nhận trích xuất đúng `NameIdentifier`, cờ `Admin` và trả HTTP 401 khi thiếu claim).

2. **Chạy toàn bộ giải pháp `ProcurementSystem.slnx`**:
   ```text
   dotnet test ProcurementSystem.slnx --no-restore --disable-build-servers -p:UseSharedCompilation=false --verbosity minimal
   Passed! - Failed: 0, Passed: 99, Skipped: 0, Total: 99, Duration: 42 s - ProcurementSystem.Tests.dll (net8.0)
   ```

### 3.2. Kiểm thử & Biên dịch Frontend (Node / Vite)
- `npm test --prefix frontend -- --run`: **6/6 tests pass** (`fail 0, skipped 0`).
- `npm run build --prefix frontend`: `generate-enums.mjs --check` hợp lệ, Vite biên dịch thành công **2078 modules** trong **6.32s** (`exit code 0`).
