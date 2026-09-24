# Hệ Thống Quản Lý Đấu Thầu & Theo Dõi Hợp Đồng
### Procurement & Contract Tracking System

[![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![C#](https://img.shields.io/badge/C%23-12.0-239120?logo=c-sharp&logoColor=white)](https://learn.microsoft.com/en-us/dotnet/csharp/)
[![EF Core 8](https://img.shields.io/badge/EF%20Core-8.0-512BD4)](https://learn.microsoft.com/en-us/ef/core/)
[![SQL Server](https://img.shields.io/badge/Database-SQL%20Server%202019%2B-CC292B?logo=microsoft-sql-server&logoColor=white)](https://www.microsoft.com/sql-server)
[![Swagger](https://img.shields.io/badge/OpenAPI-Swagger%20UI-85EA2D?logo=swagger&logoColor=black)](https://swagger.io/)
[![Architecture](https://img.shields.io/badge/Architecture-Clean%203--Layer-blue)](#-kiến-trúc-hệ-thống)

---

## 📌 Giới Thiệu Dự Án

**Procurement & Contract Tracking System** là giải pháp phần mềm Backend chuẩn doanh nghiệp được xây dựng trên nền tảng **ASP.NET Core Web API (.NET 8)** và **Microsoft SQL Server**, mô phỏng chính xác quy trình quản lý đấu thầu và vòng đời thực hiện hợp đồng kinh tế theo Luật Đấu thầu thực tế.

Hệ thống số hóa toàn diện quy trình khép kín qua 6 phân hệ cốt lõi:
1. **Xác thực & Phân quyền (Auth & RBAC)**: Cấp phát JWT Bearer Token (HMAC-SHA256) và kiểm soát truy cập nghiêm ngặt dựa trên 4 nhóm vai trò (`Admin`, `Procurement`, `Evaluator`, `Contractor`).
2. **Quản lý Kế hoạch Mời thầu (Bid Packages)**: Tạo lập hồ sơ mời thầu, đăng tải tài liệu HSMT, tự động sinh mã định danh và quản lý cỗ máy trạng thái vòng đời gói thầu (`Open` $\rightarrow$ `Closed` $\rightarrow$ `Evaluating` $\rightarrow$ `Contracted`).
3. **Nộp Hồ sơ & Phòng vệ IDOR (Bid Submissions)**: Tiếp nhận hồ sơ đề xuất kỹ thuật, lưu trữ an toàn tệp tin và thiết lập cơ chế kiểm soát quyền sở hữu chống lỗ hổng IDOR khi tải tệp.
4. **Hội đồng Thẩm định & Chấm điểm (Evaluation & Ranking)**: Thiết lập tiêu chí chấm thầu có trọng số ($\sum Weight = 100\%$), chấm điểm độc lập đa giám khảo và tự động xuất bảng xếp hạng nhà thầu trúng thầu.
5. **Quản lý Hợp đồng & Nghiệm thu (Contracts & Milestones)**: Thiết lập hợp đồng kinh tế, quản lý tiến độ các mốc thanh toán, nghiệm thu bàn giao và cập nhật tiến độ giải ngân ngân sách thực tế.
6. **Báo cáo Điều hành Thời gian Thực (Executive Dashboard)**: Tổng hợp các chỉ số KPI tài chính, tỷ lệ tiết kiệm ngân sách qua đấu thầu và tiến độ giải ngân hợp đồng.

---

## 🏗️ Kiến Trúc Hệ Thống (Clean Architecture)

Hệ thống được thiết kế tuân thủ nghiêm ngặt mô hình **Clean 3-Layer Architecture**, bảo đảm tính độc lập cao, dễ bảo trì, mở rộng và kiểm thử:

```
ProcurementSystem.sln
│
├── 🌐 ProcurementSystem.API/              # Tầng Trình bày (Presentation Layer)
│   ├── Controllers/                        # 11 RESTful Controllers (Hỗ trợ Swagger XML Docs)
│   │   ├── AuthController.Login.cs         # Đăng nhập & luân chuyển Refresh Token
│   │   ├── AuthController.Register.cs      # Đăng ký thông tin nhà thầu
│   │   ├── BidPackagesController.cs        # Quản trị vòng đời gói thầu
│   │   ├── BidSubmissionsController.cs     # Nộp hồ sơ thầu & tải file an toàn
│   │   ├── EvaluationController.cs         # Chấm điểm trọng số & xếp hạng trúng thầu
│   │   ├── ContractsController.cs          # Hợp đồng kinh tế & nghiệm thu mốc thanh toán
│   │   ├── ContractorsController.cs        # Hồ sơ năng lực & uy tín nhà thầu
│   │   ├── ReportsController.cs            # API Dashboard quản trị thời gian thực
│   │   ├── UsersController.cs              # Quản trị tài khoản người dùng
│   │   ├── RolesController.cs              # Danh mục vai trò trong hệ thống
│   │   └── HealthController.cs             # Kiểm tra trạng thái máy chủ & CSDL
│   ├── Middlewares/                        # Pipeline Middleware tập trung
│   │   └── GlobalExceptionMiddleware.cs    # Bắt lỗi toàn cục, che giấu Stack Trace
│   ├── Properties/launchSettings.json      # Cấu hình cổng chạy (Port 5225)
│   └── Program.cs                          # Đăng ký DI, cấu hình JWT, Swagger XML, CORS
│
├── ⚙️ ProcurementSystem.Core/             # Tầng Nghiệp vụ Cốt lõi (Domain / Core Layer)
│   ├── DTOs/                               # Data Transfer Objects (Ngăn ngừa Over-posting)
│   ├── Enums/                              # Enum trạng thái (Package, Submission, Contract)
│   ├── Interfaces/                         # Hợp đồng dịch vụ (Service Interfaces, Repository)
│   ├── Helpers/                            # Quy chuẩn phân trang PaginatedList, Constants
│   └── Mappings/                           # AutoMapper Profiles
│
└── 🗄️ ProcurementSystem.Infrastructure/   # Tầng Truy cập Dữ liệu & Hạ tầng (Data Access Layer)
    ├── Data/                               # AppDbContext (Cấu hình Fluent API, 16 Entities chuẩn 3NF)
    ├── Entities/                           # Mô hình cơ sở dữ liệu quan hệ
    ├── Repositories/                       # GenericRepository & UnitOfWork pattern
    ├── Seeders/                            # Extended DataSeeder (4 vai trò, 5 user, 3 gói thầu)
    ├── Services/                           # Hiện thực hóa toàn bộ Business Logic Services
    └── Migrations/                         # Quản lý phiên bản cấu trúc CSDL EF Core
```

---

## 💻 Tech Stack & Công Nghệ Trọng Tâm

| Phân Loại | Công Nghệ / Thư Viện | Phiên Bản | Mục Đích Sử Dụng |
|---|---|---|---|
| **Runtime & Language** | .NET SDK / C# | .NET 8 / C# 12 | Nền tảng thực thi ứng dụng backend hiệu năng cao |
| **Web Framework** | ASP.NET Core Web API | 8.0 | Xây dựng hệ thống RESTful API chuẩn mực |
| **ORM** | Entity Framework Core | 8.0 | Ánh xạ đối tượng CSDL, Code-First Migration |
| **Database** | Microsoft SQL Server | 2019 / 2022 | Lưu trữ dữ liệu quan hệ chuẩn 3NF, Non-Clustered Indexes |
| **Security & Auth** | JWT Bearer Token | System.IdentityModel | Xác thực người dùng không trạng thái (Stateless Auth) |
| **Password Hashing** | BCrypt.Net-Next | 4.0.3 | Băm mật khẩu một chiều chống tấn công từ điển |
| **API Documentation** | Swashbuckle Swagger UI | 6.5.0 | Giao diện kiểm thử trực quan tích hợp XML Documentation |
| **Object Mapping** | AutoMapper | 12.0.1 | Chuyển đổi dữ liệu tự động giữa Entity và DTO |
| **Validation** | Data Annotations & ModelState | Tích hợp sẵn | Xác thực tính hợp lệ của dữ liệu đầu vào |

---

## 🚀 Hướng Dẫn Cài Đặt & Khởi Chạy

### 1. Yêu cầu môi trường
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) hoặc mới hơn.
- [Microsoft SQL Server](https://www.microsoft.com/sql-server) (bản Developer, Express hoặc Standard).
- Công cụ quản lý CSDL (SQL Server Management Studio hoặc Azure Data Studio).
- Visual Studio 2022 / VS Code hoặc Command Line (PowerShell/CMD).

### 2. Cấu hình Chuỗi Kết Nối CSDL (Connection String)
Mở tệp `ProcurementSystem.API/appsettings.json` và cập nhật thông tin máy chủ SQL Server của bạn:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost;Database=procurement_db;User Id=sa;Password=YOUR_STRONG_PASSWORD;TrustServerCertificate=True;MultipleActiveResultSets=True;"
}
```

### 3. Cập nhật Cơ Sở Dữ Liệu (Database Migration)
Mở PowerShell tại thư mục dự án và thực thi lệnh cập nhật cấu trúc bảng:

```powershell
dotnet ef database update --project ProcurementSystem.Infrastructure --startup-project ProcurementSystem.API
```
*(Hệ thống sẽ tự động tạo CSDL `procurement_db`, 16 bảng quan hệ, khóa ngoại và các chỉ mục Composite Indexes tối ưu hóa hiệu năng).*

### 4. Khởi chạy Ứng dụng & Nạp Dữ Liệu Mẫu Tự Động
Chạy lệnh khởi động máy chủ Web API:

```powershell
dotnet run --project ProcurementSystem.API
```

> **Ghi chú**: Ngay khi máy chủ khởi động lần đầu, bộ **Extended Data Seeder** sẽ tự động kích hoạt, kiểm tra an toàn và nạp sẵn 4 nhóm vai trò, 5 tài khoản mẫu và 3 gói thầu mô phỏng đầy đủ vòng đời nghiệp vụ.

### 5. Truy cập Swagger UI
Mở trình duyệt web và truy cập địa chỉ tương tác API:
👉 **[http://localhost:5225/swagger](http://localhost:5225/swagger)**

---

## 👥 Danh Bạ Tài Khoản Thử Nghiệm Mẫu (Pre-Seeded Accounts)

Hệ thống được cấu hình sẵn 5 tài khoản mẫu phục vụ công tác kiểm thử và bảo vệ đồ án:

| Vai Trò | Email Đăng Nhập | Mật Khẩu Mặc Định | Mô Tả Quyền Hạn |
|---|---|---|---|
| **Admin** | `admin@procurement.com` | `Admin@123` | Toàn quyền quản trị hệ thống, quản lý người dùng, xem Dashboard tổng hợp |
| **Procurement** | `procurement@procurement.com` | `Admin@123` | Chuyên viên bên mời thầu: Tạo gói thầu, cấu hình tiêu chí, đóng thầu |
| **Evaluator** | `evaluator@procurement.com` | `Admin@123` | Thành viên hội đồng chấm thầu: Chấm điểm kỹ thuật, xem bảng xếp hạng |
| **Contractor 1** | `contractor1@test.com` | `Admin@123` | Nhà thầu Cloud: Nộp hồ sơ thầu, ký hợp đồng `CTR-2026-CLOUD-01`, báo cáo tiến độ |
| **Contractor 2** | `contractor2@test.com` | `Admin@123` | Nhà thầu Viễn thông: Nộp hồ sơ dự thầu cạnh tranh, cập nhật hồ sơ năng lực |

---

## 🧪 Bộ Kịch Bản Kiểm Thử Tự Động (Automated Test Suites)

Các kịch bản kiểm thử tự động chuyên sâu được đóng gói sẵn trong thư mục `scripts/testing/`:

```
scripts/testing/
├── test_e2e_full_flow.ps1      # Kiểm thử tích hợp toàn trình 9 bước (End-to-End Workflow)
├── test_security_rbac.ps1      # Kiểm toán an ninh phân quyền RBAC & chống lỗ hổng IDOR (15 ca kiểm thử)
└── test_load_concurrency.py     # Đo lường hiệu năng chịu tải đồng thời (50 luồng đồng thời per API)
```

### 1. Chạy Kiểm Thử Tích Hợp Toàn Trình (E2E Integration Test)
Kịch bản tự động thực thi chuỗi nghiệp vụ từ Mời thầu $\rightarrow$ Nộp thầu $\rightarrow$ Chấm điểm $\rightarrow$ Trao thầu $\rightarrow$ Nghiệm thu:
```powershell
powershell -ExecutionPolicy Bypass -File scripts/testing/test_e2e_full_flow.ps1
```

### 2. Chạy Kiểm Toán An Ninh RBAC & Khắc Chế Lỗ Hổng IDOR
Kiểm tra khả năng phòng thủ trước các hành vi leo thang đặc quyền dọc và leo thang đặc quyền ngang:
```powershell
powershell -ExecutionPolicy Bypass -File scripts/testing/test_security_rbac.ps1
```
*(Kết quả thẩm định: Đạt 15/15 ca kiểm thử, phản hồi đúng các mã chuẩn HTTP 401 Unauthorized và HTTP 403 Forbidden).*

### 3. Chạy Đo Lường Hiệu Năng Tải Đa Luồng (Concurrency Benchmark)
Bắn đồng thời 50 requests vào các endpoints trọng yếu để đo lường độ trễ P50/P95 và thông lượng:
```powershell
python scripts/testing/test_load_concurrency.py
```
*(Kết quả thẩm định: Tỷ lệ thành công 100%, độ trễ trung bình đạt 8.14 ms nhờ hiệu quả của Composite Indexes và AsNoTracking).*

---

## 📋 Kịch Bản Demo Trực Tiếp 9 Bước Trên Swagger UI

1. **Bước 1 (Đăng nhập Quản trị viên)**:
   - Gọi `POST /api/auth/login` với tài khoản `admin@procurement.com` / `Admin@123`.
   - Sao chép chuỗi JWT Token và dán vào nút **Authorize** ở góc trên (`Bearer <token>`).
2. **Bước 2 (Tạo Gói Thầu Mới)**:
   - Gọi `POST /api/bid-packages` tạo gói thầu mới với ngân sách dự toán và thời hạn nộp thầu. Hệ thống tự động sinh mã `PKG-yyyyMMdd-XXXX` ở trạng thái `Open`.
3. **Bước 3 (Thiết lập Tiêu chí Đánh giá)**:
   - Gọi `POST /api/evaluations/packages/{id}/criteria` thiết lập bộ tiêu chí (Kỹ thuật: trọng số 60%, Tài chính: trọng số 40%).
4. **Bước 4 (Nhà thầu Nộp Hồ sơ Dự thầu)**:
   - Đăng nhập tài khoản nhà thầu `contractor1@test.com` lấy token vai trò `Contractor`.
   - Gọi `POST /api/bid-packages/{id}/submissions` đính kèm tệp đề xuất kỹ thuật (`multipart/form-data`).
5. **Bước 5 (Đóng thầu & Chuyển sang Chấm điểm)**:
   - Sử dụng token Admin gọi `PUT /api/bid-packages/{id}/status` chuyển trạng thái sang `Closed` (1), sau đó sang `Evaluating` (2).
6. **Bước 6 (Hội đồng Giám khảo Chấm điểm)**:
   - Đăng nhập tài khoản `evaluator@procurement.com` lấy token vai trò `Evaluator`.
   - Gọi `POST /api/evaluations/submissions/{id}/scores` chấm điểm chi tiết theo từng tiêu chí (thang điểm 0 - 100).
7. **Bước 7 (Xem Bảng Xếp hạng Điểm Trọng số)**:
   - Gọi `GET /api/evaluations/packages/{id}/summary` xem kết quả tính toán tự động điểm tổng hợp có trọng số và thứ hạng của các nhà thầu.
8. **Bước 8 (Phê duyệt Kết quả Trúng thầu)**:
   - Sử dụng token Admin gọi `POST /api/evaluations/packages/{id}/finalize?selectedSubmissionId={subId}` để trao thầu cho nhà thầu xếp hạng 1. Hồ sơ chuyển sang `Selected`, gói thầu chuyển sang `Contracted` và khóa cứng cỗ máy trạng thái.
9. **Bước 9 (Bàn giao Hợp đồng & Giám sát Dashboard)**:
   - Gọi `GET /api/evaluations/packages/{id}/awarded-bid` để xác nhận cờ sẵn sàng lập hợp đồng (`isReadyForContract: true`).
   - Gọi `GET /api/reports/dashboard` để quan sát số liệu KPI tài chính, tỷ lệ tiết kiệm và tiến độ giải ngân được cập nhật theo thời gian thực.

---

## 🎓 Thông Tin Báo Cáo Học Thuật

- **Sinh viên thực hiện:** Phạm Minh Tài
- **Vị trí thực tập:** Backend Software Engineer Intern
- **Đơn vị đào tạo:** Trường Đại học Công Thương TP.HCM (HUIT)
- **Khoa:** Công nghệ Thông tin
- **Dự án:** Hệ thống Quản lý Đấu Thầu & Theo Dõi Hợp Đồng (*Procurement & Contract Tracking System*)
- **Kho lưu trữ GitHub:** [https://github.com/nguyen221205-bit/Procurement-Contract-Tracking-System](https://github.com/nguyen221205-bit/Procurement-Contract-Tracking-System)
