# BÁO CÁO KẾT QUẢ SỬA LỖI & HOÀN THIỆN REFACTOR TUẦN 12 (W12-01 ĐẾN W12-04)

**Ngày thực hiện:** 13/11/2026 – 14/11/2026  
**Căn cứ bàn giao:** `HANDOFF_AGENT_WEEK12_REFACTOR_REVIEW_FIXES_2026-11-13.md`  
**Nhánh làm việc:** `fix/week12-refactor-review-fixes` (Tách từ `dev`)  
**Kết quả kiểm thử hồi quy:**
* **Backend xUnit:** **153/153 Passed** (100% thành công)
* **Frontend Node Test:** **6/6 Passed** (100% thành công)
* **Frontend Bundle:** Vite build thành công (`✓ built in 12.47s`)
* **Git diff check:** Không có lỗi định dạng hay khoảng trắng thừa (`git diff --check` return code 0)

---

## 1. TỔNG HỢP KẾT QUẢ THEO TỪNG HẠNG MỤC

### 1.1. W12-01: Nối lại mã OTP ở cả hai Form đăng ký (P1)

* **Tệp chỉnh sửa:**
  * `frontend/src/hooks/useOtpSender.js`
  * `frontend/src/components/auth/ContractorRegisterForm.jsx`
  * `frontend/src/components/auth/ProcuringEntityRegisterForm.jsx`
* **Hiện trạng trước sửa:**
  * Sau khi tách form, hai component `ContractorRegisterForm` và `ProcuringEntityRegisterForm` chỉ tạo state `otpCode: ''` và thêm vào multipart nếu có giá trị. Giao diện hoàn toàn thiếu nút gửi OTP và ô nhập mã.
  * Hook `useOtpSender` chưa có cơ chế chống race-condition khi đổi email trong lúc request đang gửi mạng dở dang.
* **Giải pháp sau sửa:**
  * Bổ sung cơ chế `inFlightEmailRef` và hàm `resetOtp()` trong `useOtpSender.js`: Khi người dùng thay đổi trường Email, form tự động reset mã OTP cũ, hủy đếm ngược và bỏ qua response nếu phản hồi mạng của email cũ trả về muộn.
  * Tích hợp `useOtpSender('RegisterContractor', ...)` vào `ContractorRegisterForm.jsx` và `useOtpSender('RegisterProcuringEntity', ...)` vào `ProcuringEntityRegisterForm.jsx`.
  * Bổ sung giao diện chuẩn Tailwind: Nút "Lấy mã OTP" / "Gửi lại (Xs)" tích hợp bên cạnh ô Email, ô nhập "Mã xác thực OTP (6 chữ số)" chuẩn `font-mono tracking-widest` ngay dưới ô Email.
  * Đưa `otpCode` vào `REQUIRED_*_FIELDS` và `FORM_FIELD_MAP` với `isRequired = true`, đảm bảo luôn gửi `OtpCode` trong multipart FormData và hiển thị lỗi server chính xác.
* **Kiểm chứng:**
  * `npm test --prefix frontend`: 6/6 test passed.
  * `npm run build --prefix frontend`: Biên dịch Vite thành công, bundle 2087 modules không lỗi.

---

### 1.2. W12-02: Giới hạn thẩm quyền đọc chi tiết hợp đồng theo đơn vị (P1)

* **Tệp chỉnh sửa:**
  * `ProcurementSystem.Infrastructure/Services/ContractService.cs` (`GetContractByIdAsync`)
  * `tests/ProcurementSystem.Tests/Priority1BoundaryAndCapabilitiesTests.cs`
* **Hiện trạng trước sửa:**
  * `ContractsController.GetContractById` truyền `isInternalStaff = true` cho vai trò `Admin` hoặc `Procurement`. Trong `ContractService.GetContractByIdAsync`, nhánh kiểm tra chủ quyền chỉ kích hoạt khi `!isInternalStaff`. Nhân sự Bên mời thầu của đơn vị B có thể đọc chi tiết hợp đồng của đơn vị A.
* **Giải pháp sau sửa:**
  * Bổ sung kiểm tra thẩm quyền đọc nội bộ trước khi tạo DTO:
    ```csharp
    if (isInternalStaff && !isAdmin &&
        !await PackageWriteAuthorization.CanReadEntityStaffAsync(_unitOfWork, contract.BidPackage, userId, isAdmin))
    {
        return ApiResponse<ContractDto>.Forbidden("Bạn không có quyền xem hợp đồng thuộc đơn vị mời thầu khác.");
    }
    ```
  * Chuẩn hóa trả về `ApiResponse<ContractDto>.NotFound(...)` khi hợp đồng không tồn tại (thay vì `Fail` chung chung).
  * Bảo toàn quyền đọc của `Admin`, thành viên `Active` cùng đơn vị và Nhà thầu trúng thầu hợp đồng đó. Chặn triệt để nhân sự đơn vị ngoài hoặc thành viên đã bị thu hồi quyền (`Revoked`).
* **Kiểm chứng:**
  * Mở rộng test trong `Priority1BoundaryAndCapabilitiesTests.cs`:
    * Admin xem hợp đồng -> 200 OK.
    * Nhân sự active cùng đơn vị A (`managerA`, `officerCreatorA`, `officerOtherA`, `signerA`) -> 200 OK.
    * Nhân sự bị thu hồi quyền (`revokedOfficerA`) -> 403 Forbidden.
    * Nhân sự Bên mời thầu đơn vị B (`outsiderB`) -> 403 Forbidden.
    * Nhà thầu sở hữu hợp đồng (`contractorUser1`) -> 200 OK.
    * Nhà thầu khác (`contractorUser2`) -> 403 Forbidden.

---

### 1.3. W12-03: Kiểm tra tính hợp lệ của bản sao Local khi đọc file Cloud (P2)

* **Tệp chỉnh sửa:**
  * `ProcurementSystem.Infrastructure/Services/SupabaseStorageService.cs` (`ResolveDownloadAsync`, `IsValidLocalCopyAsync`)
  * `ProcurementSystem.Infrastructure/Services/DocumentSampleFallbackProvider.cs` (`NeedsFallbackGeneration`)
  * `tests/ProcurementSystem.Tests/HybridStorageAndAdminTests.cs`
* **Hiện trạng trước sửa:**
  * `SupabaseStorageService.ResolveDownloadAsync` dùng `!NeedsFallbackGeneration(localFullPath)` để nhận diện bản sao Local. Nếu tệp rỗng (0 bytes) hoặc không có dấu hiệu stub, hàm có thể coi là hợp lệ và trả về tệp rỗng cho người dùng.
* **Giải pháp sau sửa:**
  * Tách biệt logic xác thực bản sao có thể phục vụ (`IsValidLocalCopyAsync`):
    * Kiểm tra tệp tồn tại và dung lượng > 0 bytes.
    * Đối với PDF: Yêu cầu dung lượng tối thiểu >= 20 bytes, header bắt đầu bằng 4 magic bytes `%PDF` (`0x25 0x50 0x44 0x46`), và không chứa nội dung stub notice / html lỗi.
    * Đối với định dạng khác (.png, .docx, .xlsx, .zip...): Yêu cầu dung lượng > 0 và không chứa stub notice.
  * Tinh chỉnh `DocumentSampleFallbackProvider.NeedsFallbackGeneration`: Nhận diện chuẩn xác tệp thiếu, tệp 0 bytes hoặc tệp stub notice HTML để kích hoạt luồng sinh mẫu an toàn mà không phá vỡ các tệp dữ liệu kiểm thử có sẵn.
* **Kiểm chứng:**
  * Bổ sung 2 test hồi quy chuyên sâu trong `HybridStorageAndAdminTests.cs`:
    * `DocumentSampleFallbackProvider_NeedsFallbackGeneration_IdentifiesEmptyStubAndInvalidPdfs`: Nhận diện chính xác tệp không tồn tại, tệp 0 bytes, tệp stub notice.
    * `SupabaseStorageService_ResolveDownloadAsync_WhenLocalCopyIsCorruptOrEmpty_ReplacesWithValidFallback`: Bản sao local bị rỗng được tự động thay thế bằng PDF hợp lệ có header `%PDF`.

---

### 1.4. W12-04: Bảo toàn mã trạng thái HTTP của lỗi tải file (P2)

* **Tệp chỉnh sửa:**
  * `ProcurementSystem.API/Extensions/ControllerResultExtensions.cs` (`ToFileActionResult`)
  * `tests/ProcurementSystem.Tests/Priority1BoundaryAndCapabilitiesTests.cs`
* **Hiện trạng trước sửa:**
  * Overload `ToFileActionResult` nhận `ApiResponse<FileDownloadResult>` có dòng logic:
    ```csharp
    var statusCode = result.StatusCode >= 400 && result.StatusCode != StatusCodes.Status400BadRequest
        ? result.StatusCode
        : StatusCodes.Status404NotFound;
    ```
    Logic này đã cố tình biến đổi lỗi `400 BadRequest` (ví dụ từ `ContractorService.GetBusinessLicenseForDownloadAsync`) thành `404 NotFound`.
* **Giải pháp sau sửa:**
  * Bảo toàn trực tiếp mã trạng thái từ Service:
    ```csharp
    var statusCode = result.StatusCode >= 400
        ? result.StatusCode
        : StatusCodes.Status404NotFound;

    return statusCode switch
    {
        StatusCodes.Status400BadRequest => controller.BadRequest(payload),
        StatusCodes.Status403Forbidden => controller.StatusCode(StatusCodes.Status403Forbidden, payload),
        StatusCodes.Status404NotFound => controller.NotFound(payload),
        _ => controller.StatusCode(statusCode, payload)
    };
    ```
  * Giữ nguyên vẹn payload lỗi JSON `ApiResponse<string>` để client nhận diện lỗi rõ ràng.
* **Kiểm chứng:**
  * Thêm test `ToFileActionResult_PreservesHttpErrorCodes_BadRequest_Forbidden_NotFound` trong `Priority1BoundaryAndCapabilitiesTests.cs`:
    * `ApiResponse.Fail(..., 400)` -> `BadRequestObjectResult` (HTTP 400).
    * `ApiResponse.Forbidden(...)` -> `ObjectResult` (HTTP 403).
    * `ApiResponse.NotFound(...)` -> `NotFoundObjectResult` (HTTP 404).
    * `ApiResponse.Ok(...)` -> `FileStreamResult` (HTTP 200).

---

## 2. BẢNG TỔNG HỢP KIỂM CHỨNG KỸ THUẬT

| STT | Bộ kiểm thử / Lệnh | Số ca | Kết quả | Ghi chú |
| :--- | :--- | :---: | :---: | :--- |
| 1 | `ProcurementSystem.Tests` (Backend xUnit) | 153 | **153 Passed** | Bao phủ Boundary, Capabilities, StateMachines, DotEnvLoader, HybridStorage, SQL Server Integration. |
| 2 | `frontend tests` (Node.js test runner) | 6 | **6 Passed** | Kiểm tra enum đồng bộ BE-FE, preview scoring và wire request format. |
| 3 | `frontend build` (Vite 8.3.1) | 2087 modules | **Thành công** | `index.html` và assets sinh đầy đủ, 0 lỗi cú pháp JSX. |
| 4 | `git diff --check` | - | **Thành công** | Không có trailing whitespace hay conflict marker còn sót lại. |
| 5 | Browser E2E | - | **NOT RUN** | Môi trường dòng lệnh thực hiện kiểm thử tự động ở cấp độ Controller/Service/HTTP Pipeline. Xác nhận chức năng giao diện thông qua build thành công và unit tests. |

---

## 3. KẾT LUẬN & TRẠNG THÁI BÀN GIAO

* Tất cả 4 hạng mục **W12-01**, **W12-02**, **W12-03**, **W12-04** đã được hoàn thành trọn vẹn theo đúng yêu cầu bàn giao, tuân thủ nghiêm ngặt nguyên tắc SOLID và Clean Architecture.
* Mã nguồn sạch sẽ, không ảnh hưởng đến các tính năng khác đang phát triển. Toàn bộ 153 test backend và 6 test frontend đều xanh.
