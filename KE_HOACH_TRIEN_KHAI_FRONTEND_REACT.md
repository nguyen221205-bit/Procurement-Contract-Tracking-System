# KẾ HOẠCH TRIỂN KHAI PHÂN HỆ FRONTEND (REACT + VITE)
### Hệ Thống Quản Lý Đấu Thầu & Theo Dõi Hợp Đồng (*Procurement & Contract Tracking System*)

> **Tài liệu kỹ thuật nội bộ dành cho nhóm phát triển (Dev 1 & Dev 2)**  
> **Thời gian bắt đầu dự kiến:** Tuần 6 (Giai đoạn Ghép nối Giao diện & Trình diễn Nghiệp vụ Toàn trình)  
> **Mục tiêu:** Xây dựng ứng dụng Single Page Application (SPA) chuẩn Doanh nghiệp với React + Vite, kết nối trực tiếp vào Backend .NET 8 Web API (`http://localhost:5225`), giao diện hiện đại, trải nghiệm mượt mà và phân quyền động theo 4 vai trò.

---

## 📌 PHẦN I: BỐI CẢNH & MỤC TIÊU DỰ ÁN

### 1. Hiện trạng Phía Backend (Đã Hoàn Thành 100%)
- **Nền tảng:** ASP.NET Core Web API (.NET 8), Clean 3-Layer Architecture, Microsoft SQL Server chuẩn 3NF.
- **Quy mô API:** 11 RESTful Controllers bao phủ trọn vẹn 42 Endpoints đã được chuẩn hóa 100% bằng Swagger XML Documentation.
- **Bảo mật:** JWT Bearer Token (HMAC-SHA256) + Refresh Token luân chuyển, băm mật khẩu BCrypt, phân quyền chặt chẽ 4 vai trò (`Admin`, `Procurement`, `Evaluator`, `Contractor`), chống lỗ hổng IDOR khi tải tệp.
- **Dữ liệu hạt giống (Extended Data Seeder):** Tự động sinh sẵn 4 nhóm vai trò, 5 tài khoản mẫu và 3 gói thầu mô phỏng đầy đủ vòng đời (Mở thầu, Đang chấm điểm, Đã trao thầu có hợp đồng và mốc thanh toán nghiệm thu giải ngân).
- **Cấu hình CORS:** Backend đã cấu hình sẵn sàng cho phép gọi API từ bất kỳ cổng nào của Frontend (bao gồm Vite mặc định `http://localhost:5173`).

### 2. Mục tiêu Phía Frontend (React + Vite)
- Chuyển đổi từ mô hình thử nghiệm API trên Swagger UI sang giao diện người dùng thực tế (Web Client), mang lại trải nghiệm chuyên nghiệp, trực quan cho Hội đồng bảo vệ đồ án và Người dùng cuối.
- Ứng dụng mô hình **Single Page Application (SPA)**: Điều hướng giữa các trang tức thì trong chớp mắt mà không cần tải lại trang.
- Hiển thị giao diện điều hướng (Menu / Sidebar) thông minh, phân quyền chặt chẽ theo vai trò người dùng sau khi đăng nhập.

---

## 💻 PHẦN II: TECH STACK & CÔNG NGHỆ LỰA CHỌN

| Thành Phần | Công Nghệ / Thư Viện | Phiên Bản | Vai Trò & Lý Do Lựa Chọn |
|---|---|---|---|
| **Build Tool & Framework** | **React + Vite** | React 18/19 | Khởi động dự án cực nhanh, Hot Module Replacement (HMR) phản hồi trong vài mili-giây, nhẹ hơn Webpack/CRA gấp nhiều lần |
| **CSS Framework** | **Tailwind CSS** | v3/v4 | Tùy biến giao diện Dashboard SaaS hiện đại, responsive, không bị giới hạn bởi component dựng sẵn |
| **Bộ Icon Hệ Thống** | **Lucide React** | Mới nhất | Bộ icon chuẩn thiết kế hiện đại, nhẹ, đẹp và đồng bộ |
| **Định Tuyến & Điều Hướng** | **React Router DOM** | v6.x | Định tuyến Single Page, quản lý Nested Routes, xây dựng `ProtectedRoute` chặn quyền truy cập |
| **Giao Tiếp Mạng HTTP** | **Axios** | v1.x | Cấu hình BaseURL, tự động gắn Header `Authorization: Bearer <token>`, bắt mã 401 điều hướng login |
| **Trực Quan Hóa Dữ Liệu** | **Chart.js + react-chartjs-2** | Mới nhất | Vẽ biểu đồ KPI tài chính, tỷ lệ tiết kiệm ngân sách qua đấu thầu và tiến độ giải ngân mốc thanh toán |
| **Thông Báo Popup** | **React Hot Toast** | Mới nhất | Hiển thị thông báo Toast thành công/thất bại mượt mà, thanh lịch |

---

## 🏗️ PHẦN III: KIẾN TRÚC THƯ MỤC DỰ ÁN (`frontend/`)

Dự án Frontend sẽ được đặt trong thư mục `frontend/` ngang hàng với các tầng của Backend:

```
Procurement & Contract Tracking System/
├── ProcurementSystem.API/              # Backend .NET 8 (Port 5225)
├── ProcurementSystem.Core/
├── ProcurementSystem.Infrastructure/
└── frontend/                           # Ứng dụng React mới (Port 5173)
    ├── package.json
    ├── vite.config.js
    ├── tailwind.config.js
    ├── index.html
    └── src/
        ├── api/                        # Tầng kết nối RESTful API Backend
        │   ├── axiosClient.js          # Cấu hình Axios instance + Interceptors tự động gắn JWT
        │   ├── authApi.js              # Đăng nhập, đăng ký nhà thầu, thông tin cá nhân
        │   ├── bidPackageApi.js        # Lấy danh sách, tìm kiếm, xem chi tiết, tạo gói thầu, đổi trạng thái
        │   ├── submissionApi.js        # Nộp hồ sơ dự thầu, upload file đề xuất kỹ thuật (FormData)
        │   ├── evaluationApi.js        # Cấu hình tiêu chí trọng số, giám khảo nộp điểm, xếp hạng, trao thầu
        │   ├── contractApi.js          # Hợp đồng kinh tế, cập nhật tiến độ mốc, duyệt nghiệm thu
        │   ├── reportApi.js            # Thống kê KPI Dashboard thời gian thực
        │   └── healthApi.js            # Kiểm tra trạng thái máy chủ
        ├── context/
        │   └── AuthContext.jsx         # Global State: Quản lý User, JWT Token, Role, Logout
        ├── components/
        │   ├── common/
        │   │   ├── Navbar.jsx          # Thanh điều hướng trên: Tên người dùng, Badge vai trò, Đăng xuất
        │   │   ├── Sidebar.jsx         # Menu động theo quyền (Admin, Procurement, Evaluator, Contractor)
        │   │   ├── ProtectedRoute.jsx  # Route Guard: Chặn truy cập trái phép nếu không đúng vai trò
        │   │   ├── StatusBadge.jsx     # Badge màu hiển thị trạng thái gói thầu và hợp đồng
        │   │   ├── LoadingSpinner.jsx  # Hiệu ứng đang tải dữ liệu
        │   │   └── Modal.jsx           # Hộp thoại xác nhận thao tác quan trọng
        │   └── dashboard/
        │       ├── StatCard.jsx        # Thẻ hiển thị KPI ngân sách, số gói thầu, tiết kiệm
        │       └── FinancialCharts.jsx # Biểu đồ tài chính Chart.js
        ├── pages/
        │   ├── LoginPage.jsx           # Đăng nhập (kèm bảng nút 1-Click chọn nhanh 5 tài khoản mẫu)
        │   ├── RegisterPage.jsx        # Đăng ký hồ sơ năng lực nhà thầu
        │   ├── DashboardPage.jsx       # Bảng điều hành tổng quan dành cho Quản trị viên
        │   ├── PackagesPage.jsx        # Danh sách & bộ lọc trạng thái gói thầu
        │   ├── PackageDetailPage.jsx   # Chi tiết gói thầu, tải HSMT, form nộp hồ sơ
        │   ├── EvaluationPage.jsx      # Giao diện chấm điểm tiêu chí có trọng số & xếp hạng trúng thầu
        │   ├── ContractsPage.jsx       # Quản lý hợp đồng & danh sách các mốc giải ngân
        │   ├── ContractDetailPage.jsx # Chi tiết hợp đồng, cập nhật tiến độ & duyệt nghiệm thu
        │   └── UsersPage.jsx           # Quản trị tài khoản người dùng (Admin)
        └── utils/
            ├── formatters.js           # Định dạng tiền tệ VNĐ (ví dụ: 1.800.000.000 đ) & ngày tháng
            └── constants.js            # Các hằng số trạng thái, mã vai trò và màu sắc nhận diện
```

---

## 👥 PHẦN IV: MA TRẬN MÀN HÌNH THEO 4 VAI TRÒ (ROLE MATRIX)

Hệ thống điều hướng linh hoạt, menu hiển thị tương ứng theo vai trò của tài khoản đăng nhập:

| Màn Hình Giao Diện | Admin | Procurement | Evaluator | Contractor | Mô Tả Chức Năng Chính |
|---|:---:|:---:|:---:|:---:|---|
| **Đăng nhập (`/login`)** | ✅ | ✅ | ✅ | ✅ | Đăng nhập hệ thống, có bảng nút 1-Click điền sẵn 5 tài khoản mẫu |
| **Đăng ký Nhà thầu (`/register`)** | ❌ | ❌ | ❌ | ✅ | Đăng ký tài khoản doanh nghiệp kèm thông tin mã số thuế, địa chỉ |
| **Dashboard (`/dashboard`)** | ✅ | ❌ | ❌ | ❌ | Biểu đồ KPI tài chính, tiến độ giải ngân, tổng gói thầu & hợp đồng |
| **Danh sách Gói thầu (`/packages`)** | ✅ | ✅ | ✅ | ✅ | Xem danh sách gói thầu, thanh lọc trạng thái (`Open`, `Evaluating`, `Contracted`) |
| **Tạo Gói thầu mới (`/packages/create`)** | ✅ | ✅ | ❌ | ❌ | Tạo gói thầu, nhập ngân sách dự toán, quy định hạn nộp thầu |
| **Chi tiết Gói thầu (`/packages/:id`)** | ✅ | ✅ | ✅ | ✅ | Xem thông tin chi tiết, tải HSMT, xem danh sách hồ sơ thầu đã nộp |
| **Nộp Hồ sơ Dự thầu (`Form Modal`)** | ❌ | ❌ | ❌ | ✅ | Nhà thầu nhập giá chào, cam kết tiến độ và đính kèm file kỹ thuật |
| **Chấm điểm & Xếp hạng (`/evaluation/:pkgId`)**| ✅ | ❌ | ✅ | ❌ | Chấm điểm từng tiêu chí (thang 0-100), xem bảng xếp hạng & nút trao thầu |
| **Danh sách Hợp đồng (`/contracts`)** | ✅ | ✅ | ❌ | ✅ | Xem các hợp đồng kinh tế đã ký, giá trị hợp đồng, tỷ lệ giải ngân |
| **Chi tiết Hợp đồng (`/contracts/:id`)** | ✅ | ✅ | ❌ | ✅ | Xem các mốc thanh toán, nhà thầu cập nhật tiến độ, bên mua duyệt nghiệm thu |
| **Quản trị Người dùng (`/users`)** | ✅ | ❌ | ❌ | ❌ | Quản lý danh sách tài khoản, khóa/mở khóa người dùng |

---

## 🚀 PHẦN V: LỘ TRÌNH TRIỂN KHAI 4 GIAI ĐOẠN (TUẦN 6)

### 🔹 Giai đoạn 1: Khởi Tạo Bộ Khung & Tầng Xác Thực (Foundation & Auth)
- **Công việc:**
  1. Khởi tạo dự án Vite React trong thư mục `frontend/`.
  2. Cài đặt các packages: `axios`, `react-router-dom`, `lucide-react`, `tailwindcss`, `postcss`, `autoprefixer`, `react-hot-toast`, `chart.js`, `react-chartjs-2`.
  3. Cấu hình `src/api/axiosClient.js`: Trỏ vào `http://localhost:5225/api`, bắt mã lỗi 401 tự động chuyển về `/login`.
  4. Xây dựng `AuthContext.jsx`: Lưu thông tin `user` và `token` vào `localStorage`, cung cấp các hàm `login()`, `logout()`, `hasRole()`.
  5. Xây dựng Layout chuẩn: `Navbar`, `Sidebar` với menu tự động thay đổi theo vai trò.
  6. Xây dựng màn hình `LoginPage.jsx` thông minh: Ngoài form đăng nhập thông thường, thiết kế **Bảng 5 nút bấm 1-Click chọn nhanh tài khoản mẫu**:
     - 👑 *Admin* (`admin@procurement.com`)
     - 💼 *Procurement* (`procurement@procurement.com`)
     - ⚖️ *Evaluator* (`evaluator@procurement.com`)
     - 🏢 *Nhà thầu 1* (`contractor1@test.com`)
     - 🏗️ *Nhà thầu 2* (`contractor2@test.com`)  
     *(Giúp hội đồng chấm thi hoặc người xem chỉ cần bấm 1 click là đăng nhập thử nghiệm ngay mà không cần nhớ mật khẩu).*

### 🔹 Giai đoạn 2: Phân Hệ Quản Lý Gói Thầu & Nộp Hồ Sơ Dự Thầu (Bidding & Submissions)
- **Công việc:**
  1. Xây dựng `PackagesPage.jsx`:
     - Gọi API `GET /api/bid-packages` với phân trang và bộ lọc trạng thái.
     - Hiển thị danh sách gói thầu dạng lưới thẻ (Cards) bắt mắt, mỗi thẻ có StatusBadge (`Đang mở`, `Đang chấm`, `Đã ký HĐ`).
  2. Xây dựng `PackageDetailPage.jsx`:
     - Hiển thị đầy đủ thông tin: Mã gói thầu, tên gói, chủ đầu tư, ngân sách, đếm ngược thời hạn nộp thầu.
     - Nút tải tài liệu hồ sơ mời thầu (HSMT).
  3. Xây dựng Form tạo gói thầu mới (dành riêng cho vai trò `Procurement` / `Admin`).
  4. Xây dựng Form nộp hồ sơ dự thầu (dành cho `Contractor`):
     - Nhập giá dự thầu, cam kết thời gian hoàn thành.
     - Kéo thả / Chọn tệp đề xuất kỹ thuật (`multipart/form-data`) gửi lên API `POST /api/bid-packages/{id}/submissions`.

### 🔹 Giai đoạn 3: Phân Hệ Chấm Điểm Độc Lập & Bảng Xếp Hạng (Evaluation & Award)
- **Công việc:**
  1. Màn hình cấu hình tiêu chí chấm thầu:
     - Thêm tiêu chí đánh giá và trọng số (Kiểm tra tổng trọng số phải đạt đúng 100%).
  2. Màn hình chấm điểm thầu trực quan dành cho Giám khảo (`Evaluator`):
     - Hiển thị danh sách hồ sơ của các nhà thầu tham gia.
     - Phiếu chấm điểm trực quan: Thanh trượt (slider 0–100) hoặc ô nhập điểm cho từng tiêu chí, tự động tính điểm trung bình có trọng số.
  3. Bảng xếp hạng thứ hạng (Leaderboard):
     - Hiển thị danh sách xếp hạng nhà thầu (Rank 1, 2, 3) dựa trên tổng điểm có trọng số từ API `GET /api/evaluations/packages/{id}/summary`.
     - Nút bấm phê duyệt trúng thầu (`Finalize Evaluation`) dành cho Quản trị viên: Tự động trao thầu cho Rank 1 và chuyển trạng thái sang `Contracted`.

### 🔹 Giai đoạn 4: Hợp Đồng, Nghiệm Thu Giải Ngân & Dashboard Điều Hành (Contracts & Dashboard)
- **Công việc:**
  1. Xây dựng `ContractsPage.jsx`:
     - Danh sách hợp đồng kinh tế đã ký kết, hiển thị tên nhà thầu, số hợp đồng, tổng giá trị và thanh tiến độ giải ngân (`Progress Bar`).
  2. Xây dựng `ContractDetailPage.jsx`:
     - Danh sách chi tiết các mốc thanh toán (`Milestones`):
       • Nhà thầu bấm cập nhật tiến độ thực hiện mốc (`ProgressUpdate`).
       • Bên mời thầu / Admin bấm duyệt nghiệm thu mốc (`Acceptance`) để giải ngân thực tế.
  3. Xây dựng `DashboardPage.jsx`:
     - Tích hợp thư viện Chart.js hiển thị biểu đồ ngân sách, biểu đồ hình tròn phân bổ trạng thái gói thầu, tỷ lệ tiết kiệm qua đấu thầu và tiến độ giải ngân thời gian thực từ API `GET /api/reports/dashboard`.
  4. Kiểm thử tích hợp toàn trình liên thông Frontend $\leftrightarrow$ Backend.

---

## 🤝 PHẦN VI: PHÂN CÔNG TRÁCH NHIỆM NHÓM (DEV 1 & DEV 2)

Nhằm đảm bảo tiến độ triển khai nhanh và đồng đều, công việc Tuần 6 được phân chia như sau:

| Thành Viên | Phân Vùng Trách Nhiệm Trọng Tâm | Các Màn Hình & Module Phụ Trách |
|---|---|---|
| **DEV 1 (Track 1)** | **Hạ tầng Core, Xác thực, Gói thầu & Dashboard** | - Khởi tạo dự án Vite, cấu hình Tailwind CSS, Axios Client, AuthContext.<br>- Xây dựng Layout (`Navbar`, `Sidebar`), điều hướng phân quyền `ProtectedRoute`.<br>- Màn hình `LoginPage.jsx` (kèm bảng nút 1-Click đăng nhập 5 tài khoản mẫu).<br>- Phân hệ Gói thầu: `PackagesPage.jsx`, `PackageDetailPage.jsx` và Form tạo thầu.<br>- Màn hình `DashboardPage.jsx` kết nối Chart.js vẽ biểu đồ KPI tài chính. |
| **DEV 2 (Track 2)** | **Nộp thầu, Chấm điểm trọng số, Hợp đồng & Nghiệm thu** | - Màn hình `RegisterPage.jsx` (Đăng ký tài khoản nhà thầu).<br>- Form nộp hồ sơ dự thầu kèm upload file đề xuất kỹ thuật.<br>- Phân hệ Chấm điểm: `EvaluationPage.jsx` (Phiếu chấm điểm trượt của giám khảo, bảng xếp hạng Rank 1-2-3 và nút phê duyệt trúng thầu).<br>- Phân hệ Hợp đồng: `ContractsPage.jsx`, `ContractDetailPage.jsx` (Danh sách mốc thanh toán, nút cập nhật tiến độ và nút duyệt nghiệm thu giải ngân).<br>- Rà soát kiểm thử form validation và giao diện responsive. |

---

## 📋 PHẦN VII: QUY TRÌNH PHỐI HỢP GIT TRONG TUẦN 6

1. **Thư mục làm việc:** Cả hai làm việc trong thư mục `frontend/`.
2. **Quy tắc phân nhánh:**
   - Dev 1: `feature/fe-core-packages-dashboard` (tách từ `dev`).
   - Dev 2: `feature/fe-evaluation-contracts` (tách từ `dev`).
3. **Quy tắc chạy ứng dụng trên máy cá nhân:**
   - Terminal 1 (Backend): Mở thư mục gốc chạy `dotnet run --project ProcurementSystem.API` (Backend lắng nghe tại `http://localhost:5225`).
   - Terminal 2 (Frontend): Mở thư mục `frontend/` chạy `npm run dev` (Frontend lắng nghe tại `http://localhost:5173`).
4. **Quy tắc commit:**
   - Tuân thủ Conventional Commits: `feat(fe): ...`, `ui(fe): ...`, `fix(fe): ...`.

---

## 🎯 KẾT LUẬN & ĐẦU RA KỲ VỌNG CUỐI TUẦN 6

Khi hoàn thành kế hoạch này, nhóm sẽ sở hữu:
1. Một sản phẩm **Fullstack hoàn chỉnh từ A đến Z (.NET 8 Web API + React SPA)**.
2. Giao diện trực quan, sang trọng, hỗ trợ bấm thử nghiệm toàn trình 9 bước cực kỳ mượt mà.
3. Tài liệu thuyết minh và sản phẩm mẫu hoàn toàn vượt trội so với các đồ án thông thường, tự tin đạt điểm tối đa khi bảo vệ tốt nghiệp!
