# CẨM NANG HƯỚNG DẪN KIỂM THỬ TOÀN DIỆN NGHIỆP VỤ HỆ THỐNG
## HỆ THỐNG QUẢN LÝ ĐẤU THẦU & THEO DÕI HỢP ĐỒNG (PROCUREMENT & CONTRACT TRACKING SYSTEM)

- **Cơ sở pháp lý & Chuẩn mực:** Luật Đấu thầu số 22/2023/QH15, Nghị định số 24/2024/NĐ-CP, Tiêu chuẩn an toàn thông tin OWASP Top 10, Nguyên tắc kiểm soát nội bộ Maker - Checker.
- **Nền tảng công nghệ:** ASP.NET Core 8 Web API (.NET 8 SDK) | Entity Framework Core 8 | Microsoft SQL Server / SQLite | React 18 SPA (Vite + TailwindCSS) | Supabase Cloud Storage (S3 Protocol + AES-256 Zero-Trust).
- **Mục đích tài liệu:** Hướng dẫn từng bước (Step-by-Step Walkthrough) giúp Kiểm thử viên (Tester), Giảng viên hướng dẫn và Hội đồng nghiệm thu có thể tự mình kiểm chứng toàn bộ 10 phân hệ nghiệp vụ khép kín từ khi phát hành hồ sơ mời thầu đến giải ngân tài chính.

---

## MỤC LỤC HƯỚNG DẪN KIỂM THỬ

1. [Thông Tin Truy Cập & Khởi Động Môi Trường](#1-thông-tin-truy-cập--khởi-động-môi-trường)
2. [Bảng Danh Sách Tài Khoản & Phân Quyền Kiểm Thử](#2-bảng-danh-sách-tài-khoản--phân-quyền-kiểm-thử)
3. [Quy Trình Kiểm Thử 10 Giai Đoạn Nghiệp Vụ Vòng Đời Khép Kín](#3-quy-trình-kiểm-thử-10-giai-đoạn-nghiệp-vụ-vòng-đời-khép-kín)
   - [Giai đoạn 1: Đăng ký & Xác thực Tài khoản (Authentication & Onboarding)](#giai-đoạn-1-đăng-ký--xác-thực-tài-khoản-authentication--onboarding)
   - [Giai đoạn 2: Quản lý Bên mời thầu & Phân quyền Thành viên Nội bộ](#giai-đoạn-2-quản-lý-bên-mời-thầu--phân-quyền-thành-viên-nội-bộ)
   - [Giai đoạn 3: Khởi tạo & Phát hành Gói thầu (Bid Package Management)](#giai-đoạn-3-khởi-tạo--phát-hành-gói-thầu-bid-package-management)
   - [Giai đoạn 4: Tham gia Dự thầu & Nộp Hồ sơ (Bid Submission & Sealed-Bid)](#giai-đoạn-4-tham-gia-dự-thầu--nộp-hồ-sơ-bid-submission--sealed-bid)
   - [Giai đoạn 5: Đóng thầu & Chấm thầu Đa Giám khảo Độc lập (Evaluation & Scorecard)](#giai-đoạn-5-đóng-thầu--chấm-thầu-đa-giám-khảo-độc-lập-evaluation--scorecard)
   - [Giai đoạn 6: Trao thầu & Phân tách Trách nhiệm Maker - Checker (Award Approval)](#giai-đoạn-6-trao-thầu--phân-tách-trách-nhiệm-maker---checker-award-approval)
   - [Giai đoạn 7: Quản lý Vòng đời Hợp đồng Kinh tế (Contract Lifecycle Management)](#giai-đoạn-7-quản-lý-vòng-đời-hợp-đồng-kinh-tế-contract-lifecycle-management)
   - [Giai đoạn 8: Nghiệm thu Kỹ thuật & Phê duyệt Giải ngân Mốc thanh toán](#giai-đoạn-8-nghiệm-thu-kỹ-thuật--phê-duyệt-giải-ngân-mốc-thanh-toán)
   - [Giai đoạn 9: Quản trị Lưu trữ Tệp Hybrid Cloud & Bảo mật Zero-Trust](#giai-đoạn-9-quản-trị-lưu-trữ-tệp-hybrid-cloud--bảo-mật-zero-trust)
   - [Giai đoạn 10: Báo cáo Quản trị, Nhật ký Kiểm toán & Phòng chống Tấn công](#giai-đoạn-10-báo-cáo-quản-trị-nhật-ký-kiểm-toán--phòng-chống-tấn-công)
4. [Bảng Ma Trận Các Ca Kiểm Thử Tiêu Cực (Negative & Security Test Cases)](#4-bảng-ma-trận-các-ca-kiểm-thử-tiêu-cực-negative--security-test-cases)
5. [Lệnh Kiểm Thử Tự Động Hóa (Automated Test Suite Execution)](#5-lệnh-kiểm-thử-tự-động-hóa-automated-test-suite-execution)

---

## 1. THÔNG TIN TRUY CẬP & KHỞI ĐỘNG MÔI TRƯỜNG

### 1.1. Địa chỉ dịch vụ
- **Giao diện Web SPA (Frontend):** `http://localhost:5173`
- **Cổng giao tiếp Backend API (Swagger UI):** `http://localhost:5225/swagger`
- **Backend API Base URL:** `http://localhost:5225`

### 1.2. Cách khởi động hệ thống
- **Cách 1 (Khởi động tự động 1-Click):**
  Chạy tệp `start-all.bat` tại thư mục gốc dự án. Tệp sẽ tự động kích hoạt cả Backend ASP.NET Core và Frontend Vite React.
- **Cách 2 (Khởi động thủ công qua Terminal):**
  - Mở Terminal 1 (Backend):
    ```powershell
    cd "d:\Thuc Tap\DuAnTT\ProcurementSystem.API"
    dotnet run
    ```
  - Mở Terminal 2 (Frontend):
    ```powershell
    cd "d:\Thuc Tap\DuAnTT\frontend"
    npm run dev
    ```

---

## 2. BẢNG DANH SÁCH TÀI KHOẢN & PHÂN QUYỀN KIỂM THỬ

> 🔑 **MẬT KHẨU CHUNG CHO TẤT CẢ TÀI KHOẢN:** `Admin@123`

| STT | Họ và Tên | Email Đăng Nhập | Vai Trò (Role) | Chức Danh / Đơn Vị Phụ Trách | Quyền Hạn Nghiệp Vụ Cốt Lõi |
|:---:|---|---|:---:|---|---|
| **1** | System Administrator | `admin@procurement.com` | **Admin** | Quản trị viên Toàn hệ thống | • Quản lý toàn bộ người dùng, duyệt/khóa tài khoản.<br>• Thẩm định hồ sơ pháp lý Nhà thầu & Bên mời thầu.<br>• Phê duyệt đề xuất cấp tài khoản Giám khảo chấm thầu.<br>• Nhập/Xuất Excel nhà thầu, quản trị lưu trữ Cloud. |
| **2** | Nguyễn Thị Mai | `procurement@procurement.com` | **Procurement** | Chuyên viên Bên mời thầu (Ban QLDA Đầu tư Xây dựng) | • Tạo lập gói thầu, cấu hình tiêu chí chấm điểm.<br>• Thành lập Tổ chuyên gia (tối thiểu 3 giám khảo lẻ).<br>• Đóng thầu, gia hạn thầu, mở lại gói thầu.<br>• Phê duyệt trao thầu (Checker), soạn thảo hợp đồng. |
| **3** | Trần Văn Giám Khảo | `evaluator@procurement.com` | **Evaluator** | Trưởng Tổ chuyên gia Chấm thầu | • Chấm điểm độc lập ma trận Scorecard hồ sơ dự thầu.<br>• Xem điểm đánh giá độc lập của chính mình.<br>• Xem bảng xếp hạng sau khi hoàn tất chấm điểm.<br>• **Bị khóa quyền phê duyệt trao thầu** (Anti-Self Approval). |
| **4** | Lê Thị Kỹ Thuật | `evaluator2@procurement.com` | **Evaluator** | Thành viên Tổ chuyên gia (Kỹ thuật) | • Chấm điểm độc lập các tiêu chí năng lực kỹ thuật.<br>• Không thể xem điểm của các giám khảo khác khi đang chấm. |
| **5** | Phạm Minh Tài | `evaluator3@procurement.com` | **Evaluator** | Thành viên Tổ chuyên gia (Tài chính) | • Chấm điểm độc lập tiêu chí tài chính, thương mại.<br>• Bảo đảm Tổ chuyên gia đủ số lượng lẻ $\ge 3$ người. |
| **6** | Nguyễn Văn Nhà Thầu | `contractor1@test.com` | **Contractor** | CÔNG TY CP CÔNG NGHỆ TEST (MST: `0300588569`) | • Nộp hồ sơ dự thầu điện tử (E-HSDT), rút hồ sơ.<br>• Xem kết quả trúng thầu, ký hợp đồng điện tử.<br>• Báo cáo tiến độ các mốc thanh toán nghiệm thu. |
| **7** | Trần Văn Phần Mềm | `contractor2@test.com` | **Contractor** | CÔNG TY CP PHẦN MỀM ABC (MST: `0102030405`) | • Nộp hồ sơ dự thầu cạnh tranh, tải báo giá tài chính.<br>• Kiểm tra cơ chế xếp hạng Quán quân / Á quân. |

---

## 3. QUY TRÌNH KIỂM THỬ 10 GIAI ĐOẠN NGHIỆP VỤ VÒNG ĐỜI KHÉP KÍN

---

### GIAI ĐOẠN 1: ĐĂNG KÝ & XÁC THỰC TÀI KHOẢN (AUTHENTICATION & ONBOARDING)

#### Mục tiêu kiểm thử:
Xác minh tính hợp lệ của quy trình tự phục vụ (Self-Registration), bắt buộc xác thực mã OTP email 6 chữ số, tra cứu tự động thông tin doanh nghiệp qua Cổng Quốc gia (VietQR API), và cơ chế phê duyệt hồ sơ năng lực của Quản trị viên.

#### Kịch bản 1.1: Đăng ký tài khoản Nhà thầu mới (Contractor Self-Signup)
1. **Truy cập:** Mở trình duyệt tại `http://localhost:5173/login`, bấm **"Đăng ký tài khoản mới"** hoặc truy cập trực tiếp `http://localhost:5173/register`.
2. **Chọn vai trò:** Chọn tab **"Nhà thầu / Doanh nghiệp"**.
3. **Thao tác kiểm thử:**
   - **Bước 1:** Nhập Mã số thuế hợp lệ (Ví dụ: `0101248141`). Bấm icon **Kính lúp** $\rightarrow$ Hệ thống tự động gọi API Quốc gia và điền sẵn Tên công ty (*CÔNG TY TNHH ĐẦU TƯ VÀ PHÁT TRIỂN CÔNG NGHỆ VIỆT*) và Địa chỉ.
   - **Bước 2:** Nhập Email: `doanhnghiep.test@gmail.com`.
   - **Bước 3 (Kiểm tra OTP):** Bấm nút **"Gửi mã OTP"**. Hệ thống hiển thị đồng hồ đếm ngược Cooldown 60 giây và gửi mã 6 chữ số qua email.
   - **Bước 4 (Thử nghiệm tính năng chống bypass đổi email):** Thử đổi email sang `khac@gmail.com` $\rightarrow$ Quan sát ô OTP lập tức bị xóa rỗng và trạng thái xác thực bị hủy. Trả lại email ban đầu và bấm gửi OTP.
   - **Bước 5:** Nhập mã OTP 6 số hợp lệ (Ví dụ lấy từ hộp thư hoặc môi trường test).
   - **Bước 6:** Tải lên tệp Giấy phép kinh doanh (.pdf hoặc ảnh scan .png, .jpg).
   - **Bước 7:** Bấm **"Đăng ký tài khoản"**.
4. **Kết quả mong đợi:**
   - Màn hình hiển thị thông báo Toast xanh: *"Đăng ký thành công! Hồ sơ của bạn đang được xét duyệt."*
   - Tài khoản được lưu vào cơ sở dữ liệu với trạng thái `VerificationStatus = "Pending"`. Nhà thầu chưa thể nộp hồ sơ thầu ngay lúc này (đáp ứng Quy tắc N10).

#### Kịch bản 1.2: Quản trị viên thẩm định & Phê duyệt hồ sơ Nhà thầu
1. **Đăng nhập Admin:** Đăng nhập bằng tài khoản `admin@procurement.com` / `Admin@123`.
2. **Truy cập:** Vào menu **"Quản lý Nhà thầu"** (`http://localhost:5173/contractors`).
3. **Thao tác:**
   - Tìm kiếm nhà thầu vừa đăng ký (trạng thái hiển thị huy hiệu vàng: *Chờ duyệt - Pending*).
   - Bấm xem chi tiết hồ sơ $\rightarrow$ Bấm mở xem tệp Giấy phép kinh doanh đính kèm.
   - Bấm nút **"Phê duyệt hồ sơ" (Verify)**.
4. **Kết quả mong đợi:**
   - Trạng thái nhà thầu chuyển sang xanh: *Đã xác minh - Approved*.
   - Nhà thầu chính thức đủ tư cách pháp lý tham gia dự thầu trên toàn hệ thống.

---

### GIAI ĐOẠN 2: QUẢN LÝ BÊN MỜI THẦU & PHÂN QUYỀN THÀNH VIÊN NỘI BỘ

#### Mục tiêu kiểm thử:
Kiểm chứng mô hình quản trị Bên mời thầu đa thành viên, quy trình gửi thư mời tham gia tổ chức, phân bổ các vai trò nội bộ (`Manager`, `Officer`, `Finance`, `Signer`), và cấp phát ủy quyền ký số (`SigningAuthority`).

#### Kịch bản 2.1: Quản trị viên Đơn vị mời thầu cấp phát tài khoản thành viên
1. **Đăng nhập:** Đăng nhập tài khoản Bên mời thầu `procurement@procurement.com` / `Admin@123`.
2. **Truy cập:** Menu **"Tổ chức & Thành viên"** (`http://localhost:5173/members`).
3. **Thao tác:**
   - Bấm nút **"Mời thành viên mới"**.
   - Điền Email: `chuyenvien.moithau@donvi.gov.vn`, Họ tên: `Lê Văn Cán Bộ`, Vai trò trong đơn vị: `Officer` (Cán bộ phụ trách gói thầu).
   - Bấm **"Gửi thư mời"**.
4. **Kết quả mong đợi:**
   - Hệ thống tạo lời mời với mã Token bảo mật có thời hạn 48 giờ.
   - Người được mời nhận email chứa liên kết kích hoạt `/accept-invitation?token=...`.
   - Khi kích hoạt, thành viên tạo mật khẩu mới và được liên kết chặt chẽ vào Bên mời thầu với trạng thái `Active`.

#### Kịch bản 2.2: Phân quyền Ủy quyền Ký duyệt chi (Signing Authority)
1. **Thao tác:** Tại trang chi tiết thành viên, chọn thành viên có chức danh Lãnh đạo/Kế toán trưởng.
2. Bấm **"Cấp Quyền Ký Phê Duyệt"** (`DisbursementApproval`):
   - Số quyết định ủy quyền: `QD-UQ-2026/01`.
   - Căn cứ pháp lý: `Luật Đấu thầu 2023 và Quy chế tài chính cơ quan`.
   - Phạm vi: `Toàn đơn vị (EntityWide)`.
   - Thời hạn hiệu lực: Từ ngày hiện tại đến `31/12/2026`.
3. Bấm **"Xác nhận cấp quyền"**.
4. **Kết quả mong đợi:** Thành viên mang vai trò `Signer` được cấp thẩm quyền ký giải ngân hợp pháp, phục vụ cho quy trình Maker - Checker ở Giai đoạn 8.

---

### GIAI ĐOẠN 3: KHỞI TẠO & PHÁT HÀNH GÓI THẦU (BID PACKAGE MANAGEMENT)

#### Mục tiêu kiểm thử:
Xác minh việc gắn cứng `ProcuringEntityId` cố định khi tạo gói thầu, thiết lập tiêu chí kỹ thuật + giá tự động, và bắt buộc Tổ chuyên gia phải có số lượng lẻ $\ge 3$ thành viên.

#### Kịch bản 3.1: Tạo gói thầu và Thiết lập tiêu chí đánh giá
1. **Đăng nhập:** Tài khoản Bên mời thầu `procurement@procurement.com`.
2. **Truy cập:** Menu **"Gói thầu"** $\rightarrow$ Bấm nút **"Tạo gói thầu mới"** (`http://localhost:5173/packages`).
3. **Điền thông tin:**
   - Mã gói thầu: `PKG-2026-XAYLAP-01`.
   - Tên gói thầu: *Gói thầu Xây dựng Hệ thống Nền tảng Số và Trung tâm Dữ liệu 2026*.
   - Dự toán ngân sách: `5.000.000.000` VNĐ (5 tỷ đồng).
   - Hình thức đấu thầu: `Đấu thầu rộng rãi trong nước qua mạng`.
   - Phương thức lựa chọn: `Một giai đoạn một túi hồ sơ`.
   - Thời điểm đóng thầu (Deadline): Chọn thời điểm cách ngày hiện tại `15 ngày`.
   - Tệp Hồ sơ mời thầu (HSMT): Tải lên tệp `HSMT_Chuan.pdf`.
4. **Cấu hình tiêu chí chấm điểm (Evaluation Criteria):**
   - Tiêu chí 1 (Kỹ thuật): *Năng lực giải pháp công nghệ và thiết bị* (Trọng số 40%, Chấm điểm thủ công 0-100).
   - Tiêu chí 2 (Kỹ thuật): *Kinh nghiệm thực hiện hợp đồng tương tự* (Trọng số 30%, Chấm điểm thủ công 0-100).
   - Tiêu chí 3 (Tài chính - Tự động): *Giá dự thầu hợp lý* (Trọng số 30%, gạt bật **"Tiêu chí Giá tự động - IsPriceCriteria"**).
     *(Lưu ý: Hệ thống chỉ cho phép duy nhất 1 tiêu chí giá tự động theo Quy tắc N5).*
5. **Thành lập Tổ chuyên gia chấm thầu (Evaluators Selection):**
   - Phân công Giám khảo 1: `evaluator@procurement.com` (Trưởng tổ).
   - Phân công Giám khảo 2: `evaluator2@procurement.com` (Thành viên kỹ thuật).
   - Phân công Giám khảo 3: `evaluator3@procurement.com` (Thành viên tài chính).
   *(Lưu ý: Nếu chỉ chọn 2 giám khảo, hệ thống sẽ báo lỗi ngăn chặn theo Quy tắc N4 - Bắt buộc số lẻ $\ge 3$).*
6. **Bấm "Phát hành gói thầu":**
7. **Kết quả mong đợi:**
   - Gói thầu được tạo thành công với trạng thái **Đang mở thầu (Open)**.
   - Gói thầu hiển thị công khai trên danh sách để các nhà thầu vào nộp hồ sơ.

---

### GIAI ĐOẠN 4: THAM GIA DỰ THẦU & NỘP HỒ SƠ (BID SUBMISSION & SEALED-BID)

#### Mục tiêu kiểm thử:
Kiểm chứng luồng nộp E-HSDT, tính bảo mật niêm phong giá (Sealed-Bid) và quyền rút/nộp lại hồ sơ trước hạn đóng thầu.

#### Kịch bản 4.1: Nhà thầu 1 nộp hồ sơ dự thầu (E-HSDT)
1. **Đăng nhập:** Tài khoản Nhà thầu 1 `contractor1@test.com` / `Admin@123`.
2. **Truy cập:** Menu **"Tìm kiếm Gói thầu"** $\rightarrow$ Tìm gói `PKG-2026-XAYLAP-01`.
3. **Thao tác:** Bấm nút **"Nộp hồ sơ dự thầu"**.
   - Giá dự thầu: `4.500.000.000` VNĐ (4.5 tỷ đồng).
   - Thời gian thực hiện: `180 ngày`.
   - Tệp Đề xuất kỹ thuật: Tải file `DeXuatKyThuat_NhaThau1.pdf`.
   - Tệp Đề xuất tài chính: Tải file `BaoGiaChiTiet_NhaThau1.pdf`.
   - Tệp Bảo lãnh dự thầu: Tải file `BaoLanhDuThau_NganHang.pdf`.
4. Bấm **"Xác nhận Nộp thầu"**.
5. **Kết quả:** Hồ sơ ghi nhận trạng thái `Submitted`.

#### Kịch bản 4.2: Nhà thầu 2 nộp hồ sơ dự thầu cạnh tranh
1. **Đăng nhập:** Tài khoản Nhà thầu 2 `contractor2@test.com` / `Admin@123`.
2. **Truy cập gói thầu:** `PKG-2026-XAYLAP-01` $\rightarrow$ Bấm **"Nộp hồ sơ dự thầu"**.
   - Giá dự thầu: `4.200.000.000` VNĐ (4.2 tỷ đồng - giá thấp hơn Nhà thầu 1).
   - Tải lên đầy đủ các tài liệu đề xuất kỹ thuật và bảo lãnh.
3. Bấm **"Xác nhận Nộp thầu"**.
4. **Kiểm tra cơ chế Niêm phong hồ sơ (Sealed-Bid):**
   - Đăng nhập tài khoản Bên mời thầu hoặc Giám khảo: Mở xem danh sách gói thầu `PKG-2026-XAYLAP-01`.
   - **Xác nhận:** Giá dự thầu và tệp tài chính của các nhà thầu hoàn toàn bị niêm phong bí mật, điểm số và xếp hạng hiển thị `null` (Tuân thủ Quy tắc N2 & N21).

---

### GIAI ĐOẠN 5: ĐÓNG THẦU & CHẤM THẦU ĐA GIÁM KHẢO ĐỘC LẬP (EVALUATION & SCORECARD)

#### Mục tiêu kiểm thử:
Kiểm chứng tính năng đóng thầu, chuyển sang trạng thái Đang đánh giá (`Evaluating`), tính độc lập tuyệt đối giữa các giám khảo và công thức tính điểm giá tự động.

#### Kịch bản 5.1: Đóng thầu & Mở phiên chấm điểm
1. **Đăng nhập:** Tài khoản Bên mời thầu `procurement@procurement.com`.
2. **Thao tác:** Tại trang chi tiết gói thầu `PKG-2026-XAYLAP-01`, bấm nút **"Đóng thầu & Bắt đầu Đánh giá"** (hoặc đợi worker tự động đóng khi hết hạn).
3. **Kết quả:** Trạng thái gói thầu chuyển sang màu vàng: **Đang đánh giá (Evaluating)**.

#### Kịch bản 5.2: Giám khảo 1 (`evaluator@procurement.com`) chấm điểm
1. **Đăng nhập:** `evaluator@procurement.com` / `Admin@123`.
2. **Truy cập:** Menu **"Trung tâm Thẩm định"** (`http://localhost:5173/evaluation`) $\rightarrow$ Chọn gói thầu `PKG-2026-XAYLAP-01`.
3. **Thao tác chấm điểm:**
   - **Hồ sơ Nhà thầu 1:**
     + Tiêu chí 1 (Giải pháp công nghệ): Nhập `90` điểm.
     + Tiêu chí 2 (Kinh nghiệm): Nhập `85` điểm.
     + Nhập nhận xét: *"Giải pháp kiến trúc tốt, thiết bị chính hãng."*
     + Bấm **"Lưu phiếu chấm điểm"** $\rightarrow$ Toast xanh báo lưu thành công.
   - **Hồ sơ Nhà thầu 2:**
     + Tiêu chí 1: Nhập `75` điểm.
     + Tiêu chí 2: Nhập `80` điểm.
     + Nhập nhận xét: *"Giải pháp đạt yêu cầu, tiến độ chấp nhận được."*
     + Bấm **"Lưu phiếu chấm điểm"**.
4. **Xác nhận nguyên tắc Niêm phong điểm (Sealed Scoring):**
   - Bấm sang tab "Bảng xếp hạng & Trao thầu": Giám khảo 1 chỉ thấy tiến độ của mình, tuyệt đối không nhìn thấy điểm số của Giám khảo 2 hay Giám khảo 3 (Tuân thủ Quy tắc N14).

#### Kịch bản 5.3: Giám khảo 2 & Giám khảo 3 hoàn tất chấm điểm
1. Lần lượt đăng nhập bằng `evaluator2@procurement.com` và `evaluator3@procurement.com`.
2. Chấm điểm cho cả 2 nhà thầu và bấm Lưu phiếu chấm điểm.
3. **Kết quả tính điểm Giá tự động:**
   - Hệ thống tự động tính điểm tiêu chí Giá: Nhà thầu 2 chào giá `4.2 tỷ` (thấp nhất) đạt điểm tối đa `100` điểm cho tiêu chí giá; Nhà thầu 1 chào giá `4.5 tỷ` đạt điểm tỷ lệ theo công thức luật định.
   - Điểm tổng hợp bằng trung bình trọng số của 3 giám khảo kết hợp điểm giá tự động.

---

### GIAI ĐOẠN 6: TRAO THẦU & PHÂN TÁCH TRÁCH NHIỆM MAKER - CHECKER (AWARD APPROVAL)

#### Mục tiêu kiểm thử:
Kiểm chứng nguyên tắc phòng vệ ngăn ngừa tự phê duyệt (Anti-Self Approval): Giám khảo chấm điểm (Maker) bị khóa quyền phê duyệt; Bên mời thầu (Checker) kiểm tra bục vinh danh và phê duyệt trúng thầu; hệ thống tự động khóa bảng điểm và xuất Biên bản Đánh giá HSDT chính thức theo Nghị định 24/2024/NĐ-CP.

#### Kịch bản 6.1: Kiểm tra Giám khảo (Maker) bị chặn quyền phê duyệt trao thầu
1. **Đăng nhập:** Giám khảo `evaluator@procurement.com`.
2. **Truy cập:** Gói thầu `PKG-2026-XAYLAP-01` $\rightarrow$ Tab **"Bảng xếp hạng & Trao thầu"**.
3. **Quan sát:**
   - Tiến độ chấm điểm hiển thị: *Hoàn thành 100% (3/3 giám khảo)*.
   - **Xác nhận kiểm thử:** Trên toàn bộ màn hình của Giám khảo **KHÔNG HỀ XUẤT HIỆN** nút "Phê duyệt Trao thầu" (Số lượng nút = 0). Giám khảo không thể tự mình quyết định đơn vị trúng thầu.

#### Kịch bản 6.2: Bên mời thầu (Checker) đối soát và Phê duyệt trúng thầu
1. **Đăng nhập:** Bên mời thầu `procurement@procurement.com`.
2. **Truy cập:** Gói thầu `PKG-2026-XAYLAP-01` $\rightarrow$ Tab **"Bảng xếp hạng & Trao thầu"**.
3. **Quan sát giao diện:**
   - Khối **"Trung Tâm Phê Duyệt Trao Thầu"** hiển thị nổi bật với nút màu cam vàng: **"Phê duyệt Trao thầu"**.
   - Phía dưới hiển thị **Bục Vinh Danh Xếp Hạng Nhà Thầu**:
     + **Vị trí Quán quân (Hạng 1):** Thuộc về nhà thầu có điểm tổng hợp cao nhất (Ví dụ: Nhà thầu 1 với điểm kỹ thuật vượt trội).
     + **Vị trí Á quân (Hạng 2):** Thuộc về nhà thầu tiếp theo.
4. **Thao tác phê duyệt:**
   - Bấm nút **"Phê duyệt Trao thầu"** $\rightarrow$ Hộp thoại Modal mở ra hiển thị tóm tắt Quán quân Hạng 1.
   - Nhập Căn cứ pháp lý: *"Căn cứ Biên bản đánh giá E-HSDT của Tổ chuyên gia, Nhà thầu 1 đạt điểm kỹ thuật và tổng hợp cao nhất theo Quyết định số 105/QĐ-BQL."*
   - Bấm **"Xác nhận Trao thầu"**.
5. **Kết quả mong đợi:**
   - Trạng thái gói thầu chuyển sang: **Đã trao thầu (Awarded)**.
   - Xuất hiện Banner bảo vệ màu xanh ngọc: **"Bảng điểm đã khóa"** (Nghiêm cấm mọi hành vi sửa điểm hồi tố).
   - Nút **"Xuất Biên bản Đánh giá (Chính thức)"** xuất hiện ở góc trên bên phải trang web.

#### Kịch bản 6.3: Xuất Biên bản Đánh giá HSDT theo Nghị định số 24/2024/NĐ-CP
1. Bấm nút **"Xuất Biên bản Đánh giá (Chính thức)"**.
2. **Quan sát:** Hộp thoại toàn màn hình mở ra hiển thị văn bản hành chính khổ A4 chuẩn mực:
   - Quốc hiệu, Tiêu ngữ, Tên Bên mời thầu.
   - Tiêu đề in hoa đậm: **BÁO CÁO ĐÁNH GIÁ HỒ SƠ DỰ THẦU**.
   - Bảng điểm chi tiết của từng giám khảo, xếp hạng nhà thầu, kiến nghị trao thầu.
   - Thanh công cụ hỗ trợ: Phóng to, Thu nhỏ, Tải về file `.doc` và nút In trực tiếp ra máy in/PDF.

---

### GIAI ĐOẠN 7: QUẢN LÝ VÒNG ĐỜI HỢP ĐỒNG KINH TẾ (CONTRACT LIFECYCLE MANAGEMENT)

#### Mục tiêu kiểm thử:
Kiểm chứng tự động tạo hợp đồng trạng thái `Draft` từ gói thầu đã trao thầu, ràng buộc phân bổ 100% ngân sách mốc thanh toán, kiểm tra bảo lãnh thực hiện hợp đồng và kích hoạt hợp đồng sang `Active`.

#### Kịch bản 7.1: Tạo hợp đồng & Phân bổ mốc thanh toán nghiệm thu
1. **Đăng nhập:** Bên mời thầu `procurement@procurement.com`.
2. **Truy cập:** Menu **"Hợp đồng"** (`http://localhost:5173/contracts`).
3. **Thao tác:** Bấm nút **"Lập hợp đồng mới"**:
   - Chọn Gói thầu: `PKG-2026-XAYLAP-01` $\rightarrow$ Hệ thống tự động điền Nhà thầu trúng thầu và Giá trị hợp đồng (`4.500.000.000` VNĐ).
   - Số hợp đồng: `HD-2026/XAYLAP-01`.
   - Thời hạn: Từ ngày hiện tại đến `180 ngày tiếp theo`.
   - Bấm **"Tạo hợp đồng"** $\rightarrow$ Hợp đồng tạo ở trạng thái **Nháp (Draft)**.
4. **Phân bổ các mốc thanh toán (Milestones):**
   - Mở chi tiết hợp đồng vừa tạo.
   - Thêm Mốc 1: *Thi công móng và hạ tầng kỹ thuật ngầm* - Số tiền: `1.500.000.000` VNĐ (33.33%).
   - Thêm Mốc 2: *Lắp đặt thiết bị máy chủ và mạng lõi* - Số tiền: `2.000.000.000` VNĐ (44.44%).
   - Thêm Mốc 3: *Nghiệm thu bàn giao đưa vào vận hành* - Số tiền: `1.000.000.000` VNĐ (22.23%).
   - **Xác nhận ràng buộc ngân sách (Quy tắc N9):** Tổng 3 mốc = `4.500.000.000` VNĐ đúng bằng 100% giá trị hợp đồng. Nếu thử thêm mốc vượt quá tổng giá trị, hệ thống lập tức báo lỗi chặn.

#### Kịch bản 7.2: Tải lên Bảo lãnh thực hiện hợp đồng & Kích hoạt hợp đồng
1. **Thêm Bảo lãnh hợp đồng (Contract Guarantee):**
   - Loại bảo lãnh: `Bảo lãnh thực hiện hợp đồng`.
   - Số bảo lãnh: `BL-BIDV-2026/888`.
   - Ngân hàng phát hành: `Ngân hàng TMCP Đầu tư và Phát triển Việt Nam (BIDV)`.
   - Giá trị bảo lãnh: `225.000.000` VNĐ (5% giá trị hợp đồng).
   - Tải lên bản scan bảo lãnh có dấu đỏ ngân hàng (.pdf).
2. **Tải lên bản Scan hợp đồng ký kết:**
   - Bấm tải lên bản scan hợp đồng đã ký và đóng con dấu pháp nhân giữa hai bên: `HopDong_KyKet_DauDo.pdf`.
3. **Kích hoạt hợp đồng (Activate):**
   - Bấm nút **"Kích hoạt Hợp đồng"**.
4. **Kết quả mong đợi:**
   - Trạng thái hợp đồng chuyển từ `Draft` sang xanh lá: **Đang thực hiện (Active)**.

---

### GIAI ĐOẠN 8: NGHIỆM THU KỸ THUẬT & PHÊ DUYỆT GIẢI NGÂN MỐC THANH TOÁN

#### Mục tiêu kiểm thử:
Kiểm chứng quy trình phân tách trách nhiệm Maker - Checker trong giải ngân vốn: Cán bộ kỹ thuật kiểm tra hiện trường lập biên bản nghiệm thu (Maker), lưu snapshot tài liệu kèm mã băm SHA-256; Người có thẩm quyền ký độc lập (Checker) đối soát và phê duyệt giải ngân; hệ thống tự động phát hiện nếu tệp biên bản bị sửa đổi trái phép (lỗi 409 Conflict).

#### Kịch bản 8.1: Nhà thầu báo cáo hoàn thành mốc
1. **Đăng nhập:** Nhà thầu `contractor1@test.com`.
2. **Truy cập:** Mở chi tiết hợp đồng `HD-2026/XAYLAP-01` $\rightarrow$ Chọn Mốc 1.
3. **Thao tác:** Bấm **"Báo cáo hoàn thành mốc"**, nhập nội dung hoàn thành và đính kèm biên bản kiểm tra nội bộ.
4. **Kết quả:** Mốc chuyển trạng thái sang **Chờ nghiệm thu (Pending Acceptance)**.

#### Kịch bản 8.2: Cán bộ Kỹ thuật (Maker) Nghiệm thu kỹ thuật hiện trường
1. **Đăng nhập:** Cán bộ kỹ thuật Bên mời thầu `procurement@procurement.com` (vai trò Officer).
2. **Truy cập:** Mở Mốc 1 của hợp đồng.
3. **Thao tác:** Bấm nút **"Nghiệm thu Kỹ thuật"**:
   - Tải lên tệp Biên bản nghiệm thu kỹ thuật hiện trường có chữ ký giám sát: `BienBan_NghiemThu_Moc1.pdf`.
   - Tích chọn: **"Đạt tiêu chuẩn kỹ thuật chất lượng (Passed)"**.
   - Nhập nhận xét: *"Khối lượng thi công đạt 100%, đúng quy chuẩn kỹ thuật thiết kế."*
   - Bấm **"Lưu kết quả Nghiệm thu Kỹ thuật"**.
4. **Kết quả mong đợi:**
   - Mốc 1 chuyển sang trạng thái: **Đã đạt nghiệm thu kỹ thuật (TechnicalApproved)**.
   - Hệ thống tự động tạo một bản sao Snapshot của tài liệu và tính toán mã băm SHA-256 nguyên bản lưu vào cơ sở dữ liệu.
   - **Xác nhận Anti-Self Approval:** Cán bộ kỹ thuật **KHÔNG THỂ** tự duyệt giải ngân tiền (nút "Phê duyệt Giải ngân" hiển thị thông báo cần Người có thẩm quyền ký độc lập).

#### Kịch bản 8.3: Người có Thẩm quyền ký (Checker) Phê duyệt giải ngân tiền
1. **Đăng nhập:** Người có thẩm quyền ký `evaluator@procurement.com` hoặc Giám đốc/Kế toán trưởng mang quyền `Signer` (đã được cấp `SigningAuthority` ở Giai đoạn 2).
2. **Truy cập:** Mở Mốc 1 của hợp đồng `HD-2026/XAYLAP-01`.
3. **Thao tác:**
   - Hệ thống tự động kiểm tra đối soát tính toàn vẹn mã băm SHA-256 của tệp snapshot tài liệu.
   - Bấm nút **"Phê duyệt Giải ngân Mốc thanh toán" (Approve Disbursement)**.
   - Nhập số tiền giải ngân: `1.500.000.000` VNĐ.
   - Bấm **"Xác nhận Duyệt chi"**.
4. **Kết quả mong đợi:**
   - Trạng thái Mốc 1 chuyển sang: **Đã giải ngân (Disbursed)**.
   - Tiến độ giải ngân của hợp đồng tăng lên: `1.500.000.000 / 4.500.000.000 VNĐ (33.33%)`.
   - Nhật ký kiểm toán `AuditLog` ghi nhận đầy đủ sự kiện duyệt chi với chữ ký của Người có thẩm quyền.

---

### GIAI ĐOẠN 9: QUẢN TRỊ LƯU TRỮ TỆP HYBRID CLOUD & BẢO MẬT ZERO-TRUST

#### Mục tiêu kiểm thử:
Kiểm chứng khả năng vận hành của kiến trúc lưu trữ tệp đám mây lai (Hybrid Storage), tính năng nén GZip kết hợp mã hóa AES-256 Zero-Trust, tải tệp an toàn không bị lỗi 500, và cơ chế 1-Click Rollback hoàn tác CSDL.

#### Kịch bản 9.1: Kiểm tra trạng thái lưu trữ & Đồng bộ lên Supabase Cloud
1. **Đăng nhập:** Quản trị viên `admin@procurement.com`.
2. **Truy cập API Swagger:** `http://localhost:5225/swagger` $\rightarrow$ Nhóm API `StorageAdmin`.
3. **Kiểm tra trạng thái:** Gọi `GET /api/admin/storage/status` $\rightarrow$ Xem thông số dung lượng thư mục `uploads/` trên máy cục bộ và nhà cung cấp lưu trữ hiện tại.
4. **Đồng bộ hóa 1-Click lên Cloud:**
   - Gọi API `POST /api/admin/storage/sync-to-cloud`.
   - **Quan sát kết quả trả về:**
     + Bảng sao lưu tự động được tạo: `_Backup_FilePaths_YYYYMMDD_HHMMSS`.
     + Các tệp tài liệu trong `uploads/` được nén GZip và mã hóa AES-256-CBC với IV ngẫu nhiên trước khi đẩy lên Supabase Bucket.
     + Các đường dẫn trong CSDL (bao gồm cả bảng `Acceptances` cột `SnapshotDocumentPath`) tự động chuyển thành định dạng `supabase://...`.

#### Kịch bản 9.2: Kiểm chứng Tải tệp bảo mật không bị Crash 500
1. Thao tác tải về 6 loại tệp tài liệu sau khi đã đồng bộ Cloud:
   - Tải tệp scan Hợp đồng (`GET /api/contracts/{id}/download`).
   - Tải tệp Bảo lãnh hợp đồng (`GET /api/contracts/{id}/guarantees/{gId}/download`).
   - Tải tệp Phụ lục hợp đồng (`GET /api/contracts/{id}/addenda/{aId}/download`).
   - Tải tệp Snapshot biên bản nghiệm thu (`GET /api/contracts/milestones/{mId}/acceptances/{aId}/document/download`).
   - Tải tệp Chứng chỉ giám khảo (`GET /api/evaluator-proposals/{id}/certificate/download`).
   - Tải tệp Quyết định thành lập / bổ nhiệm Bên mời thầu (`GET /api/procuring-entities/{id}/establishment-file`).
2. **Kết quả mong đợi:** Toàn bộ 6 API hoạt động mượt mà qua cơ chế `ResolveDownloadAsync`, giải mã và giải nén trong suốt, trả về file PDF chuẩn xác 100%, không bị văng lỗi `HTTP 500` hay lỗi `HTTP 409`.

#### Kịch bản 9.3: Kiểm chứng Hoàn tác 1-Click Rollback về Local Storage
1. Tại Swagger, gọi API `POST /api/admin/storage/rollback`.
2. **Kết quả:** Hệ thống đọc bảng sao lưu gần nhất và khôi phục lại 100% đường dẫn trong CSDL về `/uploads/...` cục bộ chỉ trong 1-2 giây mà không mất mát dữ liệu.

---

### GIAI ĐOẠN 10: BÁO CÁO QUẢN TRỊ, NHẬT KÝ KIỂM TOÁN & PHÒNG CHỐNG TẤN CÔNG

#### Mục tiêu kiểm thử:
Kiểm chứng Dashboard thống kê quản trị đa chỉ số, tính năng xuất báo cáo Excel chống tấn công Formula Injection (CWE-1236), và hệ thống ghi vết nhật ký kiểm toán độc lập.

#### Kịch bản 10.1: Xem Dashboard Báo cáo Điều hành & KPI Mua sắm
1. **Đăng nhập:** `admin@procurement.com` hoặc `procurement@procurement.com`.
2. **Truy cập:** Menu **"Báo cáo & Thống kê"** (`http://localhost:5173/reports`).
3. **Kiểm tra các chỉ số hiển thị:**
   - Tổng số gói thầu, tỷ lệ tiết kiệm ngân sách qua đấu thầu cạnh tranh.
   - Tỷ lệ hoàn thành đúng hạn của các mốc hợp đồng.
   - Biểu đồ phân bổ giá trị giải ngân theo quý/năm.

#### Kịch bản 10.2: Xuất dữ liệu ra Excel & Kiểm chứng Chống Formula Injection
1. Tại trang Quản lý Nhà thầu hoặc Báo cáo Hợp đồng, bấm nút **"Xuất Excel (.xlsx)"**.
2. Mở file Excel tải về:
   - Dữ liệu hiển thị bảng biểu ngay ngắn, chuẩn định dạng tiếng Việt Unicode.
   - **Xác minh an toàn (Quy tắc N11):** Các ô dữ liệu chứa ký tự đặc biệt như `=`, `+`, `-`, `@` đều được tự động tiền tố dấu nháy đơn `'` để vô hiệu hóa mã độc Macro Excel.

#### Kịch bản 10.3: Kiểm tra Nhật ký Kiểm toán (Audit Logs)
1. **Đăng nhập Admin:** Vào menu **"Nhật ký Hệ thống"** (`http://localhost:5173/audit-logs`).
2. **Quan sát:** Mọi thao tác nhạy cảm (Đăng nhập, Tạo gói thầu, Chấm điểm, Trao thầu, Phê duyệt giải ngân) đều được ghi nhận chi tiết: Thời gian UTC, User ID, Địa chỉ IP Client thực tế (đã qua bộ lọc chống IP Spoofing), và dữ liệu trước/sau thay đổi.

---

## 4. BẢNG MA TRẬN CÁC CA KIỂM THỬ TIÊU CỰC (NEGATIVE & SECURITY TEST CASES)

Dưới đây là danh mục các tình huống thử thách tính bền vững và bảo mật của hệ thống mà Tester nên kiểm thử:

| Mã Ca Test | Tên Tình Huống Kiểm Thử Tiêu Cực | Thao Tác Thực Hiện | Kết Quả Mong Đợi (Hệ Thống Phải Chặn) |
|:---:|---|---|---|
| **SEC-01** | Bỏ qua OTP khi Đăng ký | Gửi request đăng ký Nhà thầu/Bên mời thầu với `OtpCode = null` hoặc để trống | Bị từ chối với mã lỗi `HTTP 400 Bad Request`. Hoàn toàn không tạo User hay lưu tệp rác. |
| **SEC-02** | Nhập sai OTP quá 5 lần | Nhập mã OTP sai liên tiếp 5 lần | Mã OTP bị hủy lập tức khỏi bộ nhớ Cache. Yêu cầu người dùng phải xin mã mới. |
| **SEC-03** | Khóa Cooldown gửi OTP | Bấm gửi OTP liên tục nhiều lần trong vòng 60 giây | Hệ thống trả về thông báo: *"Vui lòng chờ X giây trước khi yêu cầu mã OTP mới."* |
| **SEC-04** | Tấn công Đổi Email sau khi nhận OTP | Lấy OTP cho `a@gmail.com`, sau đó sửa form gửi đăng ký cho `b@gmail.com` | Bị từ chối do lệch Purpose/Email giữa mã OTP và dữ liệu đăng ký. |
| **SEC-05** | Lỗ hổng IDOR Tải Hồ sơ Dự thầu | Nhà thầu A cố tình gọi API tải tệp HSDT của Nhà thầu B | Trả về mã lỗi `HTTP 403 Forbidden` - Bạn không có quyền truy cập hồ sơ này. |
| **SEC-06** | Xem lén Điểm số khi đang chấm thầu | Gọi API `/scores` hoặc `/rankings` khi gói thầu đang `Evaluating` | Trường `TotalScore` và `Rank` trả về `null`. Giám khảo chỉ thấy điểm của chính mình. |
| **SEC-07** | Giám khảo tự duyệt trao thầu (Anti-Self Approval) | Giám khảo gọi API `POST /api/evaluations/packages/{id}/finalize` | Bị từ chối với mã lỗi `HTTP 403 Forbidden` (Chỉ Bên mời thầu / Chủ đầu tư mới có quyền). |
| **SEC-08** | Phân bổ mốc vượt giá trị hợp đồng | Hợp đồng 5 tỷ, cố tình tạo các mốc có tổng giá trị 5.5 tỷ | Bị từ chối với thông báo: *"Tổng giá trị các mốc vượt quá giá trị hợp đồng."* |
| **SEC-09** | Kích hoạt hợp đồng khi thiếu file scan | Bấm kích hoạt hợp đồng khi chưa tải lên tệp scan có dấu đỏ | Bị từ chối: *"Hợp đồng chỉ được kích hoạt khi đã tải lên bản scan hợp đồng hợp lệ."* |
| **SEC-10** | Cán bộ kỹ thuật tự duyệt giải ngân tiền | Cán bộ kỹ thuật gọi API duyệt chi giải ngân mốc thanh toán | Bị từ chối: Bắt buộc phải là Người có thẩm quyền ký độc lập (`SigningAuthority`). |
| **SEC-11** | Phát hiện tệp snapshot nghiệm thu bị sửa đổi | Can thiệp sửa đổi nội dung tệp snapshot biên bản nghiệm thu trên đĩa | Khi người có thẩm quyền ký phê duyệt, hệ thống phát hiện sai lệch mã băm SHA-256 và trả về `HTTP 409 Conflict`. |
| **SEC-12** | Tải lên tệp giả mạo đuôi mở rộng | Đổi tên tệp thực thi `virus.exe` thành `hoso.pdf` rồi tải lên | Bị từ chối ngay tại tầng kiểm tra Header nhị phân (Magic Bytes Verification). |

---

## 5. LỆNH KIỂM THỬ TỰ ĐỘNG HÓA (AUTOMATED TEST SUITE EXECUTION)

Hệ thống được trang bị bộ kiểm thử tự động toàn diện từ tầng Unit Test, Integration Test đến HTTP Pipeline:

### 5.1. Chạy toàn bộ 93 bài kiểm thử Backend C# (.NET 8):
Mở PowerShell tại thư mục gốc dự án:
```powershell
dotnet test tests/ProcurementSystem.Tests/ProcurementSystem.Tests.csproj --verbosity normal
```
*Kết quả kỳ vọng:* **93 Passed, 0 Failed, 0 Skipped (Tỷ lệ đạt 100%)**.

### 5.2. Chạy bài kiểm thử Đơn vị Frontend (JavaScript / Vite):
```powershell
cd frontend
npm test
```
*Kết quả kỳ vọng:* **6 Passed, 0 Failed (Tỷ lệ đạt 100%)**.

### 5.3. Kiểm tra tính toàn vẹn đóng gói ứng dụng (Production Build):
```powershell
cd frontend
npm run build
```
*Kết quả kỳ vọng:* Hoàn tất chuyển đổi mã nguồn và đóng gói Vite thành công trong 4-6 giây với **0 lỗi**.

---

## TỔNG KẾT

Tài liệu này cung cấp bức tranh toàn cảnh và chi tiết từng thao tác bấm chuột để bạn có thể tự tin làm chủ và kiểm chứng 100% tính năng của **Hệ thống Quản lý Đấu thầu & Theo dõi Hợp đồng**. 

Mọi thắc mắc hoặc cần hỗ trợ thêm kịch bản nâng cao, vui lòng tra cứu thêm các tài liệu kỹ thuật liên quan:
- [tai_khoan_dang_nhap.txt](file:///d:/Thuc%20Tap/tai_khoan_dang_nhap.txt) - Danh sách tài khoản đăng nhập.
- [HUONG_DAN_CAU_HINH_SUPABASE_STORAGE.md](file:///d:/Thuc%20Tap/DuAnTT/HUONG_DAN_CAU_HINH_SUPABASE_STORAGE.md) - Cấu hình Cloud Storage & AES-256.
- [TONG_HOP_CONG_VIEC_HOAN_THANH.md](file:///d:/Thuc%20Tap/DuAnTT/TONG_HOP_CONG_VIEC_HOAN_THANH.md) - Báo cáo tổng hợp quy tắc hệ thống N1 - N26.
