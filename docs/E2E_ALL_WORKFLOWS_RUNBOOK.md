# Runbook Vận Hành Bộ Kiểm Thử Nghiệm Thu Toàn Hệ Thống (All Workflows & All Roles E2E)

Tài liệu này hướng dẫn từng bước cách chuẩn bị môi trường, cấu hình cô lập, khởi chạy bộ kiểm thử nghiệm thu toàn bộ luồng nghiệp vụ và vai trò (`All-Workflows & All-Roles E2E Suite`), kiểm tra bằng chứng đầu ra và dọn dẹp tài nguyên sau khi chạy.

---

## 1. Kiến trúc Cô lập & Nguyên tắc Nghiệm thu

Bộ kiểm thử được thiết kế theo kiến trúc tự khép kín (self-contained) trên từng lần chạy (`RunId`):

1. **Cơ sở dữ liệu SQL Server cô lập**:
   - Tự động khởi tạo database riêng `procurement_db_e2e_<RunId>` trên SQL Server cục bộ.
   - Thực thi đầy đủ chuỗi EF Core Migrations và `DataSeeder` khi Backend API khởi động.
   - Tuyệt đối không chạm vào database chính `procurement_db` hoặc môi trường staging/production.
2. **Máy chủ SMTP Catcher cục bộ (`127.0.0.1:2525`)**:
   - Tự động khởi chạy `LocalSmtpCatcher` ([smtp_catcher.py](file:///D:/Thuc_Tap_backend/Procurement%20&%20Contract%20Tracking%20System/scripts/testing/lifecycle-e2e/smtp_catcher.py)) lắng nghe trên `127.0.0.1:2525`.
   - Backend API được cấu hình gửi toàn bộ email (mã OTP đăng ký, link mời thành viên `ProcuringEntityInvitations`, mật khẩu tạm thời khi import nhà thầu, mật khẩu đặt lại, email thông báo duyệt/từ chối) về `LocalSmtpCatcher`.
   - Bộ test trích xuất mã OTP 6 số, token lời mời và mật khẩu tạm trực tiếp từ nội dung email thực tế trong `runs/<RunId>/smtp_messages.json`, không đọc trộm hay sửa bảng DB.
3. **Thư mục Lưu trữ Tệp Cục bộ Cô lập**:
   - Toàn bộ tệp đính kèm (PDF quyết định thành lập, giấy phép ĐKKD, HSMT, HSDT, hợp đồng scan, bảo lãnh, phụ lục, biên bản snapshot nghiệm thu có băm SHA-256) được lưu tại `runs/<RunId>/uploads/`.
   - Biến môi trường `StorageSettings__Provider=Local` và các khóa Supabase rỗng đảm bảo không gọi ra bucket đám mây bên ngoài.
4. **Không can thiệp trạng thái nghiệp vụ bằng SQL `UPDATE`**:
   - Toàn bộ chuyển đổi trạng thái gói thầu, hồ sơ dự thầu, hợp đồng, mốc nghiệm thu, ủy quyền ký và tư cách thành viên đều đi qua giao diện người dùng (Playwright Chromium) hoặc HTTP API chính thức.

---

## 2. Yêu cầu Tiên quyết (Prerequisites)

Trước khi chạy bộ kiểm thử, máy trạm cần đáp ứng các điều kiện sau:

- **Hệ điều hành**: Windows 10/11 với PowerShell 5.1+ hoặc PowerShell 7+.
- **.NET SDK**: `.NET 8.0 SDK` trở lên (`dotnet --version`).
- **SQL Server**: Dịch vụ SQL Server cục bộ (`localhost`) đang hoạt động và cho phép kết nối bằng tài khoản `sa` (hoặc cấu hình trong `DB_CONNECTION_STRING`).
- **Node.js & Frontend Dependencies**: Node.js 18+ đã cài đặt gói thư viện tại thư mục `frontend/` (`npm install --prefix frontend`).
- **Python & Playwright**:
  - Python 3.10+ (`python --version`).
  - Cài đặt thư viện Python và trình duyệt Chromium cho Playwright:

```powershell
pip install -r "scripts/testing/lifecycle-e2e/requirements.txt"
python -m playwright install chromium
```

- **Bộ tài liệu PDF mẫu**: Thư mục `D:\Thuc_Tap_backend\TaiLieuMau` có sẵn các tệp PDF mẫu hợp lệ (`QuyetDinhThanhLap_DonViMoiThau.pdf`, `GiayPhepKinhDoanh_NhaThau.pdf`, `HoSoMoiThau_HSMT.pdf`, `BaoGia_TaiChinh.pdf`, `HoSoNangLuc.pdf`, `TienDo_BienPhap.pdf`, `HopDong_DaKy_Scan.pdf`).
- **Cổng mạng trống**: Các cổng `5225` (Backend API), `5173` (Frontend Vite) và `2525` (Local SMTP Catcher) phải đang trống trước khi chạy. Nếu cổng đang bận, script sẽ dừng ngay với mã lỗi `exit 2` để bảo vệ tiến trình đang chạy.

---

## 3. Cấu trúc Thư mục Bộ Kiểm thử

- [run-lifecycle-e2e.ps1](file:///D:/Thuc_Tap_backend/Procurement%20&%20Contract%20Tracking%20System/scripts/testing/run-lifecycle-e2e.ps1): Script điều phối tổng (kiểm tra cổng, tạo DB, build & chạy API + Vite, gọi `run_suite.py`, thu dọn tiến trình và xóa DB tạm khi hoàn tất).
- [run_suite.py](file:///D:/Thuc_Tap_backend/Procurement%20&%20Contract%20Tracking%20System/scripts/testing/lifecycle-e2e/run_suite.py): Điểm vào Python khởi động `LocalSmtpCatcher`, chạy `fixture_setup.py`, `test_lifecycle_e2e.py`, đối soát SQL và chạy `test_all_workflows_all_roles.py`.
- [smtp_catcher.py](file:///D:/Thuc_Tap_backend/Procurement%20&%20Contract%20Tracking%20System/scripts/testing/lifecycle-e2e/smtp_catcher.py): Máy chủ ESMTP nội bộ bắt toàn bộ email hệ thống và trích xuất OTP/token/mật khẩu tạm.
- [fixture_setup.py](file:///D:/Thuc_Tap_backend/Procurement%20&%20Contract%20Tracking%20System/scripts/testing/lifecycle-e2e/fixture_setup.py): Khởi tạo dữ liệu nền qua HTTP API + SMTP Catcher (Entity A, Entity B, Manager A/B, Officer A1/A2, Finance A, Signer A1/A2, Evaluators E1/E2/E3, Contractors C1/C2).
- [test_lifecycle_e2e.py](file:///D:/Thuc_Tap_backend/Procurement%20&%20Contract%20Tracking%20System/scripts/testing/lifecycle-e2e/test_lifecycle_e2e.py): Kiểm thử trình duyệt Playwright xuyên suốt 11 bước vòng đời chính và chụp ảnh màn hình bằng chứng.
- [test_negative_rules.py](file:///D:/Thuc_Tap_backend/Procurement%20&%20Contract%20Tracking%20System/scripts/testing/lifecycle-e2e/test_negative_rules.py): Kiểm thử 6 quy tắc chặn âm tính cốt lõi phía máy chủ trong quá trình nghiệm thu.
- [test_all_workflows_all_roles.py](file:///D:/Thuc_Tap_backend/Procurement%20&%20Contract%20Tracking%20System/scripts/testing/lifecycle-e2e/test_all_workflows_all_roles.py): Thực thi toàn bộ 93 ca kiểm thử thuộc 22 phân hệ và 6 chuỗi E2E, xuất `case_results.json` và `case_results.csv`.
- [requirements.txt](file:///D:/Thuc_Tap_backend/Procurement%20&%20Contract%20Tracking%20System/scripts/testing/lifecycle-e2e/requirements.txt) & [.env.e2e.example](file:///D:/Thuc_Tap_backend/Procurement%20&%20Contract%20Tracking%20System/scripts/testing/lifecycle-e2e/.env.e2e.example): Danh sách gói phụ thuộc và mẫu biến môi trường.

---

## 4. Hướng dẫn Thực thi (Execution Commands)

### 4.1. Chạy 1 lượt nghiệm thu đầy đủ và tự động xóa DB tạm sau khi hoàn tất

Từ thư mục gốc dự án (`D:\Thuc_Tap_backend\Procurement & Contract Tracking System`):

```powershell
powershell -ExecutionPolicy Bypass -File "scripts/testing/run-lifecycle-e2e.ps1" -RunId "full_run1_20261106" -DropDatabaseOnSuccess
```

### 4.2. Chạy 2 lượt liên tiếp độc lập để chứng minh tính tái lập (Reproducibility)

```powershell
powershell -ExecutionPolicy Bypass -File "scripts/testing/run-lifecycle-e2e.ps1" -RunId "full_run1_20261106" -DropDatabaseOnSuccess
powershell -ExecutionPolicy Bypass -File "scripts/testing/run-lifecycle-e2e.ps1" -RunId "full_run2_20261106" -DropDatabaseOnSuccess
```

### 4.3. Giữ lại Database tạm để truy vấn/kiểm tra thủ công sau khi chạy

Bỏ cờ `-DropDatabaseOnSuccess` khi muốn giữ lại database `procurement_db_e2e_<RunId>` trên SQL Server:

```powershell
powershell -ExecutionPolicy Bypass -File "scripts/testing/run-lifecycle-e2e.ps1" -RunId "debug_run_01"
```

### 4.4. Chạy kèm bộ kiểm thử đơn vị và tích hợp (.NET & Frontend)

```powershell
dotnet test --nologo
dotnet build --nologo
npm test --prefix frontend -- --run
npm run build --prefix frontend
```

---

## 5. Kiểm tra Kết quả và Bằng chứng Đầu ra

Sau mỗi lượt chạy, toàn bộ bằng chứng được lưu tại `scripts/testing/lifecycle-e2e/runs/<RunId>/`:

1. **`manifest.json`**:
   - Chứa thông tin commit Git, môi trường, danh sách tài khoản/đơn vị đã tạo, kết quả đối soát chuỗi khóa ngoại (`BidPackage -> BidSubmission -> Contract -> ContractMilestones -> Acceptances -> AuditLogs`), số lượng email SMTP bắt được và tóm tắt kết quả `allWorkflowsSummary`.
2. **`case_results.json` & `case_results.csv`**:
   - Bảng kết quả chi tiết của từng ca kiểm thử trong tổng số 93 ca (`caseId`, `chain`, `module`, `role`, `entityScope`, `preconditionState`, `action`, `expectedHttpStatus`, `actualHttpStatus`, `expectedPostState`, `actualPostState`, `evidenceFile`, `status`, `notes`).
   - Các lỗi sản phẩm thực tế phát hiện trên nhánh kiểm thử được ghi nhận trung thực là `FAIL` kèm nguyên nhân gốc trong cột `notes`.
3. **`smtp_messages.json`**:
   - Toàn bộ bức thư điện tử do Backend gửi qua SMTP trong suốt lượt chạy, kèm trường trích xuất `extractedOtp`, `extractedInviteToken`, `extractedTempPassword`.
4. **`screenshots/`**:
   - Bộ 12 ảnh chụp màn hình trình duyệt Chromium tại các cột mốc nghiệp vụ và kiểm tra `RouteGuard` / `F5 reload`.
5. **`exports/`**:
   - Các tệp báo cáo CSV/Excel và mẫu import xuất ra từ hệ thống (`contractor_import_template.xlsx`, `contractors_export.xlsx`, `contractors_export.csv`, `contracts_export.xlsx`, `contracts_export.csv`).
6. **`logs/`**:
   - Nhật ký đầu ra chuẩn và lỗi của tiến trình Backend API (`api_stdout.log`, `api_stderr.log`) và Frontend Vite (`fe_stdout.log`, `fe_stderr.log`).

---

## 6. Xử lý Sự cố Thường gặp (Troubleshooting)

- **Lỗi `[BLOCKED] Port 5225/5173/2525 is already in use`**:
  - Có tiến trình API hoặc Frontend cũ đang chạy. Chạy `stop-all.bat` hoặc đóng tiến trình chiếm cổng trước khi khởi chạy lại.
- **Lỗi kết nối SQL Server (`sqlcmd` failed)**:
  - Kiểm tra dịch vụ SQL Server đã bật chưa bằng `Bat-SQL-Server.bat` hoặc `Get-Service MSSQL*`.
- **Lỗi thiếu trình duyệt Playwright**:
  - Chạy `python -m playwright install chromium` để tải binary Chromium tương thích với phiên bản Playwright hiện tại.
