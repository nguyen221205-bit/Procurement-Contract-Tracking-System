# Procurement & Contract Tracking System
> **Hệ thống Quản lý Đấu Thầu & Theo Dõi Hợp Đồng**  
> Dự án thực tập tốt nghiệp Backend (.NET 8 Web API & SQL Server)

---

## 📌 Giới thiệu Dự án

Dự án quản lý toàn diện quy trình đấu thầu và vòng đời hợp đồng trong doanh nghiệp:
1. **Mời thầu**: Đăng tải gói thầu, tài liệu mời thầu, kiểm soát thời hạn nộp.
2. **Dự thầu**: Nhà thầu đăng ký tài khoản, nộp hồ sơ dự thầu kèm báo giá, năng lực, tiến độ.
3. **Chấm điểm & Xếp hạng**: Ban giám khảo chấm điểm theo tiêu chí có trọng số, tự động tính tổng điểm và xếp hạng nhà thầu trúng thầu.
4. **Hợp đồng & Nghiệm thu**: Tạo hợp đồng, thiết lập các mốc thanh toán, nhà thầu cập nhật tiến độ hàng tuần, bên mua duyệt nghiệm thu giải ngân.
5. **Cảnh báo & Kiểm toán**: Tự động cảnh báo hợp đồng sắp hết hạn, ghi nhật ký kiểm toán (Audit Log) mọi thao tác.

---

## 🏗️ Kiến trúc Hệ thống

Dự án được xây dựng theo mô hình **Clean 3-Layer Architecture** tách biệt Frontend - Backend qua **RESTful API**:

```
ProcurementSystem.sln
│
├── ProcurementSystem.API/            # Tầng trình bày (Presentation Layer)
│   ├── Controllers/                  # API Endpoints (RESTful)
│   ├── Middlewares/                  # JWT Auth, Global Exception Handler
│   └── Program.cs                    # Cấu hình DI, Pipeline, Swagger, CORS
│
├── ProcurementSystem.Core/           # Tầng nghiệp vụ (Business Logic Layer)
│   ├── DTOs/                         # Request/Response Data Transfer Objects
│   ├── Enums/                        # Các bộ mã trạng thái và phân loại
│   ├── Interfaces/                   # IGenericRepository, IUnitOfWork
│   ├── Helpers/                      # Constants, quy định file upload, phân trang
│   └── Mappings/                     # AutoMapper profiles
│
└── ProcurementSystem.Infrastructure/ # Tầng truy cập dữ liệu (Data Access Layer)
    ├── Data/                         # AppDbContext (EF Core Fluent API)
    ├── Entities/                     # 16 Entities chuẩn 3NF
    ├── Repositories/                 # GenericRepository, UnitOfWork pattern
    ├── Seeders/                      # DataSeeder (Roles & Admin mặc định)
    └── Migrations/                   # EF Core database migrations
```

---

## 💻 Tech Stack

- **Runtime:** .NET 8 (C#)
- **Framework:** ASP.NET Core Web API
- **ORM:** Entity Framework Core 8 (Code-First)
- **Database:** Microsoft SQL Server
- **Authentication:** JWT Bearer Token + Role-Based Access Control (RBAC)
- **API Documentation:** Swagger / OpenAPI UI
- **Mã hóa mật khẩu:** BCrypt.Net

---

## 🚀 Hướng dẫn Cài đặt & Khởi chạy

### 1. Yêu cầu môi trường
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Microsoft SQL Server](https://www.microsoft.com/sql-server)
- [Visual Studio 2022](https://visualstudio.microsoft.com/) hoặc VS Code

### 2. Cấu hình Connection String
Mở file `ProcurementSystem.API/appsettings.json` và cấu hình kết nối SQL Server của bạn:
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost;Database=procurement_db;User Id=sa;Password=YOUR_PASSWORD;TrustServerCertificate=True;"
}
```

### 3. Cập nhật Database (Migration)
```bash
# Cài đặt EF Core CLI (nếu chưa có)
dotnet tool install --global dotnet-ef

# Cập nhật database
dotnet ef database update --project ProcurementSystem.Infrastructure --startup-project ProcurementSystem.API
```
*(Hoặc chạy trực tiếp file script `procurement_db.sql` trong SQL Server Management Studio)*

### 4. Khởi chạy ứng dụng
```bash
dotnet run --project ProcurementSystem.API
```
Truy cập giao diện Swagger UI tại: **[http://localhost:5225](http://localhost:5225)**

### 5. Tài khoản Quản trị mặc định
- **Email:** `admin@procurement.com`
- **Mật khẩu:** `Admin@123`
- **Vai trò:** `Admin`

---

## 👥 Thành viên Thực hiện
- **Phạm Minh Tài** - TTS Backend
- Đơn vị: Trường Đại học Công Thương TP.HCM (HUIT)
