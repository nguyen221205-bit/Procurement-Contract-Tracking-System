using Microsoft.EntityFrameworkCore;
using ProcurementSystem.Core.DTOs;
using ProcurementSystem.Core.DTOs.Auth;
using ProcurementSystem.Core.Interfaces;
using ProcurementSystem.Infrastructure.Data;
using ProcurementSystem.Infrastructure.Entities;

namespace ProcurementSystem.Infrastructure.Services
{
    public class ProcuringEntityAuthService : IProcuringEntityAuthService
    {
        private readonly AppDbContext _context;
        private readonly IFileStorageService _fileStorageService;

        public ProcuringEntityAuthService(
            AppDbContext context,
            IFileStorageService fileStorageService)
        {
            _context = context;
            _fileStorageService = fileStorageService;
        }

        public async Task<ApiResponse<ProcuringEntityRegisterResponse>> RegisterProcuringEntityAsync(RegisterProcuringEntityRequest request)
        {
            var normalizedEmail = request.Email.Trim().ToLowerInvariant();

            // 1. Kiểm tra email đã tồn tại trong hệ thống chưa
            var emailExists = await _context.Users.AnyAsync(u => u.Email.ToLower() == normalizedEmail);
            if (emailExists)
            {
                return ApiResponse<ProcuringEntityRegisterResponse>.Fail("Email đã được sử dụng trong hệ thống.");
            }

            // 2. Kiểm tra mã số thuế của đơn vị đã được đăng ký chưa
            var taxExists = await _context.ProcuringEntities.AnyAsync(pe => pe.TaxCode == request.TaxCode.Trim());
            if (taxExists)
            {
                return ApiResponse<ProcuringEntityRegisterResponse>.Fail("Mã số thuế của đơn vị này đã được đăng ký trong hệ thống.");
            }

            // 3. Kiểm tra file Quyết định thành lập / Giấy phép
            var allowedExtensions = new[] { ".pdf", ".jpg", ".jpeg", ".png" };
            var extEst = Path.GetExtension(request.EstablishmentDecisionFile.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(extEst))
            {
                return ApiResponse<ProcuringEntityRegisterResponse>.Fail("File Quyết định thành lập phải có định dạng PDF, JPG hoặc PNG.");
            }

            if (request.EstablishmentDecisionFile.Length > 15 * 1024 * 1024)
            {
                return ApiResponse<ProcuringEntityRegisterResponse>.Fail("Dung lượng file Quyết định thành lập không được vượt quá 15MB.");
            }

            // Kiểm tra file quyết định bổ nhiệm nếu có
            if (request.AppointmentDecisionFile != null)
            {
                var extApp = Path.GetExtension(request.AppointmentDecisionFile.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(extApp))
                {
                    return ApiResponse<ProcuringEntityRegisterResponse>.Fail("File Quyết định bổ nhiệm phải có định dạng PDF, JPG hoặc PNG.");
                }
                if (request.AppointmentDecisionFile.Length > 15 * 1024 * 1024)
                {
                    return ApiResponse<ProcuringEntityRegisterResponse>.Fail("Dung lượng file Quyết định bổ nhiệm không được vượt quá 15MB.");
                }
            }

            // 4. Lưu tệp đính kèm
            string estFileUrl;
            string? appFileUrl = null;
            try
            {
                estFileUrl = await _fileStorageService.SaveFileAsync(request.EstablishmentDecisionFile, "procuring-entities");
                if (request.AppointmentDecisionFile != null)
                {
                    appFileUrl = await _fileStorageService.SaveFileAsync(request.AppointmentDecisionFile, "procuring-entities");
                }
            }
            catch (Exception ex)
            {
                return ApiResponse<ProcuringEntityRegisterResponse>.Fail($"Lỗi khi lưu trữ tài liệu: {ex.Message}");
            }

            // 5. Lưu cơ sở dữ liệu với Transaction
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Tạo User
                var user = new User
                {
                    FullName = request.FullName.Trim(),
                    Email = normalizedEmail,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                    Phone = request.Phone?.Trim(),
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                await _context.Users.AddAsync(user);
                await _context.SaveChangesAsync();

                // Gán vai trò "Procurement"
                var procRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Procurement");
                if (procRole == null)
                {
                    throw new Exception("Vai trò 'Procurement' chưa được cấu hình trong hệ thống.");
                }

                await _context.UserRoles.AddAsync(new UserRole
                {
                    UserId = user.Id,
                    RoleId = procRole.Id
                });

                // Tạo ProcuringEntity với trạng thái Pending chờ Admin thẩm định
                var procuringEntity = new ProcuringEntity
                {
                    UserId = user.Id,
                    OrganizationName = request.OrganizationName.Trim(),
                    OrganizationType = request.OrganizationType.Trim(),
                    TaxCode = request.TaxCode.Trim(),
                    BudgetCode = request.BudgetCode?.Trim(),
                    Address = request.Address?.Trim(),
                    RepresentativeName = request.RepresentativeName.Trim(),
                    RepresentativeTitle = request.RepresentativeTitle.Trim(),
                    RepresentativePhone = request.RepresentativePhone?.Trim() ?? request.Phone?.Trim(),
                    EstablishmentDecisionFile = estFileUrl,
                    AppointmentDecisionFile = appFileUrl,
                    VerificationStatus = "Pending",
                    CreatedAt = DateTime.UtcNow
                };

                await _context.ProcuringEntities.AddAsync(procuringEntity);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                var response = new ProcuringEntityRegisterResponse
                {
                    UserId = user.Id,
                    ProcuringEntityId = procuringEntity.Id,
                    Email = user.Email,
                    OrganizationName = procuringEntity.OrganizationName,
                    OrganizationType = procuringEntity.OrganizationType,
                    TaxCode = procuringEntity.TaxCode,
                    EstablishmentDecisionFileUrl = estFileUrl,
                    AppointmentDecisionFileUrl = appFileUrl,
                    VerificationStatus = "Pending",
                    VerificationMessage = "Đăng ký thành công! Hồ sơ pháp nhân Bên mời thầu của bạn đang chờ Quản trị viên (Admin) thẩm định phê duyệt trước khi tạo gói thầu."
                };

                return ApiResponse<ProcuringEntityRegisterResponse>.Ok(response, "Đăng ký thông tin Bên mời thầu thành công.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

                // Dọn dẹp file vừa lưu nếu lỗi
                if (!string.IsNullOrEmpty(estFileUrl))
                {
                    _fileStorageService.DeleteFile(estFileUrl);
                }
                if (!string.IsNullOrEmpty(appFileUrl))
                {
                    _fileStorageService.DeleteFile(appFileUrl);
                }

                return ApiResponse<ProcuringEntityRegisterResponse>.Fail($"Đã xảy ra lỗi khi tạo tài khoản: {ex.Message}");
            }
        }
    }
}
