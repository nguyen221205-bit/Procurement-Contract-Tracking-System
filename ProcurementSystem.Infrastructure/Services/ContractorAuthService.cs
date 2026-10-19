using Microsoft.EntityFrameworkCore;
using ProcurementSystem.Core.DTOs;
using ProcurementSystem.Core.DTOs.Auth;
using ProcurementSystem.Core.Enums;
using ProcurementSystem.Core.Interfaces;
using ProcurementSystem.Infrastructure.Entities;

namespace ProcurementSystem.Infrastructure.Services
{
    public class ContractorAuthService : IContractorAuthService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITaxLookupService _taxLookupService;
        private readonly IPdfSecurityService _pdfSecurityService;
        private readonly IFileStorageService _fileStorageService;
        private readonly IOtpService _otpService;
        private readonly INotificationService _notificationService;

        public ContractorAuthService(
            IUnitOfWork unitOfWork,
            ITaxLookupService taxLookupService,
            IPdfSecurityService pdfSecurityService,
            IFileStorageService fileStorageService,
            IOtpService otpService,
            INotificationService notificationService)
        {
            _unitOfWork = unitOfWork;
            _taxLookupService = taxLookupService;
            _pdfSecurityService = pdfSecurityService;
            _fileStorageService = fileStorageService;
            _otpService = otpService;
            _notificationService = notificationService;
        }

        public async Task<ApiResponse<ContractorRegisterResponse>> RegisterContractorAsync(RegisterContractorRequest request)
        {
            var normalizedEmail = request.Email.Trim().ToLowerInvariant();

            // 0. Xác thực mã OTP email (cố định purpose RegisterContractor, tránh ghi đè thông báo số lần thử còn lại)
            if (!_otpService.VerifyOtp(normalizedEmail, request.OtpCode, "RegisterContractor", out var otpError))
            {
                return ApiResponse<ContractorRegisterResponse>.Fail(otpError);
            }

            // 1. Kiểm tra email tồn tại
            var emailExists = await _unitOfWork.Repository<User>().ExistsAsync(u => u.Email.ToLower() == normalizedEmail);
            if (emailExists)
            {
                return ApiResponse<ContractorRegisterResponse>.Fail("Email đã được sử dụng.");
            }

            // 2. Kiểm tra mã số thuế trong hệ thống
            var taxExists = await _unitOfWork.Repository<Contractor>().ExistsAsync(c => c.TaxCode == request.TaxCode);
            if (taxExists)
            {
                return ApiResponse<ContractorRegisterResponse>.Fail("Mã số thuế đã được đăng ký trong hệ thống.");
            }

            // 3. Tra cứu mã số thuế qua API Quốc gia (VietQR Doanh nghiệp)
            var taxInfo = await _taxLookupService.VerifyTaxCodeAsync(request.TaxCode);
            if (taxInfo == null)
            {
                return ApiResponse<ContractorRegisterResponse>.Fail("Mã số thuế không tồn tại hoặc không hợp lệ trên Cổng thông tin Doanh nghiệp Quốc gia.");
            }

            // 4. Kiểm tra chữ ký số trên file GPKD (nếu là PDF)
            bool isSigned = false;
            string? signerInfo = null;
            DateTime? signDate = null;
            string verificationMessage = "Đăng ký thành công. Hồ sơ đang chờ Bên mời thầu thẩm định phê duyệt.";
            
            var extension = Path.GetExtension(request.BusinessLicenseFile.FileName).ToLowerInvariant();
            if (extension == ".pdf")
            {
                using var stream = request.BusinessLicenseFile.OpenReadStream();
                var signResult = _pdfSecurityService.InspectPdfSignature(stream);
                isSigned = signResult.IsSigned && signResult.IsIntegrityValid;
                signerInfo = signResult.SignerName;
                signDate = signResult.SignDate;

                if (signResult.IsSigned && !signResult.IsIntegrityValid)
                {
                    return ApiResponse<ContractorRegisterResponse>.Fail("File PDF có chữ ký số không hợp lệ hoặc đã bị chỉnh sửa sau khi ký.");
                }
                
                verificationMessage = signResult.SummaryMessage;
            }

            // 5. Lưu file GPKD
            string fileUrl = string.Empty;
            try
            {
                fileUrl = await _fileStorageService.SaveFileAsync(request.BusinessLicenseFile, "contractors");
            }
            catch (Exception ex)
            {
                return ApiResponse<ContractorRegisterResponse>.Fail($"Lỗi khi lưu file: {ex.Message}");
            }

            // 6. Lưu vào cơ sở dữ liệu với Transaction
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                // Tạo User
                var user = new User
                {
                    FullName = request.FullName.Trim(),
                    Email = normalizedEmail,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                    Phone = request.Phone?.Trim(),
                    IsActive = true
                };

                await _unitOfWork.Repository<User>().AddAsync(user);
                await _unitOfWork.SaveChangesAsync(); // Cần lưu để lấy UserId

                // Gán Role "Contractor"
                var contractorRole = (await _unitOfWork.Repository<Role>().FindAsync(r => r.Name == "Contractor")).FirstOrDefault();
                if (contractorRole == null)
                {
                    throw new Exception("Role 'Contractor' chưa được cấu hình trong hệ thống.");
                }

                var userRole = new UserRole
                {
                    UserId = user.Id,
                    RoleId = contractorRole.Id
                };
                await _unitOfWork.Repository<UserRole>().AddAsync(userRole);

                // Tạo Contractor với VerificationStatus = Pending (Cần Bên mời thầu / Admin thẩm định phê duyệt)
                var contractor = new Contractor
                {
                    UserId = user.Id,
                    CompanyName = request.CompanyName.Trim(),
                    TaxCode = request.TaxCode?.Trim(),
                    Address = request.Address?.Trim(),
                    BusinessLicenseFile = fileUrl,
                    Rating = 0,
                    VerificationStatus = "Pending",
                    IsDigitalSigned = isSigned,
                    DigitalSignerName = signerInfo,
                    DigitalSignDate = signDate,
                    DigitalSignMessage = verificationMessage
                };
                await _unitOfWork.Repository<Contractor>().AddAsync(contractor);

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();

                // 6.1 Gửi thông báo In-app & Email cho Admin và Nhà thầu mới (bọc try-catch độc lập)
                try
                {
                    var adminUserIds = await _unitOfWork.Repository<User>()
                        .Query()
                        .Where(u => u.IsActive && u.UserRoles.Any(ur => ur.Role.Name == "Admin"))
                        .Select(u => u.Id)
                        .ToListAsync();

                    if (adminUserIds.Any())
                    {
                        await _notificationService.CreateBulkNotificationsAsync(
                            adminUserIds,
                            "Hồ sơ Nhà thầu mới chờ thẩm định",
                            $"Nhà thầu {contractor.CompanyName} (MST: {contractor.TaxCode}) vừa hoàn tất đăng ký tài khoản và đang chờ Quản trị viên thẩm định hồ sơ.",
                            NotificationType.Info,
                            sendEmail: true);
                    }

                    await _notificationService.CreateNotificationAsync(
                        user.Id,
                        "Đăng ký tài khoản thành công",
                        "Hồ sơ đăng ký của quý đơn vị đã được tiếp nhận và đang trong quá trình thẩm định. Bạn sẽ nhận được thông báo khi có kết quả.",
                        NotificationType.Info,
                        sendEmail: false);
                }
                catch
                {
                    // Đảm bảo không làm gián đoạn phản hồi đăng ký nếu gửi thông báo gặp sự cố
                }

                // 7. Trả về Response
                var response = new ContractorRegisterResponse
                {
                    UserId = user.Id,
                    ContractorId = contractor.Id,
                    Email = user.Email,
                    CompanyName = contractor.CompanyName,
                    TaxCode = contractor.TaxCode ?? string.Empty,
                    BusinessLicenseUrl = fileUrl,
                    IsTaxCodeVerified = true,
                    TaxOfficialName = taxInfo.Name,
                    IsDigitallySigned = isSigned,
                    SignerInfo = signerInfo,
                    VerificationMessage = verificationMessage
                };

                return ApiResponse<ContractorRegisterResponse>.Ok(response, "Đăng ký nhà thầu thành công.");
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                
                // Xóa file nếu đã lưu thành công nhưng insert DB lỗi
                if (!string.IsNullOrEmpty(fileUrl))
                {
                    _fileStorageService.DeleteFile(fileUrl);
                }
                
                return ApiResponse<ContractorRegisterResponse>.Fail($"Đã xảy ra lỗi hệ thống: {ex.Message}");
            }
        }
    }
}
