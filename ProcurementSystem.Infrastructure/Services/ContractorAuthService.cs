using ProcurementSystem.Core.DTOs;
using ProcurementSystem.Core.DTOs.Auth;
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

        public ContractorAuthService(
            IUnitOfWork unitOfWork,
            ITaxLookupService taxLookupService,
            IPdfSecurityService pdfSecurityService,
            IFileStorageService fileStorageService)
        {
            _unitOfWork = unitOfWork;
            _taxLookupService = taxLookupService;
            _pdfSecurityService = pdfSecurityService;
            _fileStorageService = fileStorageService;
        }

        public async Task<ApiResponse<ContractorRegisterResponse>> RegisterContractorAsync(RegisterContractorRequest request)
        {
            // 1. Kiểm tra email tồn tại
            var emailExists = await _unitOfWork.Repository<User>().ExistsAsync(u => u.Email == request.Email);
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
            string verificationMessage = "Đăng ký thành công.";
            
            var extension = Path.GetExtension(request.BusinessLicenseFile.FileName).ToLowerInvariant();
            if (extension == ".pdf")
            {
                using var stream = request.BusinessLicenseFile.OpenReadStream();
                var signResult = _pdfSecurityService.InspectPdfSignature(stream);
                isSigned = signResult.IsSigned && signResult.IsIntegrityValid;
                signerInfo = signResult.SignerName;

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
                    FullName = request.FullName,
                    Email = request.Email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                    Phone = request.Phone,
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

                // Tạo Contractor
                var contractor = new Contractor
                {
                    UserId = user.Id,
                    CompanyName = request.CompanyName,
                    TaxCode = request.TaxCode,
                    Address = request.Address,
                    BusinessLicenseFile = fileUrl,
                    Rating = 0
                };
                await _unitOfWork.Repository<Contractor>().AddAsync(contractor);

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();

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
