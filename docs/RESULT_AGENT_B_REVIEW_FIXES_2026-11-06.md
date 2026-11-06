# BÁO CÁO KẾT QUẢ KHẮC PHỤC REVIEW VÀ BỔ SUNG BẰNG CHỨNG KIỂM CHỨNG - AGENT B (2026-11-06)

**Người thực hiện**: Agent B  
**Nhánh thực hiện**: `feat/agent-b-security-otp-storage`  
**Dựa trên tài liệu**: `HANDOFF_AGENT_B_REVIEW_FIXES_2026-11-06.md`  
**Ngày hoàn thành**: 06/11/2026  

---

## 1. TỔNG QUAN KẾT QUẢ THỰC HIỆN

| Mã hạng mục | Mức độ | Yêu cầu nghiệp vụ & Kỹ thuật | Trạng thái | Ghi chú kiểm chứng |
| :--- | :---: | :--- | :---: | :--- |
| **R-B01** | **P1** | Sửa `FileCryptoCompressionService` và `Program.cs` để Local Storage hoạt động độc lập, không bắt buộc `MasterKey` | **HOÀN THÀNH** | Khởi tạo qua DI không ném lỗi; trì hoãn kiểm tra khóa đến khi thực thi mã hóa/giải mã |
| **R-B02** | **P2** | Sửa `SupabaseStorageService.ResolveDownloadAsync`: Tự động nhận diện header `PSEN` để giải mã độc lập với cờ `Encryption.Enabled` | **HOÀN THÀNH** | Đọc file cũ có header `PSEN` giải mã bình thường kể cả khi tắt cờ ghi mới; không fallback trả ciphertext hay redirect |
| **R-B03** | **P2** | Sửa lỗi nhận diện tệp `PSEN` bị cắt ngắn (< 20 bytes) trong `RestoreFromStorageAsync` | **HOÀN THÀNH** | Kiểm tra header trước; ném `InvalidDataException` khi cắt ngắn; passthrough an toàn với file < 4 bytes |
| **R-B04** | **P2** | Bổ sung bài test kiểm chứng OTP boundary thực tế với `OtpService` và `IMemoryCache` thật | **HOÀN THÀNH** | Không lộ `DevOtpCode`, chống replay, chặn brute-force 5 lần sai, phân tách purpose, kiểm thử đăng ký E2E |

**Thống kê kết quả kiểm thử**:
- **Backend Test Suite**: **106/106 tests PASSED (100%)** (Thời gian chạy: 35s).
- **Frontend Test Suite**: **6/6 tests PASSED (100%)**.
- **Frontend Production Build**: **Thành công 100%** (Vite build hoàn tất trong 5.52s).

---

## 2. CHI TIẾT CÁC SỬA ĐỔI KỸ THUẬT

### 2.1. R-B01: Độc lập hóa Local Storage khỏi MasterKey
- **Tệp chỉnh sửa**: 
  - `ProcurementSystem.Infrastructure/Services/FileCryptoCompressionService.cs`
  - `ProcurementSystem.API/Program.cs`
- **Chi tiết**:
  - Tại constructor `FileCryptoCompressionService`: Nếu `MasterKey` rỗng hoặc null, service ghi log warning (nếu `Encryption:Enabled == true`) và gán `_masterKey = null`, **tuyệt đối không ném exception**.
  - Ràng buộc khóa chỉ kích hoạt khi thực thi nghiệp vụ:
    + `ProcessForStorageAsync`: Ném `InvalidOperationException` nếu `_masterKey == null`.
    + `RestoreFromStorageAsync`: Khi phát hiện header `PSEN`, nếu `_masterKey == null` thì ném `InvalidOperationException`.
  - Tại `Program.cs`: Startup validation chỉ kiểm tra tính hợp lệ của `MasterKey` khi và chỉ khi: `StorageSettings:Provider == "Supabase"` VÀ `StorageSettings:Encryption:Enabled == true`. Khi chọn `Local` hoặc để trống (mặc định), hệ thống khởi động trơn tru.

### 2.2. R-B02: Giải mã tệp Cloud độc lập với cờ cấu hình ghi
- **Tệp chỉnh sửa**:
  - `ProcurementSystem.Infrastructure/Services/SupabaseStorageService.cs`
- **Chi tiết**:
  - Loại bỏ hoàn toàn điều kiện `if (_settings.Encryption?.Enabled == true)` trong `ResolveDownloadAsync`.
  - Toàn bộ tệp trên Supabase Storage khi tải về sẽ được stream qua `_cryptoService.RestoreFromStorageAsync`.
  - `RestoreFromStorageAsync` tự động nhận diện header nhị phân:
    + Nếu có header `PSEN`: Giải mã AES-256-CBC và giải nén GZip bằng `MasterKey` (kể cả khi admin đã tắt cờ `Encryption:Enabled` để ghi file mới dạng plain).
    + Nếu không có header `PSEN`: Passthrough trả về stream plaintext an toàn.
  - Xử lý ngoại lệ nghiêm ngặt: Nếu tệp mã hóa bị sai khóa hoặc thiếu khóa, hệ thống ném `CryptographicException` / `InvalidOperationException`, **tuyệt đối không fallback trả Pre-signed URL chứa ciphertext** hoặc fallback sang Local.
  - Bổ sung constructor nhận `IAmazonS3` phục vụ Dependency Injection và Unit Testing.
  - Trong `SaveFileAsync` và `SaveStreamAsync`: Gắn metadata `x-amz-meta-encrypted = true` khi tải tệp đã mã hóa lên Cloud.

### 2.3. R-B03: Khắc phục nhận nhầm tệp PSEN bị cắt cụt là plaintext
- **Tệp chỉnh sửa**:
  - `ProcurementSystem.Infrastructure/Services/FileCryptoCompressionService.cs`
- **Chi tiết**:
  - Thay đổi thứ tự kiểm tra trong `RestoreFromStorageAsync`:
    + Nếu `memorySource.Length < 4`: Chắc chắn không phải `PSEN` -> Trả về plaintext stream gốc an toàn (hỗ trợ stream 0 byte hoặc tệp nhỏ < 4 bytes).
    + Đọc 4 bytes đầu: Nếu khác `PSEN` -> Passthrough stream plaintext gốc an toàn.
    + Nếu khớp header `PSEN`:
      * Nếu `Length < 20` (thiếu 16 bytes IV): Ném `InvalidDataException("Tệp dữ liệu có header mã hóa 'PSEN' nhưng bị cắt cụt (thiếu 16 bytes IV).")`.
      * Nếu `_masterKey == null`: Ném `InvalidOperationException("Tệp dữ liệu đã được mã hóa bảo mật nhưng hệ thống hiện chưa được cấu hình MasterKey hợp lệ để giải mã.")`.
      * Đọc 16 bytes IV: Nếu không có ciphertext payload sau IV (`Position >= Length`), ném `InvalidDataException("Tệp mã hóa PSEN không có nội dung ciphertext.")`.
      * Tiến hành giải mã AES-256 + GZip. Nếu thất bại, ném `CryptographicException`.
    + Quản lý bộ nhớ: Đảm bảo giải phóng `memorySource` trong khối `finally`.

### 2.4. R-B04: Bộ kiểm chứng OTP Boundary thực tế
- **Tệp chỉnh sửa**:
  - `tests/ProcurementSystem.Tests/RegistrationOtpSecurityTests.cs`
- **Chi tiết**:
  - Khởi tạo `OtpService` thật kết nối với `IMemoryCache` thật, cấu hình SMTP InMemory (`Smtp:Host`, `Smtp:Username`, `Smtp:Password`) và `CapturingFakeEmailService` ghi nhận thư gửi đi.
  - Triển khai 4 bài kiểm thử bảo mật chuyên sâu:
    1. `SendOtpAsync_WithRealCacheAndEmail_GeneratesSecureCode_AndDoesNotExposeDevOtpCode`:
       - Gọi `SendOtpAsync` thật -> Gửi thư thành công.
       - Xác nhận `DevOtpCode == null` trong phản hồi API.
       - Trích xuất mã OTP 6 số từ nội dung thư gửi đi.
       - Verify thành công với mã trích xuất.
       - Xác minh tính năng chống Replay Attack (mã bị hủy ngay sau lần đầu xác thực thành công, dùng lại bị từ chối).
    2. `VerifyOtp_WhenFailed5Times_ShouldInvalidateCode_AndBlockSubsequentAttempts`:
       - Thử sai liên tiếp 5 lần với mã `000000`.
       - Lần 1-4: Báo số lần thử còn lại.
       - Lần 5: Thông báo mã bị hủy do brute-force quá 5 lần.
       - Lần 6: Nhập đúng mã ban đầu vẫn thất bại vì mã đã bị xóa khỏi cache.
    3. `VerifyOtp_WhenPurposeMismatch_ShouldFailVerification`:
       - Gửi OTP với purpose `RegisterContractor`.
       - Xác thực với purpose `RegisterProcuringEntity` hoặc `ResetPassword` bị từ chối.
       - Xác thực với đúng purpose `RegisterContractor` thành công.
    4. `EndToEnd_ContractorRegistration_WithRealOtpDelivery` & `EndToEnd_ProcuringEntityRegistration_WithRealOtpDelivery`:
       - Kiểm thử boundary tích hợp toàn diện: Gửi OTP thật -> Lấy mã từ fake email -> Đăng ký tài khoản thành công -> Gọi lại lần 2 bị từ chối vì OTP đã tiêu thụ.

---

## 3. BẰNG CHỨNG KIỂM CHỨNG TỰ ĐỘNG

### 3.1. Kết quả chạy toàn bộ Backend Test Suite (`dotnet test`)
```text
Test run for D:\Thuc Tap\DuAnTT\tests\ProcurementSystem.Tests\bin\Debug\net8.0\ProcurementSystem.Tests.dll (.NETCoreApp,Version=v8.0)
VSTest version 17.14.1 (x64)

Starting test execution, please wait...
A total of 1 test files matched the specified pattern.

Passed!  - Failed:     0, Passed:   106, Skipped:     0, Total:   106, Duration: 35 s - ProcurementSystem.Tests.dll (net8.0)
```
- Số lượng test ban đầu: 93 tests.
- Số lượng test hiện tại: **106 tests (+13 tests mới bao phủ R-B01, R-B02, R-B03, R-B04)**.
- Tỉ lệ Pass: **100% (106/106 tests)**.

### 3.2. Kết quả chạy Frontend Test Suite (`npm test`)
```text
> node --test tests/*.test.mjs --run
✔ generated enums match BE source including Cancelled and Success (74.7362ms)
✔ wire request uses enum IDs and preserves zero filters (16.9458ms)
✔ unknown enum is rejected, names and numeric REST/realtime values normalize equally (0.3136ms)
✔ string domain statuses and role codes are not converted into invented IDs (0.104ms)
✔ unsaved preview normalizes MaxScore rather than treating every criterion as /100 (1.1594ms)
✔ manual preview does not fabricate automatic price points or a complete /100 total (0.12ms)
ℹ tests 6
ℹ suites 0
ℹ pass 6
ℹ fail 0
ℹ cancelled 0
ℹ skipped 0
ℹ todo 0
ℹ duration_ms 187.5947
```

### 3.3. Kết quả Frontend Production Build (`npm run build`)
```text
vite v8.3.1 building client environment for production...
transforming...
✓ 2075 modules transformed.
rendering chunks...
computing gzip size...
dist/index.html                     0.47 kB │ gzip:   0.30 kB
dist/assets/index-BWqP3dqS.css     73.74 kB │ gzip:  12.28 kB
dist/assets/index-w5Ul2-OV.js   1,358.48 kB │ gzip: 332.28 kB
✓ built in 5.52s
```

---

## 4. DANH SÁCH TỆP ĐÃ THAY ĐỔI / TẠO MỚI

1. `ProcurementSystem.Infrastructure/Services/FileCryptoCompressionService.cs` (Sửa R-B01 và R-B03)
2. `ProcurementSystem.Infrastructure/Services/SupabaseStorageService.cs` (Sửa R-B02, gắn metadata S3, bổ sung DI constructor)
3. `tests/ProcurementSystem.Tests/ProcurementSystem.Tests.csproj` (Bổ sung PackageReference Moq 4.20.72)
4. `tests/ProcurementSystem.Tests/FileCryptoCompressionTests.cs` (Cập nhật test constructor R-B01, bổ sung 5 tests R-B03)
5. `tests/ProcurementSystem.Tests/HybridStorageAndAdminTests.cs` (Bổ sung 4 tests DI resolution R-B01 và giải mã độc lập R-B02)
6. `tests/ProcurementSystem.Tests/RegistrationOtpSecurityTests.cs` (Bổ sung 5 tests OTP boundary thực tế R-B04)
7. `HUONG_DAN_CAU_HINH_SUPABASE_STORAGE.md` (Cập nhật tài liệu cấu hình Local Storage, cơ chế giải mã tự động và cấu hình SMTP)
8. `docs/RESULT_AGENT_B_REVIEW_FIXES_2026-11-06.md` (Tạo mới báo cáo nghiệm thu Agent B)

---

## 5. HƯỚNG DẪN KIỂM TRA DÀNH CHO REVIEWER

1. **Kiểm tra Unit Tests Backend**:
   ```powershell
   cd "d:\Thuc Tap\DuAnTT"
   dotnet test tests/ProcurementSystem.Tests/ProcurementSystem.Tests.csproj
   ```
   *Kỳ vọng: Toàn bộ 106 tests đều Passed.*

2. **Kiểm tra Unit Tests Frontend & Build**:
   ```powershell
   cd "d:\Thuc Tap\DuAnTT\frontend"
   npm test
   npm run build
   ```
   *Kỳ vọng: 6 tests Passed, bản build Vite tạo thành công trong thư mục `dist/`.*

3. **Kiểm tra tính độc lập của Local Storage**:
   - Trong `ProcurementSystem.API/appsettings.json`, đặt:
     ```json
     "StorageSettings": {
       "Provider": "Local",
       "Encryption": { "Enabled": false, "MasterKey": "" }
     }
     ```
   - Khởi động ứng dụng Backend: Ứng dụng khởi động bình thường, không ném ngoại lệ.
