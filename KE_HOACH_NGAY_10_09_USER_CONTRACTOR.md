# KẾ HOẠCH PHỐI HỢP NGÀY [10/09/2026]: USER MANAGEMENT & CONTRACTOR PROFILE
> **Dự án**: Procurement & Contract Tracking System  
> **Nguyên tắc**: 2 Lập trình viên làm việc song song trên 2 nhánh Git riêng biệt, các file hoàn toàn độc lập (Zero-Conflict).

---

## 📑 PHÂN CHIA TRÁCH NHIỆM 2 TRACK ĐỘC LẬP

```
ProcurementSystem.Core/
├── DTOs/
│   ├── User/                              <-- TRACK 1 (BẠN)
│   │   ├── UserDto.cs
│   │   ├── UserFilterParams.cs
│   │   ├── UpdateUserRequest.cs
│   │   ├── AssignRolesRequest.cs
│   │   └── RoleDto.cs
│   └── Contractor/                        <-- TRACK 2 (TEAMMATE)
│       ├── ContractorDto.cs
│       ├── ContractorFilterParams.cs
│       ├── UpdateContractorProfileRequest.cs
│       └── UpdateContractorRatingRequest.cs
├── Interfaces/
│   ├── IUserService.cs                    <-- TRACK 1 (BẠN)
│   └── IContractorService.cs              <-- TRACK 2 (TEAMMATE)
│
ProcurementSystem.Infrastructure/
├── Services/
│   ├── UserService.cs                     <-- TRACK 1 (BẠN)
│   └── ContractorService.cs               <-- TRACK 2 (TEAMMATE)
│
ProcurementSystem.API/
├── Controllers/
│   ├── UsersController.cs                 <-- TRACK 1 (BẠN)
│   ├── RolesController.cs                 <-- TRACK 1 (BẠN)
│   └── ContractorsController.cs           <-- TRACK 2 (TEAMMATE)
```

---

## 👤 TRACK 1: CHÚNG TA (BẠN)
* **Nhánh Git**: `feature/user-management`
* **Nhiệm vụ**: Quản lý Tài khoản & Phân quyền Người dùng (User Management & RBAC - Dành cho Admin)

### Các Endpoints phụ trách:
1. `GET /api/users`: Lấy danh sách người dùng (hỗ trợ phân trang `PaginatedList`, tìm kiếm theo Tên, Email, lọc theo Role và trạng thái `IsActive`). `[Authorize(Roles = "Admin")]`
2. `GET /api/users/{id}`: Xem chi tiết tài khoản (thông tin cá nhân, danh sách roles, ngày tạo, thông tin nhà thầu liên kết nếu có). `[Authorize(Roles = "Admin")]`
3. `PUT /api/users/{id}`: Cập nhật thông tin người dùng (`FullName`, `Phone`). `[Authorize(Roles = "Admin")]`
4. `PATCH /api/users/{id}/status`: Khóa hoặc Kích hoạt tài khoản (`IsActive = true/false`). Ràng buộc: Không cho phép Admin tự khóa tài khoản của chính mình. `[Authorize(Roles = "Admin")]`
5. `POST /api/users/{id}/roles`: Phân quyền / Gán vai trò (`Admin`, `Procurement`, `Evaluator`) cho nhân sự trong doanh nghiệp. `[Authorize(Roles = "Admin")]`
6. `GET /api/roles`: Lấy danh mục các vai trò có trong hệ thống. `[Authorize(Roles = "Admin")]`

---

## 👥 TRACK 2: TEAMMATE
* **Nhánh Git**: `feature/contractor-management`
* **Nhiệm vụ**: Quản lý Hồ sơ & Đánh giá Năng lực Nhà thầu (Contractor Profile & Management)

### Các Endpoints teammate phụ trách:
1. `GET /api/contractors`: Lấy danh sách nhà thầu (phân trang, tìm kiếm theo Tên công ty, Mã số thuế `TaxCode`, sắp xếp theo điểm đánh giá `Rating`). `[Authorize(Roles = "Admin,Procurement")]`
2. `GET /api/contractors/{id}`: Xem chi tiết hồ sơ nhà thầu (Tên công ty, MST, địa chỉ, đường dẫn file GPKD, điểm đánh giá, thông tin người đại diện). `[Authorize(Roles = "Admin,Procurement")]`
3. `GET /api/contractors/me`: Nhà thầu tự xem thông tin hồ sơ của chính mình dựa trên JWT Token đăng nhập. `[Authorize(Roles = "Contractor")]`
4. `PUT /api/contractors/me`: Nhà thầu tự cập nhật thông tin công ty (Tên công ty, địa chỉ, hỗ trợ upload thay thế file GPKD mới bằng `IFormFile` qua `IFileStorageService`). `[Authorize(Roles = "Contractor")]`
5. `PATCH /api/contractors/{id}/rating`: Bên mời thầu / Admin cập nhật điểm đánh giá uy tín năng lực nhà thầu (Rating từ 0 đến 5 sao). `[Authorize(Roles = "Admin,Procurement")]`

---

## 🚀 QUY TRÌNH GIT PHỐI HỢP

### Bước 1: Đồng bộ từ nhánh `dev`
Cả 2 mở Terminal và kéo code mới nhất đã merge:
```bash
git checkout dev
git pull origin dev
```

### Bước 2: Tạo nhánh riêng
* **Bạn**:
  ```bash
  git checkout -b feature/user-management
  ```
* **Teammate**:
  ```bash
  git checkout -b feature/contractor-management
  ```

### Bước 3: Đăng ký DI trong `Program.cs`
* **Bạn thêm:**
  ```csharp
  builder.Services.AddScoped<IUserService, UserService>();
  ```
* **Teammate thêm:**
  ```csharp
  builder.Services.AddScoped<IContractorService, ContractorService>();
  ```

### Bước 4: Hợp nhất (Merge)
* Người nào xong trước commit và push lên nhánh của mình rồi tạo PR vào `dev`.
* Người xong sau chỉ cần fetch `dev` về merge và push vào `dev`.
