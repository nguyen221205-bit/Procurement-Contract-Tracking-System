# TÀI LIỆU HƯỚNG DẪN CÀI ĐẶT & TÍCH HỢP HỆ THỐNG API
**Dự án:** Hệ thống Quản trị Đấu Thầu & Theo Dõi Hợp Đồng Kinh Tế (*Procurement & Contract Tracking System*)  
**Phiên bản:** v2.0 (Build 2026-09-23)  
**Nhóm phát triển:** Backend Team (Dev 1 & Dev 2 - Nguyễn Quang Phương Đông)

---

## 1. TỔNG QUAN HỆ THỐNG
Hệ thống **Procurement & Contract Tracking System** là giải pháp phần mềm quản trị toàn trình vòng đời mua sắm công và theo dõi thực hiện hợp đồng kinh tế theo mô hình kiến trúc 3 lớp (*3-layer Clean Architecture*) trên nền tảng **ASP.NET Core 8 Web API** và **Entity Framework Core 8**.

### Các Phân Hệ Chức Năng Chính:
1. **Xác thực & Phân quyền (Authentication & RBAC):** Đăng nhập, cấp phát và làm mới JWT Bearer Token với 4 vai trò chặt chẽ (`Admin`, `Procurement`, `Evaluator`, `Contractor`).
2. **Quản lý Hồ sơ Nhà thầu (Contractor Management):** Đăng ký tài khoản nhà thầu, quản lý giấy phép kinh doanh, tra cứu năng lực và đánh giá uy tín (Rating).
3. **Quản lý Gói thầu (Bid Packages):** Khởi tạo, cấu hình dự toán, mở thầu công khai và chuyển trạng thái theo luồng chuẩn (`Draft` $\rightarrow$ `Open` $\rightarrow$ `Closed` $\rightarrow$ `Evaluating` $\rightarrow$ `Awarded` $\rightarrow$ `Contracted`).
4. **Nộp & Thẩm tra Hồ sơ Dự thầu (Bid Submissions):** Tiếp nhận hồ sơ nộp thầu kèm tệp đề xuất kỹ thuật đa định dạng; kiểm soát an ninh chống lỗ hổng **IDOR** (Insecure Direct Object Reference).
5. **Chấm thầu & Phê duyệt Kết quả (Evaluations & Scoring):** Cấu hình tiêu chí có trọng số 100%, hội đồng giám khảo chấm điểm độc lập, tổng hợp bảng xếp hạng và trao thầu.
6. **Quản lý Hợp đồng & Mốc Nghiệm thu Tự động (Contracts & Milestones):** Tự động điền dữ liệu trúng thầu, sinh số hiệu hợp đồng chuẩn, ràng buộc trần ngân sách, báo cáo tiến độ tuần, nghiệm thu mốc và tự động chuyển trạng thái Hợp đồng sang `Completed`.
7. **Báo cáo & Thống kê Quản trị (Executive Dashboard):** Giám sát thời gian thực các chỉ số KPI đấu thầu, tài chính tiết kiệm và dòng tiền giải ngân thực tế (`TotalDisbursedAmount`).

---

## 2. YÊU CẦU MÔI TRƯỜNG & HẠ TẦNG
Để cài đặt và vận hành hệ thống, máy chủ hoặc máy phát triển cần đáp ứng các điều kiện sau:
- **Hệ điều hành:** Windows 10/11, Windows Server 2019/2022 hoặc Linux (Ubuntu 20.04+).
- **Runtime & SDK:** [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (phiên bản 8.0.100 trở lên).
- **Cơ sở dữ liệu:** Microsoft SQL Server 2019, 2022 hoặc Azure SQL Database.
- **Công cụ kiểm thử & tích hợp:** Postman (v10+), trình duyệt hiện đại (Chrome/Edge) để tra cứu Swagger UI.

---

## 3. HƯỚNG DẪN CÀI ĐẶT & KHỞI CHẠY (QUICK START)

### Bước 1: Mở Solution và kiểm tra cấu trúc
Thư mục dự án: `d:\Thuc Tap\DuAnTT\`
```powershell
cd "d:\Thuc Tap\DuAnTT"
```

### Bước 2: Cấu hình kết nối Cơ sở Dữ liệu
Mở tệp `ProcurementSystem.API/appsettings.json` và cấu hình chuỗi kết nối SQL Server tại mục `ConnectionStrings`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=ProcurementSystemDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
  },
  "JwtSettings": {
    "SecretKey": "ProcurementSystemSecretKeyForJwtTokenGeneration2026!@#",
    "Issuer": "ProcurementSystemAPI",
    "Audience": "ProcurementSystemClient",
    "ExpirationInMinutes": 120,
    "RefreshTokenExpirationInDays": 7
  }
}
```

### Bước 3: Cập nhật Migration & Khởi tạo CSDL
Hệ thống sử dụng EF Core Code-First với đầy đủ 16 bảng dữ liệu và hệ thống Non-Clustered Indexes tối ưu hiệu năng:
```powershell
dotnet ef database update --project ProcurementSystem.Infrastructure --startup-project ProcurementSystem.API
```

### Bước 4: Khởi chạy Ứng dụng & Nạp Dữ Liệu Mẫu (Data Seeding)
Chạy lệnh khởi động Web API:
```powershell
dotnet run --project ProcurementSystem.API
```
> [!NOTE]
> Khi khởi chạy lần đầu, khối `DataSeeder.SeedAsync()` trong `Program.cs` sẽ **tự động kiểm tra và sinh sẵn toàn bộ tài khoản mẫu và các gói thầu đa trạng thái** (Open, Evaluating, Contracted) phục vụ việc demo và kiểm thử ngay lập tức.

Ứng dụng sẽ lắng nghe tại cổng:
- **Swagger UI & API URL:** `http://localhost:5225/`

---

## 4. TÀI KHOẢN MẪU & PHÂN QUYỀN TRUY CẬP (RBAC)

Hệ thống được nạp sẵn 5 tài khoản tương ứng với 4 nhóm quyền nghiệp vụ:

| Vai trò | Email đăng nhập | Mật khẩu | Phạm vi quyền hạn |
| :--- | :--- | :--- | :--- |
| **Admin** | `admin@procurement.com` | `Admin@123` | Toàn quyền quản trị hệ thống, người dùng, xem báo cáo KPI Dashboard. |
| **Procurement** | `procurement@procurement.com` | `Procurement@123` | Quản lý gói thầu, lập hợp đồng, phê duyệt mốc nghiệm thu thanh toán. |
| **Evaluator** | `evaluator1@procurement.com` | `Evaluator@123` | Thẩm định, chấm điểm hồ sơ dự thầu theo tiêu chí có trọng số. |
| **Contractor 1** | `contractor1@fpt.com` | `Contractor@123` | Nhà thầu FPT: Nộp hồ sơ thầu, xem hợp đồng của mình, nộp báo cáo tiến độ tuần. |
| **Contractor 2** | `contractor2@viettel.com` | `Contractor@123` | Nhà thầu Viettel: Nộp thầu cạnh tranh, dùng kiểm thử an ninh IDOR chéo. |

---

## 5. TRA CỨU ĐẶC TẢ API BẰNG SWAGGER UI

Hệ thống đã được tích hợp toàn diện **Swagger XML Documentation** trực tiếp tại Root URL:
- Đường dẫn: **`http://localhost:5225/`**
- Swagger JSON Schema: **`http://localhost:5225/swagger/v1/swagger.json`**

### Điểm nổi bật của tài liệu Swagger UI:
- **100% Tiếng Việt chuẩn mực:** Toàn bộ thẻ `<summary>`, `<param>`, `<remarks>` được biên soạn chi tiết, dễ hiểu.
- **Khai báo mã trạng thái rõ ràng:** Tất cả các endpoint đều khai báo đầy đủ các phản hồi: `200 OK`, `201 Created`, `400 BadRequest`, `401 Unauthorized`, `403 Forbidden`, `404 NotFound`, `409 Conflict`.
- **Hỗ trợ Authorize trực quan:** Bấm nút `Authorize` trên góc phải Swagger UI, nhập chuỗi `Bearer {token}` để thực hiện gọi thử nghiệm API trực tiếp trên trình duyệt.

---

## 6. HƯỚNG DẪN IMPORT & SỬ DỤNG POSTMAN COLLECTION V2

Bộ sưu tập kiểm thử toàn diện đã được đóng gói chuẩn mực tại tệp:  
`d:\Thuc Tap\DuAnTT\Procurement_API_v2_Postman_Collection.json`

### Các bước tích hợp:
1. Mở ứng dụng **Postman**.
2. Chọn **File** $\rightarrow$ **Import** $\rightarrow$ Kéo thả tệp `Procurement_API_v2_Postman_Collection.json`.
3. Bộ sưu tập sẽ tự động tạo sẵn 7 thư mục nghiệp vụ và bảng **Collection Variables**:
   - `baseUrl`: Mặc định là `http://localhost:5225`.
   - `adminToken`, `procurementToken`, `contractorToken`, `evaluatorToken`: Tự động được script test lưu lại sau khi gọi các request trong thư mục `01. Authentication`.
   - `packageId`, `contractId`, `milestoneId`, `submissionFileId`: Tham số động phục vụ chuỗi kiểm thử liên hoàn.

---

## 7. CÁC QUY TẮC NGHIỆP VỤ & AN NINH BẢO MẬT CỐT LÕI

### 1. Kiểm soát An ninh Chống IDOR (Insecure Direct Object Reference)
- Endpoint xem lịch sử hợp đồng `GET /api/contracts/contractor/{id}` và tải file dự thầu `GET /api/submissions/files/{fileId}/download`:
  - Người dùng có vai trò `Contractor` **chỉ được phép truy cập tài nguyên do chính mình sở hữu**.
  - Nếu cố tình đổi ID để xem hợp đồng hoặc tải tệp hồ sơ của nhà thầu đối thủ $\rightarrow$ Hệ thống phản hồi ngay lập tức mã lỗi **`403 Forbidden`**.

### 2. Kiểm soát Trần Ngân sách Hợp đồng & Mốc Thanh toán
- Khi thêm mốc (`POST /api/contracts/{id}/milestones`) hoặc cập nhật mốc (`PUT /api/contracts/{id}/milestones/{milestoneId}`):
  - Hệ thống kiểm tra: $\sum (\text{Milestone.Amount}) \le \text{Contract.Value}$.
  - Nếu vượt quá giá trị hợp đồng $\rightarrow$ Báo lỗi **`400 Bad Request`** kèm số tiền chênh lệch cụ thể.

### 3. Tự động hóa Nghiệm thu & Chuyển đổi Trạng thái Hợp đồng
- Khi phê duyệt mốc qua `PUT /api/contracts/milestones/{id}/accept`:
  - Mốc chuyển trạng thái `Completed`.
  - Hệ thống tự động kiểm tra: Nếu **tất cả** các mốc của hợp đồng đều đã `Completed`, trạng thái hợp đồng (`Contract.Status`) được tự động chuyển từ `Active` sang **`Completed`**.
  - Tự động cộng dồn số tiền đã giải ngân vào chỉ số `TotalDisbursedAmount` và tính toán lại `DisbursementRate` thời gian thực trên Dashboard quản trị.
