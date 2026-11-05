# KẾT QUẢ TRIỂN KHAI AGENT B: BẢO MẬT OTP ĐĂNG KÝ VÀ KHÓA MÃ HÓA STORAGE

**Ngày hoàn thành:** 05/11/2026  
**Nhánh triển khai:** `feat/agent-b-security-otp-storage` (tách từ `dev`)  
**Baseline HEAD ban đầu:** `997cf6f` (kế thừa `eb9d90a` và `f5111b8`)  
**Người thực hiện:** Agent B  

---

## 1. TÓM TẮT HÀNH VI TRƯỚC VÀ SAU KHI SỬA

| Hạng mục | Hành vi trước khi sửa (Vấn đề) | Hành vi sau khi sửa (Chuẩn hóa) |
|---|---|---|
| **OTP Đăng ký Nhà thầu** | `OtpCode` là `string?`, service chỉ kiểm tra `if (!string.IsNullOrWhiteSpace(request.OtpCode))`. Bỏ trống `OtpCode` vẫn đăng ký thành công $\rightarrow$ Bypass OTP hoàn toàn. | Bắt buộc `[Required]` và `[RegularExpression(@"^\d{6}$")]` ở DTO; Service bắt buộc OTP hợp lệ với purpose `RegisterContractor`. Thiếu/sai OTP lập tức từ chối, không tạo User/Contractor. |
| **OTP Đăng ký Bên mời thầu** | `OtpCode` là `string?`, bỏ trống `OtpCode` vẫn tạo tài khoản Bên mời thầu và Manager member thành công. | Bắt buộc `[Required]` và `[RegularExpression(@"^\d{6}$")]` ở DTO; Service bắt buộc OTP hợp lệ với purpose `RegisterProcuringEntity`. Thiếu/sai OTP lập tức từ chối, không tạo User/Entity/Member. |
| **Giao diện Đăng ký (`RegisterPage.jsx`)** | Ô nhập OTP và nút gửi mã bị tạm ẩn (`{/* Email đăng nhập (Đã tạm ẩn OTP) */}`). Submit form không bắt buộc mã OTP. | Khôi phục 100% UI gửi mã, đếm ngược cooldown 60s, ô nhập OTP 6 số. Bổ sung cơ chế tự động hủy xác thực và xóa mã OTP nếu người dùng sửa lại email sau khi đã lấy mã. |
| **Khóa mã hóa Storage (`MasterKey`)** | Nếu `MasterKey` rỗng $\rightarrow$ tự dùng fallback key cố định SHA-256; nếu sai Base64 $\rightarrow$ fallback về `DeriveKeyFromSeed`. | Loại bỏ hoàn toàn fallback key và `DeriveKeyFromSeed`. MasterKey bắt buộc là chuỗi Base64 giải mã ra đúng 32 bytes (256 bits). Thiếu/sai khóa ném ngoại lệ rõ ràng khi khởi động và khi mã hóa/giải mã. |
| **Giải mã tệp mã hóa Storage** | Khi giải mã file có header `"PSEN"` bị lỗi do sai khóa, hàm catch ngoại lệ và trả về ciphertext dạng fallback. `SupabaseStorageService` chuyển hướng pre-signed URL tới file mã hóa. | Nếu giải mã thất bại $\rightarrow$ ném `CryptographicException` ngay lập tức, `SupabaseStorageService` không fallback che giấu lỗi khóa; tuyệt đối không bao giờ trả về ciphertext giả vờ thành công. |
| **Tương thích ngược tệp cũ (Legacy)** | Tệp legacy không có header `"PSEN"` (hoặc dung lượng < 20 bytes). | Tiếp tục pass-through luồng plain data nguyên bản an toàn, bảo đảm các tệp cũ tải lên trước khi có mã hóa vẫn đọc bình thường. |

---

## 2. DANH SÁCH TỆP VÀ HÀM ĐÃ CHỈNH SỬA

1. **`ProcurementSystem.Core/DTOs/Auth/RegisterContractorRequest.cs`**:
   - `OtpCode`: Thêm `[Required]`, `[RegularExpression(@"^\d{6}$")]`, kiểu dữ liệu `string` (không nullable).
2. **`ProcurementSystem.Core/DTOs/Auth/RegisterProcuringEntityRequest.cs`**:
   - `OtpCode`: Thêm `[Required]`, `[RegularExpression(@"^\d{6}$")]`, kiểu dữ liệu `string` (không nullable).
3. **`ProcurementSystem.Infrastructure/Services/ContractorAuthService.cs`**:
   - `RegisterContractorAsync`: Bắt buộc kiểm tra rỗng của `OtpCode`, xác thực qua `_otpService.VerifyOtp(normalizedEmail, request.OtpCode.Trim(), "RegisterContractor", out var otpError)`.
4. **`ProcurementSystem.Infrastructure/Services/ProcuringEntityAuthService.cs`**:
   - `RegisterProcuringEntityAsync`: Bắt buộc kiểm tra rỗng của `OtpCode`, xác thực qua `_otpService.VerifyOtp(normalizedEmail, request.OtpCode.Trim(), "RegisterProcuringEntity", out var otpError)`.
5. **`frontend/src/pages/RegisterPage.jsx`**:
   - `handleContractorChange` & `handleProcuringChange`: Khi `name === 'email'` $\rightarrow$ tự động reset `otpCode: ''`, `otpSent: false`, `otpCooldown: 0`.
   - `handleContractorSubmit` & `handleProcuringSubmit`: Validate bắt buộc 6 số OTP trước khi gửi form.
   - Khôi phục khối UI nhập Email kèm nút Gửi mã OTP (hiển thị đếm ngược cooldown) và ô nhập mã OTP 6 số cho cả 2 tab Nhà thầu & Bên mời thầu.
6. **`ProcurementSystem.Infrastructure/Services/FileCryptoCompressionService.cs`**:
   - Constructor: Validate `MasterKey` đúng 32 bytes Base64; loại bỏ hoàn toàn fallback deterministic key và `DeriveKeyFromSeed`.
   - `ProcessForStorageAsync`: Ném `InvalidOperationException` nếu `_masterKey == null`.
   - `RestoreFromStorageAsync`: Ném `InvalidOperationException` nếu tệp có header `"PSEN"` mà `_masterKey == null`; ném `CryptographicException` nếu giải mã thất bại; pass-through tệp legacy không có `"PSEN"`.
7. **`ProcurementSystem.Infrastructure/Services/SupabaseStorageService.cs`**:
   - `ResolveDownloadAsync`: Không bắt `CryptographicException` và `InvalidOperationException` để fallback sang Pre-signed URL, ngăn chặn việc phát tán file mã hóa nhị phân thô cho người dùng.
8. **`ProcurementSystem.API/Program.cs`**:
   - Bổ sung startup validation kiểm tra `StorageSettings:Encryption:MasterKey` nếu Provider là `Supabase` và `Encryption:Enabled == true`.
9. **`HUONG_DAN_CAU_HINH_SUPABASE_STORAGE.md`**:
   - Bổ sung hướng dẫn sinh khóa 32 bytes Base64 ngẫu nhiên an toàn qua PowerShell và OpenSSL.
   - Thêm quy trình kiểm kê và chuyển đổi an toàn đối với các tệp mã hóa cũ nếu từng dùng fallback key.
10. **`tests/ProcurementSystem.Tests/RegistrationOtpSecurityTests.cs` (Tạo mới)**:
    - 8 bài kiểm thử chuyên sâu về OTP Nhà thầu, OTP Bên mời thầu, purpose isolation, replay attack, và bảo mật `DevOtpCode`.
11. **`tests/ProcurementSystem.Tests/FileCryptoCompressionTests.cs`**:
    - Bổ sung 4 bài test: MasterKey missing ném ngoại lệ, MasterKey invalid Base64 ném ngoại lệ, MasterKey wrong length ném ngoại lệ, và giải mã sai khóa ném `CryptographicException`.

---

## 3. BẢNG KẾT QUẢ KIỂM THỬ THỰC TẾ

| Mã Test | Phạm vi / Ca kiểm thử | Dữ liệu đầu vào | Kết quả mong đợi | Kết quả thực tế | Trạng thái |
|---|---|---|---|---|:---:|
| **OTP-01** | Contractor: Thiếu OTP (`null`) | Email hợp lệ, `OtpCode = null` | Trả về 400 Fail, không tạo User/Contractor | `result.Success = false`, DB User count = 0 | **PASS** |
| **OTP-02** | Contractor: OTP khoảng trắng (`"   "`) | `OtpCode = "   "` | Trả về 400 Fail | `result.Success = false`, DB User count = 0 | **PASS** |
| **OTP-03** | Contractor: Sai mã OTP | Cấp mã `654321`, gửi mã `000000` | Trả về 400 "Mã xác thực không chính xác" | `result.Success = false`, User count = 0 | **PASS** |
| **OTP-04** | Contractor: Lệch mục đích (Purpose Mismatch) | Cấp mã cho `RegisterProcuringEntity`, gửi đăng ký Nhà thầu | Bị từ chối vì không khớp Purpose | `result.Success = false`, User count = 0 | **PASS** |
| **OTP-05** | Contractor: Đúng OTP & Chống Replay | Cấp mã `123456`, gửi đúng mã | Thành công 200/201, mã OTP bị hủy sau dùng | `result.Success = true`, verify lại trả về `false` | **PASS** |
| **OTP-06** | ProcuringEntity: Thiếu OTP (`null`) | `OtpCode = null` | Trả về 400 Fail, không tạo User/Entity | `result.Success = false`, DB Entity count = 0 | **PASS** |
| **OTP-07** | ProcuringEntity: Sai mã OTP | Cấp mã `888888`, gửi `111111` | Trả về 400 Fail | `result.Success = false`, DB Entity count = 0 | **PASS** |
| **OTP-08** | ProcuringEntity: Đúng OTP & Tạo Manager Member | Cấp mã `999999`, gửi đúng mã | Đăng ký thành công, tự tạo Manager member | `result.Success = true`, Member Role = Manager, Status = Active | **PASS** |
| **OTP-09** | Bảo mật API: DevOtpCode không lộ | `SendOtpResponse` | `DevOtpCode == null` | `Assert.Null(response.DevOtpCode)` | **PASS** |
| **CRYPTO-01** | MasterKey rỗng khi bật mã hóa | `MasterKey = ""` | Ném `InvalidOperationException` | Ném đúng ngoại lệ và thông báo cấu hình | **PASS** |
| **CRYPTO-02** | MasterKey sai định dạng Base64 | `MasterKey = "not-a-base64!"` | Ném `InvalidOperationException` | Ném đúng ngoại lệ Base64 | **PASS** |
| **CRYPTO-03** | MasterKey sai độ dài (16 bytes) | `MasterKey` 16 bytes Base64 | Ném `InvalidOperationException` | Ném đúng ngoại lệ yêu cầu 32 bytes | **PASS** |
| **CRYPTO-04** | Giải mã sai khóa ném ngoại lệ | Mã hóa bằng Khóa A, giải mã bằng Khóa B | Ném `CryptographicException`, cấm trả ciphertext | Ném `CryptographicException` thành công | **PASS** |
| **CRYPTO-05** | Round-trip mã hóa và giải mã | Dữ liệu nén 50KB với Khóa A | Khôi phục 100% SHA256 ban đầu | Khớp SHA256 và kích thước 100% | **PASS** |
| **CRYPTO-06** | Pass-through dữ liệu legacy | Dữ liệu cũ không có header `"PSEN"` | Trả về nguyên vẹn plain stream | Dữ liệu trùng khớp 100% | **PASS** |
| **UI-TEST** | Frontend Tests | `npm test --prefix frontend` | 6/6 tests pass | 6/6 tests pass | **PASS** |
| **UI-BUILD** | Frontend Production Build | `npm run build --prefix frontend` | Vite build 0 lỗi | Build hoàn tất trong 4.59s | **PASS** |

---

## 4. BẰNG CHỨNG THỰC THI KIỂM CHỨNG (COMMAND OUTPUTS)

### 4.1. Bộ test Agent B (`RegistrationOtpSecurityTests` & `FileCryptoCompressionTests`)
```text
Command: rtk dotnet test ProcurementSystem.slnx --filter "FullyQualifiedName~RegistrationOtpSecurityTests|FullyQualifiedName~FileCryptoCompressionTests"
Result: 16 tests passed, 0 warnings in 1 projects (9.7 s) - 100% PASS
```

### 4.2. Bộ test toàn hệ thống sau tích hợp (`dotnet test`)
```text
Command: rtk dotnet test ProcurementSystem.slnx
Result: 93 tests passed, 0 warnings in 1 projects (28.8 s) - 100% PASS
```

### 4.3. Kiểm thử Frontend (`npm test` & `npm run build`)
```text
Command: npm test --prefix frontend
> node --test tests/*.test.mjs
✔ generated enums match BE source including Cancelled and Success (75.339ms)
✔ wire request uses enum IDs and preserves zero filters (20.5762ms)
✔ unknown enum is rejected, names and numeric REST/realtime values normalize equally (0.4787ms)
✔ string domain statuses and role codes are not converted into invented IDs (0.125ms)
✔ unsaved preview normalizes MaxScore rather than treating every criterion as /100 (1.1172ms)
✔ manual preview does not fabricate automatic price points or a complete /100 total (0.152ms)
ℹ tests 6 | ℹ pass 6 | ℹ fail 0 (duration: 236.6ms)

Command: npm run build --prefix frontend
✓ 2075 modules transformed.
✓ built in 4.59s
```

---

## 5. TÁC ĐỘNG TƯƠNG THÍCH VÀ KHUYẾN NGHỊ VẬN HÀNH

1. **Quy trình Đăng ký**:
   - Từ thời điểm này, mọi thao tác đăng ký Nhà thầu và Bên mời thầu đều bắt buộc phải nhập mã OTP 6 số gửi qua email thực tế.
   - Thao tác đổi email trên form đăng ký sẽ tự động hủy trạng thái mã OTP đã gửi, ngăn chặn tấn công mạo danh email.
2. **Quản lý Khóa Mã hóa Storage**:
   - Cần đảm bảo `StorageSettings:Encryption:MasterKey` được cấp phát chuỗi Base64 chuẩn 32 bytes trước khi khởi động API trên máy chủ production.
   - Hướng dẫn kiểm kê và chuyển đổi an toàn đã được cập nhật tại `HUONG_DAN_CAU_HINH_SUPABASE_STORAGE.md`.
