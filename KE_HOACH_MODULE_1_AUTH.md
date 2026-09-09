# KẾ HOẠCH & TÀI LIỆU PHỐI HỢP: MODULE 1 - AUTHENTICATION & CONTRACTOR REGISTRATION
> **Dự án**: Procurement & Contract Tracking System  
> **Mô hình phối hợp**: 2 Lập trình viên làm song song trên 2 nhánh Git riêng biệt (Zero-Conflict Workflow).  
> **Tính năng nâng cao (Enterprise Security)**: Tự động thẩm định Mã số thuế qua API Quốc gia + Kiểm tra Chữ ký số PDF (PAdES).

---

## 📑 MỤC LỤC
1. [Mục tiêu & Nguyên tắc làm việc song song](#1-mục-tiêu--nguyên-tắc-làm-việc-song-song)
2. [Chiến lược Kỹ thuật Chống Xung Đột Git (Zero-Conflict Pattern)](#2-chiến-lược-kỹ-thuật-chống-xung-đột-git)
3. [Nhiệm vụ Chi tiết - DEV 1: Đăng nhập & JWT Token](#3-nhiệm-vụ-chi-tiết---dev-1-đăng-nhập--jwt-token)
4. [Nhiệm vụ Chi tiết - DEV 2: Đăng ký Nhà thầu & Thẩm định Hồ sơ Pháp lý](#4-nhiệm-vụ-chi-tiết---dev-2-đăng-ký-nhà-thầu--thẩm-định-hồ-sơ-pháp-lý)
5. [Hướng dẫn Chi tiết: Tra cứu API Thuế & Kiểm tra Chữ ký số PDF (Cho DEV 2)](#5-hướng-dẫn-chi-tiết-tra-cứu-api-thuế--kiểm-tra-chữ-ký-số-pdf-cho-dev-2)
6. [Quy trình Git & Hợp nhất mã nguồn (Git Flow)](#6-quy-trình-git--hợp-nhất-mã-nguồn)
7. [Kịch bản Kiểm thử Tích hợp (End-to-End Testing)](#7-kịch-bản-kiểm-thử-tích-hợp)

---

## 1. MỤC TIÊU & NGUYÊN TẮC LÀM VIỆC SONG SONG

### 1.1 Phân chia trách nhiệm
| Nhân sự | Phụ trách chức năng chính | Endpoints phụ trách |
|---|---|---|
| **DEV 1** | Xác thực JWT, Đăng nhập, Refresh Token, Đăng xuất | `POST /api/auth/login`<br>`POST /api/auth/refresh-token`<br>`POST /api/auth/logout` |
| **DEV 2** | Đăng ký Nhà thầu, Upload file GPKD, **Tra cứu MST qua API Thuế**, **Kiểm tra Chữ ký số PDF** | `POST /api/auth/register-contractor` |

### 1.2 Nguyên tắc "Không đụng chạm code" (Zero Collision)
* **Không sửa chung một file Controller**: Dùng `partial class` của C# để tách thành 2 file: `AuthController.Login.cs` và `AuthController.Register.cs`.
* **Không sửa chung một Service**: Mỗi bên có Interface và Service độc lập.
* **Đăng ký DI tách biệt**: DEV 1 đăng ký nhóm Auth, DEV 2 đăng ký nhóm Contractor/Storage.

---

## 2. CHIẾN LƯỢC KỸ THUẬT CHỐNG XUNG ĐỘT GIT

```
ProcurementSystem.Core/
├── DTOs/Auth/
│   ├── LoginRequest.cs                   <-- DEV 1
│   ├── LoginResponse.cs                  <-- DEV 1
│   ├── RefreshTokenRequest.cs            <-- DEV 1
│   ├── RegisterContractorRequest.cs      <-- DEV 2
│   └── ContractorRegisterResponse.cs     <-- DEV 2
├── Interfaces/
│   ├── ITokenService.cs                  <-- DEV 1
│   ├── IAuthService.cs                   <-- DEV 1
│   ├── IFileStorageService.cs            <-- DEV 2
│   ├── ITaxLookupService.cs              <-- DEV 2 (Tra cứu API Thuế)
│   ├── IPdfSecurityService.cs            <-- DEV 2 (Kiểm tra Chữ ký số PDF)
│   └── IContractorAuthService.cs         <-- DEV 2
│
ProcurementSystem.Infrastructure/
├── Services/
│   ├── TokenService.cs                   <-- DEV 1
│   ├── AuthService.cs                    <-- DEV 1
│   ├── FileStorageService.cs             <-- DEV 2
│   ├── TaxLookupService.cs               <-- DEV 2
│   ├── PdfSecurityService.cs             <-- DEV 2
│   └── ContractorAuthService.cs          <-- DEV 2
│
ProcurementSystem.API/
├── Controllers/
│   ├── AuthController.Login.cs           <-- DEV 1 (partial class)
│   └── AuthController.Register.cs        <-- DEV 2 (partial class)
└── uploads/
    └── contractors/                      <-- Thư mục lưu file GPKD
```

---

## 3. NHIỆM VỤ CHI TIẾT - DEV 1: ĐĂNG NHẬP & JWT TOKEN

### 3.1 Nhánh Git làm việc: `feature/auth-login`

#### Bước 1: Tạo DTO Refresh Token
* File: `ProcurementSystem.Core/DTOs/Auth/RefreshTokenRequest.cs`
```csharp
namespace ProcurementSystem.Core.DTOs.Auth
{
    public class RefreshTokenRequest
    {
        public string Token { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
    }
}
```

#### Bước 2: Định nghĩa & Triển khai `ITokenService`
* Interface: `ProcurementSystem.Core/Interfaces/ITokenService.cs`
```csharp
using System.Security.Claims;
using ProcurementSystem.Infrastructure.Entities;

namespace ProcurementSystem.Core.Interfaces
{
    public interface ITokenService
    {
        string GenerateAccessToken(User user, IList<string> roles);
        string GenerateRefreshToken();
        ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
    }
}
```
* Class: `ProcurementSystem.Infrastructure/Services/TokenService.cs`
  * Đọc `JwtSettings` từ `IConfiguration`.
  * Claims nạp vào Token: `ClaimTypes.NameIdentifier` (UserId), `ClaimTypes.Email`, `ClaimTypes.Name`, và `ClaimTypes.Role`.
  * Hạn dùng: theo `ExpirationInMinutes` (60 phút).
  * Refresh Token sinh chuỗi ngẫu nhiên Base64 (64 bytes).

#### Bước 3: Triển khai `IAuthService`
* Interface: `ProcurementSystem.Core/Interfaces/IAuthService.cs`
```csharp
using ProcurementSystem.Core.DTOs;
using ProcurementSystem.Core.DTOs.Auth;

namespace ProcurementSystem.Core.Interfaces
{
    public interface IAuthService
    {
        Task<ApiResponse<LoginResponse>> LoginAsync(LoginRequest request);
        Task<ApiResponse<LoginResponse>> RefreshTokenAsync(RefreshTokenRequest request);
        Task<ApiResponse<bool>> LogoutAsync(int userId);
    }
}
```
* Logic `AuthService.cs`:
  1. Kiểm tra Email tồn tại, `user.IsActive == true`.
  2. Xác thực mật khẩu: `BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash)`.
  3. Cập nhật `RefreshToken` và `RefreshTokenExpiry = DateTime.UtcNow.AddDays(7)` vào CSDL.
  4. Trả về `LoginResponse` chứa token và thông tin user.

#### Bước 4: Tạo Controller `AuthController.Login.cs`
* File: `ProcurementSystem.API/Controllers/AuthController.Login.cs`
```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProcurementSystem.Core.DTOs;
using ProcurementSystem.Core.DTOs.Auth;
using ProcurementSystem.Core.Interfaces;
using System.Security.Claims;

namespace ProcurementSystem.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public partial class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("login")]
        public async Task<ActionResult<ApiResponse<LoginResponse>>> Login([FromBody] LoginRequest request)
        {
            var result = await _authService.LoginAsync(request);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        [HttpPost("refresh-token")]
        public async Task<ActionResult<ApiResponse<LoginResponse>>> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            var result = await _authService.RefreshTokenAsync(request);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        [Authorize]
        [HttpPost("logout")]
        public async Task<ActionResult<ApiResponse<bool>>> Logout()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim, out int userId))
                return Unauthorized(ApiResponse<bool>.Fail("Không xác định được người dùng."));

            var result = await _authService.LogoutAsync(userId);
            return Ok(result);
        }
    }
}
```

---

## 4. NHIỆM VỤ CHI TIẾT - DEV 2: ĐĂNG KÝ NHÀ THẦU & THẨM ĐỊNH HỒ SƠ PHÁP LÝ

### 4.1 Nhánh Git làm việc: `feature/auth-register-contractor`

### 4.2 Cài đặt thư viện xử lý PDF (Cho tính năng Chữ ký số)
Mở Terminal tại thư mục `Procurement & Contract Tracking System` và chạy:
```bash
dotnet add ProcurementSystem.Infrastructure package itext7
dotnet add ProcurementSystem.Infrastructure package itext7.bouncy-castle-adapter
```

---

### 4.3 Tạo các DTOs
* File: `ProcurementSystem.Core/DTOs/Auth/RegisterContractorRequest.cs`
```csharp
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace ProcurementSystem.Core.DTOs.Auth
{
    public class RegisterContractorRequest
    {
        [Required(ErrorMessage = "Họ tên người đại diện là bắt buộc")]
        [MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email là bắt buộc")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
        [MinLength(6, ErrorMessage = "Mật khẩu tối thiểu 6 ký tự")]
        public string Password { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        public string? Phone { get; set; }

        [Required(ErrorMessage = "Tên công ty là bắt buộc")]
        [MaxLength(200)]
        public string CompanyName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mã số thuế là bắt buộc")]
        [MaxLength(20)]
        public string TaxCode { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Address { get; set; }

        /// <summary>
        /// File Giấy phép kinh doanh (PDF, JPG, PNG - Tối đa 10MB)
        /// </summary>
        [Required(ErrorMessage = "Giấy phép kinh doanh là bắt buộc")]
        public IFormFile BusinessLicenseFile { get; set; } = null!;
    }

    public class ContractorRegisterResponse
    {
        public int UserId { get; set; }
        public int ContractorId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string TaxCode { get; set; } = string.Empty;
        public string BusinessLicenseUrl { get; set; } = string.Empty;
        
        // Kết quả thẩm định tự động
        public bool IsTaxCodeVerified { get; set; }
        public string TaxOfficialName { get; set; } = string.Empty;
        public bool IsDigitallySigned { get; set; }
        public string? SignerInfo { get; set; }
        public string VerificationMessage { get; set; } = string.Empty;
    }
}
```

---

## 5. HƯỚNG DẪN CHI TIẾT: TRA CỨU API THUẾ & KIỂM TRA CHỮ KÝ SỐ PDF (CHO DEV 2)

### 5.1 GIẢI PHÁP 1: Tra cứu Mã số thuế qua API VietQR Doanh Nghiệp

#### A. Giới thiệu API
* **Endpoint**: `GET https://api.vietqr.io/v2/business/{taxCode}`
* **Chi phí**: Hoàn toàn **Miễn phí**, không cần đăng ký tài khoản / API Key.
* **Payload kết quả thành công (`code == "00"`)**:
```json
{
  "code": "00",
  "desc": "success",
  "data": {
    "id": "0312345678",
    "name": "CÔNG TY TNHH XÂY DỰNG VÀ ĐẦU TƯ ABC",
    "shortName": "ABC CONST CO.",
    "address": "123 Đường Nguyễn Trãi, Phường 2, Quận 5, TP Hồ Chí Minh"
  }
}
```
* **Payload khi Mã số thuế không tồn tại**: `code != "00"` hoặc `data == null`.

#### B. Định nghĩa & Triển khai Service
* Interface: `ProcurementSystem.Core/Interfaces/ITaxLookupService.cs`
```csharp
namespace ProcurementSystem.Core.Interfaces
{
    public class TaxBusinessData
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? ShortName { get; set; }
        public string? Address { get; set; }
    }

    public class TaxApiResponse
    {
        public string Code { get; set; } = string.Empty;
        public string Desc { get; set; } = string.Empty;
        public TaxBusinessData? Data { get; set; }
    }

    public interface ITaxLookupService
    {
        Task<TaxBusinessData?> VerifyTaxCodeAsync(string taxCode);
    }
}
```

* Class: `ProcurementSystem.Infrastructure/Services/TaxLookupService.cs`
```csharp
using System.Net.Http.Json;
using ProcurementSystem.Core.Interfaces;

namespace ProcurementSystem.Infrastructure.Services
{
    public class TaxLookupService : ITaxLookupService
    {
        private readonly HttpClient _httpClient;

        public TaxLookupService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _httpClient.BaseAddress = new Uri("https://api.vietqr.io/v2/");
            _httpClient.Timeout = TimeSpan.FromSeconds(10);
        }

        public async Task<TaxBusinessData?> VerifyTaxCodeAsync(string taxCode)
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<TaxApiResponse>($"business/{taxCode.Trim()}");
                if (response != null && response.Code == "00" && response.Data != null)
                {
                    return response.Data;
                }
            }
            catch
            {
                // Xử lý timeout hoặc lỗi mạng
            }
            return null;
        }
    }
}
```

---

### 5.2 GIẢI PHÁP 2: Kiểm tra Chữ ký số trên file PDF (PAdES)

#### A. Định nghĩa & Triển khai Service
* Interface: `ProcurementSystem.Core/Interfaces/IPdfSecurityService.cs`
```csharp
namespace ProcurementSystem.Core.Interfaces
{
    public class PdfSignatureResult
    {
        public bool IsSigned { get; set; }
        public bool IsIntegrityValid { get; set; }
        public string? SignerName { get; set; }
        public DateTime? SignDate { get; set; }
        public string SummaryMessage { get; set; } = string.Empty;
    }

    public interface IPdfSecurityService
    {
        PdfSignatureResult InspectPdfSignature(Stream pdfStream);
    }
}
```

* Class: `ProcurementSystem.Infrastructure/Services/PdfSecurityService.cs`
```csharp
using iText.Kernel.Pdf;
using iText.Signatures;
using ProcurementSystem.Core.Interfaces;

namespace ProcurementSystem.Infrastructure.Services
{
    public class PdfSecurityService : IPdfSecurityService
    {
        public PdfSignatureResult InspectPdfSignature(Stream pdfStream)
        {
            var result = new PdfSignatureResult();
            try
            {
                using var reader = new PdfReader(pdfStream);
                using var pdfDoc = new PdfDocument(reader);
                var signUtil = new SignatureUtil(pdfDoc);
                var names = signUtil.GetSignatureNames();

                if (names == null || names.Count == 0)
                {
                    result.IsSigned = false;
                    result.SummaryMessage = "Bản scan thông thường (Không có chữ ký số điện tử).";
                    return result;
                }

                // Có chữ ký số -> kiểm tra tính toàn vẹn của chữ ký đầu tiên
                string firstSign = names[0];
                PdfPKCS7 pkcs7 = signUtil.ReadSignatureData(firstSign);
                bool isValid = pkcs7.VerifySignatureIntegrityAndAuthenticity();

                result.IsSigned = true;
                result.IsIntegrityValid = isValid;
                result.SignDate = pkcs7.GetSignDate();
                
                var cert = pkcs7.GetSigningCertificate();
                result.SignerName = cert.GetSubjectDN().ToString();
                result.SummaryMessage = isValid
                    ? $"Chữ ký số hợp lệ và toàn vẹn. Đơn vị ký: {result.SignerName}"
                    : "CẢNH BÁO: Chữ ký số không hợp lệ hoặc tài liệu đã bị can thiệp/sửa đổi sau khi ký!";
            }
            catch (Exception ex)
            {
                result.IsSigned = false;
                result.SummaryMessage = "Không thể phân tích chữ ký: " + ex.Message;
            }

            return result;
        }
    }
}
```

---

### 5.3 Triển khai `FileStorageService`
* Interface: `ProcurementSystem.Core/Interfaces/IFileStorageService.cs`
```csharp
using Microsoft.AspNetCore.Http;

namespace ProcurementSystem.Core.Interfaces
{
    public interface IFileStorageService
    {
        Task<string> SaveFileAsync(IFormFile file, string subFolder);
        void DeleteFile(string relativePath);
    }
}
```
* Class: `ProcurementSystem.Infrastructure/Services/FileStorageService.cs`
```csharp
using Microsoft.AspNetCore.Http;
using ProcurementSystem.Core.Interfaces;

namespace ProcurementSystem.Infrastructure.Services
{
    public class FileStorageService : IFileStorageService
    {
        private readonly string _baseUploadPath;
        private readonly string[] _allowedExtensions = { ".pdf", ".jpg", ".jpeg", ".png" };
        private const long MaxFileSize = 10 * 1024 * 1024; // 10MB

        public FileStorageService()
        {
            _baseUploadPath = Path.Combine(Directory.GetCurrentDirectory(), "uploads");
        }

        public async Task<string> SaveFileAsync(IFormFile file, string subFolder)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File không hợp lệ.");

            if (file.Length > MaxFileSize)
                throw new InvalidOperationException("Kích thước file vượt quá 10MB.");

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!_allowedExtensions.Contains(ext))
                throw new InvalidOperationException("Định dạng file không được phép. Chỉ chấp nhận .pdf, .jpg, .jpeg, .png.");

            var folderPath = Path.Combine(_baseUploadPath, subFolder);
            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            var uniqueFileName = $"{Guid.NewGuid()}{ext}";
            var fullPath = Path.Combine(folderPath, uniqueFileName);

            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return $"/uploads/{subFolder}/{uniqueFileName}";
        }

        public void DeleteFile(string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath)) return;
            var fullPath = Path.Combine(Directory.GetCurrentDirectory(), relativePath.TrimStart('/'));
            if (File.Exists(fullPath)) File.Delete(fullPath);
        }
    }
}
```

---

### 5.4 Tổng hợp Logic trong `ContractorAuthService.cs`
* Interface: `ProcurementSystem.Core/Interfaces/IContractorAuthService.cs`
```csharp
using ProcurementSystem.Core.DTOs;
using ProcurementSystem.Core.DTOs.Auth;

namespace ProcurementSystem.Core.Interfaces
{
    public interface IContractorAuthService
    {
        Task<ApiResponse<ContractorRegisterResponse>> RegisterContractorAsync(RegisterContractorRequest request);
    }
}
```

* Quy trình trong `ContractorAuthService.cs`:
  1. **Check trùng Email**: `await _unitOfWork.Users.GetFirstOrDefaultAsync(u => u.Email == request.Email)`.
  2. **Check trùng MST trong DB**: `await _unitOfWork.Contractors.GetFirstOrDefaultAsync(c => c.TaxCode == request.TaxCode)`.
  3. **GỌI API THUẾ (Giải pháp 1)**:
     ```csharp
     var taxInfo = await _taxLookupService.VerifyTaxCodeAsync(request.TaxCode);
     if (taxInfo == null)
     {
         return ApiResponse<ContractorRegisterResponse>.Fail("Mã số thuế không tồn tại hoặc không hợp lệ trên Cổng thông tin Doanh nghiệp Quốc gia.");
     }
     ```
  4. **KIỂM TRA CHỮ KÝ SỐ (Giải pháp 2)**:
     ```csharp
     bool isSigned = false;
     string signerInfo = null;
     if (Path.GetExtension(request.BusinessLicenseFile.FileName).Equals(".pdf", StringComparison.OrdinalIgnoreCase))
     {
         using var stream = request.BusinessLicenseFile.OpenReadStream();
         var signResult = _pdfSecurityService.InspectPdfSignature(stream);
         isSigned = signResult.IsSigned && signResult.IsIntegrityValid;
         signerInfo = signResult.SignerName;
     }
     ```
  5. **Lưu File**: `string fileUrl = await _fileStorageService.SaveFileAsync(request.BusinessLicenseFile, "contractors");`
  6. **Mở Transaction**:
     * Tạo `User` (băm mật khẩu BCrypt, RoleId = 4 `Contractor`).
     * Tạo `Contractor` liên kết `UserId`, lưu `fileUrl`.
     * Commit Transaction.
  7. Trả về `ContractorRegisterResponse` với đầy đủ kết quả thẩm định.

---

### 5.5 Tạo Controller `AuthController.Register.cs`
* File: `ProcurementSystem.API/Controllers/AuthController.Register.cs`
```csharp
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProcurementSystem.Core.DTOs;
using ProcurementSystem.Core.DTOs.Auth;
using ProcurementSystem.Core.Interfaces;

namespace ProcurementSystem.API.Controllers
{
    public partial class AuthController
    {
        [HttpPost("register-contractor")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<ApiResponse<ContractorRegisterResponse>>> RegisterContractor(
            [FromForm] RegisterContractorRequest request,
            [FromServices] IContractorAuthService contractorAuthService)
        {
            var result = await contractorAuthService.RegisterContractorAsync(request);
            if (!result.Success) return BadRequest(result);
            return StatusCode(StatusCodes.Status201Created, result);
        }
    }
}
```

---

## 6. QUY TRÌNH GIT & HỢP NHẤT MÃ NGUỒN

### 6.1 Khởi tạo nhánh
* **DEV 1**:
  ```bash
  git checkout dev
  git pull origin dev
  git checkout -b feature/auth-login
  ```
* **DEV 2**:
  ```bash
  git checkout dev
  git pull origin dev
  git checkout -b feature/auth-register-contractor
  ```

---

### 6.2 Đăng ký Service vào `Program.cs`
Tại vị trí `// TODO: Register your services here` trong `Program.cs`:

* **DEV 1 thêm:**
  ```csharp
  builder.Services.AddScoped<ITokenService, TokenService>();
  builder.Services.AddScoped<IAuthService, AuthService>();
  ```

* **DEV 2 thêm:**
  ```csharp
  builder.Services.AddHttpClient<ITaxLookupService, TaxLookupService>();
  builder.Services.AddScoped<IPdfSecurityService, PdfSecurityService>();
  builder.Services.AddScoped<IFileStorageService, FileStorageService>();
  builder.Services.AddScoped<IContractorAuthService, ContractorAuthService>();
  ```

---

### 6.3 Hợp nhất (Merge) lên GitHub
1. DEV 1 xong trước $\rightarrow$ Commit & Push lên `feature/auth-login` $\rightarrow$ Merge vào `dev`.
2. DEV 2 xong sau $\rightarrow$ Commit trên máy mình:
   ```bash
   git add .
   git commit -m "feat(contractor): implement contractor registration with tax API check and PDF signature verification"
   ```
3. DEV 2 kéo code mới từ `dev`:
   ```bash
   git fetch origin
   git merge origin/dev
   ```
4. Kiểm tra file `Program.cs`, giữ lại cả các dòng đăng ký DI của 2 người $\rightarrow$ Chạy `dotnet build` để đảm bảo 0 lỗi.
5. Push và tạo PR merge vào `dev`.

---

## 7. KỊCH BẢN KIỂM THỬ TÍCH HỢP

### Test Case 1: Đăng ký với Mã số thuế BỊA ĐẶT (Kỳ vọng: Bị chặn)
* Nhập MST: `9999999999` (Mã không có thật).
* **Kết quả**: Backend trả về HTTP 400 kèm thông báo: *"Mã số thuế không tồn tại hoặc không hợp lệ trên Cổng thông tin Doanh nghiệp Quốc gia."*

### Test Case 2: Đăng ký với Mã số thuế THẬT (Kỳ vọng: Thành công)
* Nhập MST thật: Ví dụ `0312345678` (hoặc mã số thuế của một công ty thật ở Việt Nam như Vinamilk `0300588569`, FPT `0101248141`).
* Upload file GPKD (ảnh hoặc PDF).
* **Kết quả**: HTTP 201 Created. Backend tự động trả về tên chính thức theo cơ quan Thuế và cờ kiểm tra chữ ký số `isDigitallySigned`.

### Test Case 3: Đăng nhập bằng tài khoản nhà thầu vừa đăng ký
* Dùng Email và Mật khẩu vừa tạo gọi `POST /api/auth/login`.
* **Kết quả**: HTTP 200 OK, trả về Access Token JWT và Refresh Token.
