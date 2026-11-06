# Hướng dẫn Vận hành Bộ Nghiệm thu Toàn Vòng Đời Có thể Chạy lại (E2E Lifecycle Runbook)

Ngày cập nhật: 05/11/2026  
Dự án: **Procurement & Contract Tracking System**  
Đường dẫn bộ kiểm thử: `scripts/testing/lifecycle-e2e/`  
Điểm vào thực thi tự động: `scripts/testing/run-lifecycle-e2e.ps1`

---

## 1. Tổng quan Kiến trúc Nghiệm thu

Bộ kiểm thử nghiệm thu toàn vòng đời (Reproducible Full-Lifecycle E2E Suite) được thiết kế để xác minh tính toàn vẹn của toàn bộ chuỗi dữ liệu nghiệp vụ trên một môi trường hoàn toàn cô lập cho mỗi lần chạy (`runId`):

`Entity → BidPackage → BidSubmissions → Kết quả trao thầu → Contract → Milestones → Technical Acceptances (Maker) → Phê duyệt giải ngân (Checker) → Contract Completed`.

### Đặc điểm cốt lõi:
1. **Cô lập Cơ sở dữ liệu theo RunId**: Mỗi lần chạy tự động khởi tạo một cơ sở dữ liệu SQL Server mới mang tên `procurement_db_e2e_<runId>`, tự động chạy EF Core Migration và nạp dữ liệu danh mục nền tảng. Tuyệt đối không sử dụng hoặc làm ảnh hưởng tới cơ sở dữ liệu gốc.
2. **Cô lập Lưu trữ Tệp đính kèm**: Mỗi lần chạy tạo thư mục lưu trữ riêng tại `scripts/testing/lifecycle-e2e/runs/<runId>/uploads`.
3. **Kiểm tra Môi trường Nghiêm ngặt (Pre-flight Check)**: Nếu cổng dịch vụ (`5225` hoặc `5173`) đang bị chiếm dụng bởi tiến trình bên ngoài, runner lập tức từ chối kết nối và dừng với mã lỗi `BLOCKED` (exit code `2`) để tránh ghi nhầm vào môi trường đang chạy.
4. **Chuỗi Dữ liệu Xuyên suốt Không Chắp vá**: Toàn bộ 11 bước trên trình duyệt và 6 ca kiểm thử âm phía máy chủ đều thao tác trên cùng một tập định danh (`packageId`, `submissionId`, `contractId`, `milestoneId`, `acceptanceId`) được sinh mới trong chính lần chạy đó.

---

## 2. Yêu cầu Môi trường (Prerequisites)

Trước khi thực thi bộ nghiệm thu, máy trạm cần đáp ứng các yêu cầu phần mềm sau:

| Thành phần | Phiên bản yêu cầu | Mục đích sử dụng |
|---|---|---|
| **.NET SDK** | `.NET 8.0 SDK` trở lên | Biên dịch và khởi chạy tiến trình Backend API cô lập |
| **SQL Server** | SQL Server 2019 / 2022 / 2025 hoặc LocalDB | Khởi tạo cơ sở dữ liệu kiểm thử cô lập theo từng `runId` |
| **Công cụ SQL CLI** | `sqlcmd` | Quản trị tạo/xóa DB kiểm thử và truy vấn đối chiếu dữ liệu |
| **Node.js & npm** | `Node.js 18+` (khuyến nghị `20+`) | Khởi chạy máy chủ giao diện Frontend Vite |
| **Python** | `Python 3.10+` | Thực thi kịch bản điều phối, thiết lập actor và kiểm thử |
| **Playwright** | `playwright==1.60.0` (Chromium) | Tự động hóa thao tác người dùng thực tế trên trình duyệt |

---

## 3. Cài đặt Phụ thuộc (Dependencies Installation)

Thực hiện các lệnh dưới đây tại thư mục gốc của dự án (`D:\Thuc_Tap_backend\Procurement & Contract Tracking System`):

### Bước 3.1: Cài đặt thư viện Python riêng cho bộ runner
```powershell
python -m pip install -r scripts/testing/lifecycle-e2e/requirements.txt
```

### Bước 3.2: Cài đặt nhân trình duyệt Chromium cho Playwright
```powershell
python -m playwright install chromium
```

### Bước 3.3: Đảm bảo gói phụ thuộc Frontend đã sẵn sàng
```powershell
cd frontend
npm install
cd ..
```

---

## 4. Cấu hình Mẫu (Sample Configuration)

Bộ runner tự động truyền biến môi trường cô lập cho tiến trình Backend API mà không cần chỉnh sửa tệp `appsettings.json` của dự án. Cấu hình mẫu được runner thiết lập tự động như sau:

```powershell
# Biến môi trường được runner gán tự động trong phiên chạy cô lập:
$env:ASPNETCORE_ENVIRONMENT = "Development"
$env:ASPNETCORE_URLS = "http://localhost:5225"
$env:DB_CONNECTION_STRING = "Server=localhost;Database=procurement_db_e2e_<runId>;User Id=<db_user>;Password=<db_password>;TrustServerCertificate=True;MultipleActiveResultSets=true"
$env:StorageSettings__LocalBasePath = "scripts/testing/lifecycle-e2e/runs/<runId>/uploads"
```

*Lưu ý bảo mật*: Không lưu mật khẩu thật, khóa bí mật hoặc chuỗi kết nối chứa thông tin nhạy cảm vào tệp cấu hình theo dõi bởi Git. `manifest.json` xuất ra đã được làm sạch toàn bộ thông tin xác thực và token.

---

## 5. Cách Chạy Bộ Nghiệm thu (Single-Command Execution)

### 5.1. Lệnh chạy tự động một bước (Khuyến nghị)
Mở PowerShell tại thư mục gốc dự án và chạy lệnh:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\testing\run-lifecycle-e2e.ps1
```

Khi không truyền tham số `-RunId`, hệ thống tự động sinh mã định danh duy nhất theo thời gian thực (ví dụ: `run_20261105_174500`).

### 5.2. Chạy với mã định danh RunId chỉ định
```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\testing\run-lifecycle-e2e.ps1 -RunId "run1_20261105"
```

### 5.3. Chạy và tự động dọn dẹp Database sau khi PASS
```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\testing\run-lifecycle-e2e.ps1 -RunId "run_clean_test" -DropDatabaseOnSuccess
```

---

## 6. Nơi Xem Kết quả và Bằng chứng Nghiệm thu

Sau mỗi lần chạy, toàn bộ bằng chứng thực thi được đóng gói tại thư mục riêng:

`scripts/testing/lifecycle-e2e/runs/<runId>/`

Cấu trúc thư mục đầu ra bao gồm:
- `manifest.json`: Chứa thông tin thời gian UTC, nhánh và mã commit Git, danh sách Actor (`admin`, `managerA`, `officerA`, `checkerA`, `procurementB`, `contractor1`, `contractor2`, `evaluators`), chuỗi khóa ngoại (`packageId`, `winningSubmissionId`, `contractId`, `milestoneId`, `acceptanceId`), kết quả từng bước trình duyệt, kết quả 6 nhóm ca kiểm thử âm và bảng đối chiếu tài chính cuối kỳ.
- `screenshots/`: Bộ 11 ảnh chụp màn hình theo đúng trình tự nghiệp vụ từ lúc tạo gói thầu mở đến khi hợp đồng chuyển sang trạng thái hoàn tất (`Completed`) và kiểm tra nhật ký kiểm toán (`AuditLogs`).
- `logs/`: Nhật ký đầu ra (`api_stdout.log`, `api_stderr.log`, `fe_stdout.log`, `fe_stderr.log`) phục vụ chẩn đoán chuyên sâu.
- `uploads/`: Các tệp hồ sơ mời thầu, hồ sơ dự thầu, bản scan hợp đồng và bản chụp nghiệm thu (`Snapshot`) phát sinh trong lần chạy.

---

## 7. Cách Chạy lại (Re-running) và Dọn dẹp (Cleanup)

### 7.1. Chạy lại liên tiếp (Repeatability Verification)
Để chứng minh tính tái lập hoàn toàn mà không cần can thiệp thủ công, thực thi liên tiếp hai lần với hai `runId` khác nhau:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\testing\run-lifecycle-e2e.ps1 -RunId "run1_20261105"
powershell -ExecutionPolicy Bypass -File .\scripts\testing\run-lifecycle-e2e.ps1 -RunId "run2_20261105"
```

Mỗi lần chạy sẽ tạo một cơ sở dữ liệu độc lập và một bộ hồ sơ hoàn toàn mới.

### 7.2. Dọn dẹp thủ công một RunId cụ thể
Để xóa cơ sở dữ liệu kiểm thử của một lần chạy cụ thể sau khi đã kiểm tra xong bằng chứng:

```powershell
python -c "import sys; sys.path.insert(0, 'scripts/testing/lifecycle-e2e'); from db_helper import drop_database; drop_database('procurement_db_e2e_run1_20261105')"
```

*Quy tắc an toàn*: Tuyệt đối không chạy lệnh xóa cơ sở dữ liệu hàng loạt theo ký tự đại diện (wildcard) để bảo vệ cơ sở dữ liệu phát triển chung.
