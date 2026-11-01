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
5. **Quản lý Hợp đồng & Nghiệm thu (Contracts & Milestones)**: Thiết lập hợp đồng, theo dõi milestone và giải ngân. Quyền ghi được giới hạn theo membership đang hoạt động của đúng Bên mời thầu; nghiệm thu tách người lập (Maker) khỏi Checker có signing authority hợp lệ.
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

### 2. Cấu hình Chuỗi Kết Nối & Bảo Mật (Connection String & Secrets)
Bạn có thể cấu hình nhanh thông qua tệp `ProcurementSystem.API/appsettings.json` (sử dụng tệp mẫu tham khảo `appsettings.Example.json`):

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost;Database=procurement_db;User Id=sa;Password=YOUR_STRONG_PASSWORD;TrustServerCertificate=True;MultipleActiveResultSets=True;"
}
```

> **Khuyến nghị bảo mật môi trường Production (P3-1)**: Hệ thống hỗ trợ nạp tự động qua biến môi trường (Environment Variables) để cách ly thông tin nhạy cảm:
> - `DB_CONNECTION_STRING`: Chuỗi kết nối đến Microsoft SQL Server.
> - `JWT_SECRET_KEY`: Khóa bí mật ký JWT Token (tối thiểu 32 ký tự).

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

## 🔎 Minh bạch và kiểm soát Procurement

Hệ thống gắn mọi thao tác với một người dùng đăng nhập riêng và một Bên mời thầu cụ thể. Vai trò toàn hệ thống như **Procurement** không tự cấp quyền trên mọi đơn vị.

- **Tài khoản cá nhân và lời mời thành viên:** Mỗi lãnh đạo, cán bộ, kế toán và người ký dùng tài khoản/email riêng. Manager mời thành viên vào đơn vị; người được mời đăng nhập hoặc đăng ký bằng đúng email rồi chấp nhận lời mời. Không chia sẻ mật khẩu, OTP hoặc token ký.
- **Vai trò trong đơn vị:** Manager quản lý thành viên; Officer thực hiện công việc nghiệp vụ (Maker); Finance tham gia kiểm soát tài chính; người ký cần có signing authority được xác minh. Quyền trong đơn vị chỉ có hiệu lực khi membership còn Active.
- **Tách người lập và người duyệt:** Người thực hiện bước kỹ thuật không được tự phê duyệt nghiệm thu/giải ngân của chính mình. Người kiểm tra cần là thành viên hợp lệ, có thẩm quyền ký đã được xác minh và có danh tính khác người lập. Admin quản trị nền tảng, không thay thế người có thẩm quyền của đơn vị để phê duyệt chi.
- **Quyền sở hữu gói thầu ổn định:** Gói thầu lưu ProcuringEntityId tại thời điểm tạo. Quyền không chuyển sang đơn vị khác chỉ vì người tạo thay đổi membership. Tạo gói yêu cầu chọn đơn vị mà người dùng có membership hợp lệ; máy chủ kiểm tra lại lựa chọn, kể cả khi FE gửi ID giả.
- **Dữ liệu lịch sử thiếu chứng cứ:** Migration đánh dấu các gói chưa xác định được đơn vị bằng OwnershipNeedsReview, giữ giá trị ứng viên để đối soát và ghi audit. Admin có thể ghi nhận quyết định cùng mã tham chiếu chứng cứ; API không tự kết luận tính pháp lý của tài liệu tham chiếu. Gói bị đánh dấu cần được đối soát trước khi dùng các thao tác nhạy cảm.
- **Nghiệm thu gắn với đúng phiên bản:** Quyết định Checker tham chiếu AcceptanceId, ExpectedRevision và ExpectedVersion. Thay đổi nội dung sau thẩm tra làm kết quả cũ mất hiệu lực; request dùng phiên bản cũ bị từ chối. Bản chụp tài liệu được băm SHA-256 từ bytes thực tế và chỉ tải qua endpoint có xác thực/phân quyền.

Các endpoint hỗ trợ kiểm tra phạm vi đơn vị:

- GET /api/bid-packages/creatable-entities: trả các đơn vị hiện tại người dùng được phép chọn.
- GET /api/bid-packages/ownership-review: Admin xem danh sách gói cần đối soát.
- POST /api/bid-packages/{id}/ownership-review: Admin ghi nhận đơn vị đã xác minh, mã chứng cứ, ghi chú và audit.

---

## 👥 Tài khoản và dữ liệu kiểm thử

DataSeeder có thể tạo tài khoản mẫu cho môi trường phát triển. Kiểm tra tài khoản thực tế trong mã seed và database đang dùng; không dựa vào tài khoản mẫu để chứng minh tách biệt Maker–Checker. Không dùng thông tin đăng nhập mẫu ở môi trường thật.

Để kiểm tra minh bạch, chuẩn bị các danh tính độc lập: Manager, Officer/Maker, người có signing authority/Checker, ít nhất hai nhà thầu và các thành viên Tổ chuyên gia. Tạo hoặc mời từng người bằng email riêng. Không dùng chung một tài khoản Procurement cho nhiều người.

---

## 🧪 Kiểm thử tự động

### Backend

Từ thư mục gốc:

~~~powershell
dotnet test tests/ProcurementSystem.Tests/ProcurementSystem.Tests.csproj
~~~

Bộ kiểm thử gồm kiểm tra nghiệp vụ, membership/Maker–Checker, xử lý revision và các kiểm thử SQL Server LocalDB. Một số ca tạo database thử nghiệm riêng, chạy migration thật, kiểm tra cạnh tranh ghi và dựng Kestrel/JWT/HttpClient. LocalDB không khả dụng thì nhóm test SQL Server sẽ **Fail rõ ràng**, không tự chuyển thành Skip hoặc PASS.

### Frontend

~~~powershell
npm install --prefix frontend
npm test --prefix frontend
npm run build --prefix frontend
~~~

Các bài test frontend hiện dùng Node test runner cho enum contract và tính điểm xem trước. Chúng không phải kiểm thử giao diện trình duyệt.

### Script tích hợp đời cũ

Đường dẫn scripts/testing/test_e2e_full_flow.ps1 được viết cho dữ liệu mẫu và luồng Admin tạo gói cũ. Script không chọn ProcuringEntityId, không tạo membership/invitation và không kiểm tra Maker–Checker; nó cũng chỉ tạo milestone chứ không hoàn tất nghiệm thu. Không dùng script này làm bằng chứng E2E cho transparency hiện tại hoặc chạy trên database thật. Hãy dùng quy trình thực hành bên dưới trên database cô lập.

---

## 🔁 Hướng dẫn E2E mở thầu → trao thầu → nghiệm thu

Đây là quy trình thực hành qua giao diện web và Swagger. Nó tạo dữ liệu nghiệp vụ; chạy trên database dùng riêng cho kiểm thử, không trỏ vào database gốc.

### 1. Khởi chạy trên database cô lập

Dùng SQL Server LocalDB hoặc SQL Server thử nghiệm và đặt chuỗi kết nối vào database mới, ví dụ:

~~~powershell
$env:DB_CONNECTION_STRING = 'Server=(localdb)\MSSQLLocalDB;Database=Procurement_E2E;Trusted_Connection=True;TrustServerCertificate=True'
dotnet run --project ProcurementSystem.API
~~~

API tự áp dụng migration khi khởi động theo cấu hình ứng dụng. Vì vậy cần xác nhận connection string trước khi chạy. Mở Swagger tại http://localhost:5225/swagger. Mở terminal khác để chạy frontend:

~~~powershell
npm install --prefix frontend
npm run dev --prefix frontend
~~~

### 2. Chuẩn bị đơn vị và danh tính

1. Tạo/duyệt một Bên mời thầu thử nghiệm.
2. Đăng nhập bằng tài khoản Manager riêng; mời một tài khoản Officer và chấp nhận lời mời bằng đúng email được mời.
3. Hoàn tất quy trình xác minh signing authority cho một tài khoản Checker khác. Không gán vai trò người ký bằng cách sửa trực tiếp dữ liệu hoặc dùng chung tài khoản Maker.
4. Tạo ít nhất hai tài khoản nhà thầu độc lập và đủ tài khoản Evaluator theo yêu cầu cấu hình Tổ chuyên gia.

### 3. Mở thầu, nhận hồ sơ, đánh giá và trao thầu

1. Manager/Officer gọi GET /api/bid-packages/creatable-entities; chọn đúng đơn vị được trả về khi tạo gói qua UI/API. Không gửi ProcuringEntityId phải bị từ chối; gửi ID của đơn vị mà tài khoản không là thành viên phải bị từ chối.
2. Tạo tiêu chí, đăng tải gói thầu và cho từng nhà thầu nộp hồ sơ bằng tài khoản riêng.
3. Đóng thầu, chuyển sang đánh giá, phân công chuyên gia và chấm hồ sơ theo tiêu chí.
4. Hoàn tất xếp hạng và trao thầu. Xác nhận gói được gắn với đơn vị đã chọn từ đầu, người ngoài đơn vị không thể sửa hồ sơ.

### 4. Tạo hợp đồng và nghiệm thu theo Maker–Checker

1. Tạo hợp đồng từ kết quả trúng thầu; tạo các milestone, đính kèm bản scan và hoàn tất điều kiện để hợp đồng có hiệu lực.
2. Officer/Maker thực hiện kiểm tra kỹ thuật:

~~~http
PUT /api/contracts/milestones/{milestoneId}/technical-review
Content-Type: application/json

{"isPassed": true, "note": "Kết quả kiểm tra tại hiện trường"}
~~~

3. Checker đăng nhập bằng tài khoản khác, có membership đang hoạt động và signing authority hợp lệ. Dùng AcceptanceId, ExpectedRevision, ExpectedVersion trả về từ bản ghi kiểm tra kỹ thuật để phê duyệt:

~~~http
PUT /api/contracts/milestones/{milestoneId}/acceptance
Content-Type: application/json

{
  "isApproved": true,
  "note": "Đã đối chiếu hồ sơ nghiệm thu",
  "acceptanceId": 12,
  "expectedRevision": 1,
  "expectedVersion": 3
}
~~~

Thay 12, 1, 3 bằng giá trị thực của lần chạy; không cố định các ID ví dụ. Lặp lại cho các milestone còn lại và xác nhận hợp đồng chuyển trạng thái hoàn tất theo quy tắc của dự án.

### 5. Kiểm tra âm và dấu vết

- Maker tự duyệt bản nghiệm thu của mình: phải bị chặn.
- Người không thuộc đơn vị hoặc membership đã bị thu hồi: không được tiếp tục thao tác ghi/duyệt.
- Gửi ExpectedRevision hoặc ExpectedVersion cũ sau khi nội dung thay đổi: phải bị từ chối do xung đột phiên bản.
- Tải bản chụp nghiệm thu khi chưa đăng nhập hoặc không thuộc phạm vi: phải bị chặn; người đủ quyền tải được đúng tài liệu đã lưu.
- Đối chiếu audit log của các hành động mời thành viên, đổi quyền/thu hồi, nghiệm thu và xử lý ownership review.

Kết quả chạy manual cần ghi lại SHA/nhánh, database kiểm thử, actor từng bước, request/response và kết quả mong đợi/thực tế. Không ghi “browser E2E PASS” nếu chưa thực sự chạy toàn bộ quy trình trên giao diện trình duyệt.

---

## 🧰 Kỹ thuật được dùng cho phần minh bạch

- **ASP.NET Core Authorization + JWT:** kiểm tra danh tính người gọi ở API; dịch vụ kiểm tra lại membership, vai trò, trạng thái đơn vị và signing authority ở phía server.
- **Entity-scoped authorization:** ProcuringEntityId là chủ sở hữu ổn định của gói thầu; lựa chọn đơn vị được xác nhận từ membership hiện hành, không suy ra từ CreatedBy.
- **Maker–Checker và least privilege:** vai trò đơn vị, signing authority, trạng thái membership và danh tính Maker/Checker là các điều kiện riêng; Admin nền tảng không thay thế Checker.
- **EF Core transaction và optimistic concurrency:** các thao tác nhạy cảm cập nhật có transaction; RowVersion/concurrency token giúp phát hiện thu hồi membership hoặc chỉnh sửa xảy ra đồng thời.
- **Revision-bound acceptance:** quyết định duyệt tham chiếu đúng acceptance/revision/version; bản thẩm tra cũ không còn dùng được sau khi milestone hoặc tài liệu thay đổi.
- **SHA-256 trên bytes và authenticated file download:** checksum phản ánh chính nội dung tệp được lưu; endpoint tải tài liệu kiểm tra xác thực và phạm vi truy cập.
- **Migrations có kiểm soát và audit trail:** dữ liệu ownership lịch sử thiếu bằng chứng được quarantine để đối soát; quyết định khắc phục được lưu actor, giá trị trước/sau và mã chứng cứ.
- **Kiểm thử nhiều tầng:** unit/service tests; SQL Server LocalDB cho migration và interleaving thực; HTTP integration qua Kestrel/JWT/HttpClient; Node tests cho một số logic frontend.

---

## 🎓 Thông Tin Báo Cáo Học Thuật

- **Sinh viên thực hiện:** Phạm Minh Tài
- **Vị trí thực tập:** Backend Software Engineer Intern
- **Đơn vị đào tạo:** Trường Đại học Công Thương TP.HCM (HUIT)
- **Khoa:** Công nghệ Thông tin
- **Dự án:** Hệ thống Quản lý Đấu Thầu & Theo Dõi Hợp Đồng (*Procurement & Contract Tracking System*)
- **Kho lưu trữ GitHub:** [https://github.com/nguyen221205-bit/Procurement-Contract-Tracking-System](https://github.com/nguyen221205-bit/Procurement-Contract-Tracking-System)
