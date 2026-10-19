using Microsoft.EntityFrameworkCore;
using ProcurementSystem.Core.DTOs;
using ProcurementSystem.Core.DTOs.User;
using ProcurementSystem.Core.Enums;
using ProcurementSystem.Core.Interfaces;
using ProcurementSystem.Infrastructure.Entities;

using System.Security.Cryptography;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace ProcurementSystem.Infrastructure.Services
{
    public class UserService : IUserService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEmailService _emailService;

        public UserService(IUnitOfWork unitOfWork, IEmailService emailService)
        {
            _unitOfWork = unitOfWork;
            _emailService = emailService;
        }

        public async Task<ApiResponse<PaginatedList<UserDto>>> GetUsersAsync(UserFilterParams filter)
        {
            var query = _unitOfWork.Repository<User>()
                .Query()
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .Include(u => u.Contractor)
                .AsNoTracking();

            // Tìm kiếm từ khóa theo Tên, Email hoặc Số điện thoại
            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var search = filter.Search.Trim().ToLower();
                query = query.Where(u => u.FullName.ToLower().Contains(search)
                                      || u.Email.ToLower().Contains(search)
                                      || (u.Phone != null && u.Phone.Contains(search)));
            }

            // Lọc theo Role
            if (!string.IsNullOrWhiteSpace(filter.Role))
            {
                var role = filter.Role.Trim().ToLower();
                query = query.Where(u => u.UserRoles.Any(ur => ur.Role.Name.ToLower() == role));
            }

            // Lọc theo trạng thái hoạt động
            if (filter.IsActive.HasValue)
            {
                query = query.Where(u => u.IsActive == filter.IsActive.Value);
            }

            // Sắp xếp người mới nhất lên đầu
            query = query.OrderByDescending(u => u.CreatedAt);

            var totalCount = await query.CountAsync();
            var pageIndex = filter.PageIndex < 1 ? 1 : filter.PageIndex;
            var pageSize = filter.PageSize < 1 ? 10 : (filter.PageSize > 100 ? 100 : filter.PageSize);

            var users = await query.Skip((pageIndex - 1) * pageSize)
                                   .Take(pageSize)
                                   .ToListAsync();

            var userDtos = users.Select(u => MapToUserDto(u)).ToList();
            var result = new PaginatedList<UserDto>(userDtos, totalCount, pageIndex, pageSize);

            return ApiResponse<PaginatedList<UserDto>>.Ok(result, "Lấy danh sách người dùng thành công.");
        }

        public async Task<ApiResponse<UserDto>> GetUserByIdAsync(int id)
        {
            var user = await _unitOfWork.Repository<User>()
                .Query()
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .Include(u => u.Contractor)
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return ApiResponse<UserDto>.Fail("Không tìm thấy người dùng với mã ID đã cung cấp.");
            }

            return ApiResponse<UserDto>.Ok(MapToUserDto(user), "Lấy thông tin người dùng thành công.");
        }

        public async Task<ApiResponse<UserDto>> UpdateUserAsync(int id, UpdateUserRequest request)
        {
            var user = await _unitOfWork.Repository<User>()
                .Query()
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .Include(u => u.Contractor)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return ApiResponse<UserDto>.Fail("Không tìm thấy người dùng cần cập nhật.");
            }

            user.FullName = request.FullName.Trim();
            user.Phone = request.Phone?.Trim();
            user.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Repository<User>().Update(user);
            await _unitOfWork.SaveChangesAsync();

            return ApiResponse<UserDto>.Ok(MapToUserDto(user), "Cập nhật thông tin người dùng thành công.");
        }

        public async Task<ApiResponse<bool>> ToggleUserStatusAsync(int id, int currentUserId)
        {
            if (id == currentUserId)
            {
                return ApiResponse<bool>.Fail("Quản trị viên không thể tự khóa tài khoản của chính mình.");
            }

            var user = await _unitOfWork.Repository<User>().GetByIdAsync(id);
            if (user == null)
            {
                return ApiResponse<bool>.Fail("Không tìm thấy người dùng.");
            }

            // Nếu chuẩn bị khóa tài khoản (user đang Active -> Inactive)
            if (user.IsActive)
            {
                // Bảo vệ hội đồng: Không cho phép khóa tài khoản chuyên gia đang hoạt động trong gói thầu Open hoặc Evaluating
                var isAssignedToActivePackage = await _unitOfWork.Repository<BidPackageEvaluator>()
                    .Query()
                    .Include(bpe => bpe.BidPackage)
                    .AnyAsync(bpe => bpe.EvaluatorId == id &&
                        (bpe.BidPackage.Status == BidPackageStatus.Open || bpe.BidPackage.Status == BidPackageStatus.Evaluating));

                if (isAssignedToActivePackage)
                {
                    return ApiResponse<bool>.Fail(
                        "Không thể khóa tài khoản này vì người dùng đang là thành viên Tổ chuyên gia của gói thầu đang mở thầu hoặc đang chấm điểm. Vui lòng thay thế chuyên gia trong các gói thầu liên quan trước khi khóa tài khoản.");
                }
            }

            var previousStatus = user.IsActive;
            user.IsActive = !user.IsActive;
            user.UpdatedAt = DateTime.UtcNow;

            // Nếu khóa tài khoản thì đồng thời thu hồi token đăng nhập
            if (!user.IsActive)
            {
                user.RefreshToken = null;
                user.RefreshTokenExpiry = null;
            }

            _unitOfWork.Repository<User>().Update(user);

            // Ghi nhận AuditLog TOGGLE_USER_STATUS
            var auditLog = new AuditLog
            {
                UserId = currentUserId,
                Action = "TOGGLE_USER_STATUS",
                EntityType = "User",
                EntityId = user.Id,
                OldValues = System.Text.Json.JsonSerializer.Serialize(new { IsActive = previousStatus }),
                NewValues = System.Text.Json.JsonSerializer.Serialize(new { IsActive = user.IsActive }),
                Timestamp = DateTime.UtcNow
            };
            await _unitOfWork.Repository<AuditLog>().AddAsync(auditLog);

            await _unitOfWork.SaveChangesAsync();

            var message = user.IsActive ? "Đã kích hoạt tài khoản thành công." : "Đã khóa tài khoản thành công.";
            return ApiResponse<bool>.Ok(user.IsActive, message);
        }

        public async Task<ApiResponse<UserDto>> AssignRolesAsync(int id, AssignRolesRequest request, int currentUserId)
        {
            var user = await _unitOfWork.Repository<User>()
                .Query()
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .Include(u => u.Contractor)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return ApiResponse<UserDto>.Fail("Không tìm thấy người dùng.");
            }

            var allRoles = (await _unitOfWork.Repository<Role>().GetAllAsync()).ToList();

            // Kiểm tra các vai trò gửi lên có hợp lệ trong CSDL không
            var requestedRoles = request.Roles.Select(r => r.Trim().ToLower()).Distinct().ToList();
            var matchedRoles = allRoles.Where(r => requestedRoles.Contains(r.Name.ToLower())).ToList();

            if (matchedRoles.Count != requestedRoles.Count)
            {
                return ApiResponse<UserDto>.Fail("Một hoặc nhiều vai trò yêu cầu không tồn tại trong hệ thống.");
            }

            // 1. Kiểm tra xung đột lợi ích: Không cho phép gán Contractor cùng các vai trò nội bộ (Admin, Procurement, Evaluator)
            var hasContractor = requestedRoles.Contains("contractor");
            var hasInternalRoles = requestedRoles.Contains("admin") || requestedRoles.Contains("procurement") || requestedRoles.Contains("evaluator");
            if (hasContractor && hasInternalRoles)
            {
                return ApiResponse<UserDto>.Fail(
                    "Xung đột lợi ích (Căn cứ Luật Đấu thầu 2023): Không thể gán đồng thời vai trò Nhà thầu (Contractor) và các vai trò quản trị/chấm thầu nội bộ (Admin, Procurement, Evaluator).");
            }

            // Nếu tài khoản đã liên kết với hồ sơ Contractor, không cho phép gán vai trò nội bộ
            if (user.Contractor != null && hasInternalRoles)
            {
                return ApiResponse<UserDto>.Fail(
                    "Tài khoản này đã liên kết với hồ sơ Nhà thầu doanh nghiệp. Để đảm bảo tính minh bạch và tránh xung đột lợi ích, không được phép gán các vai trò nội bộ (Admin, Procurement, Evaluator).");
            }

            // 2. Chặn Admin tự hạ quyền của chính mình
            if (id == currentUserId && !requestedRoles.Contains("admin"))
            {
                return ApiResponse<UserDto>.Fail("Quản trị viên không thể tự gỡ bỏ vai trò Quản trị viên (Admin) của chính mình.");
            }

            // 3. Bảo vệ hội đồng: Nếu đang gỡ vai trò Evaluator, kiểm tra xem có đang nằm trong tổ chuyên gia của gói thầu Open/Evaluating không
            var currentlyHasEvaluator = user.UserRoles.Any(ur => ur.Role.Name.Equals("evaluator", StringComparison.OrdinalIgnoreCase));
            if (currentlyHasEvaluator && !requestedRoles.Contains("evaluator"))
            {
                var isAssignedToActivePackage = await _unitOfWork.Repository<BidPackageEvaluator>()
                    .Query()
                    .Include(bpe => bpe.BidPackage)
                    .AnyAsync(bpe => bpe.EvaluatorId == id &&
                        (bpe.BidPackage.Status == BidPackageStatus.Open || bpe.BidPackage.Status == BidPackageStatus.Evaluating));

                if (isAssignedToActivePackage)
                {
                    return ApiResponse<UserDto>.Fail(
                        "Không thể thu hồi vai trò Chuyên gia (Evaluator) vì người dùng này đang là thành viên Tổ chuyên gia của gói thầu đang mở thầu hoặc đang chấm điểm. Vui lòng thay thế chuyên gia trong gói thầu trước khi điều chỉnh vai trò.");
                }
            }

            // Thu thập vai trò cũ cho AuditLog
            var oldRoles = user.UserRoles.Select(ur => ur.Role.Name).ToList();

            // Xóa toàn bộ vai trò hiện tại của người dùng
            var currentUserRoles = user.UserRoles.ToList();
            foreach (var ur in currentUserRoles)
            {
                _unitOfWork.Repository<UserRole>().Delete(ur);
            }

            // Gán các vai trò mới
            foreach (var role in matchedRoles)
            {
                await _unitOfWork.Repository<UserRole>().AddAsync(new UserRole
                {
                    UserId = user.Id,
                    RoleId = role.Id
                });
            }

            user.UpdatedAt = DateTime.UtcNow;

            // 4. Ghi nhận AuditLog ASSIGN_USER_ROLES
            var auditLog = new AuditLog
            {
                UserId = currentUserId,
                Action = "ASSIGN_USER_ROLES",
                EntityType = "User",
                EntityId = user.Id,
                OldValues = System.Text.Json.JsonSerializer.Serialize(new { Roles = oldRoles }),
                NewValues = System.Text.Json.JsonSerializer.Serialize(new { Roles = matchedRoles.Select(r => r.Name).ToList() }),
                Timestamp = DateTime.UtcNow
            };
            await _unitOfWork.Repository<AuditLog>().AddAsync(auditLog);

            await _unitOfWork.SaveChangesAsync();

            // Tải lại thông tin người dùng với roles mới
            var updatedUser = await _unitOfWork.Repository<User>()
                .Query()
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .Include(u => u.Contractor)
                .AsNoTracking()
                .FirstAsync(u => u.Id == id);

            return ApiResponse<UserDto>.Ok(MapToUserDto(updatedUser), "Gán vai trò cho người dùng thành công.");
        }

        public async Task<ApiResponse<List<RoleDto>>> GetRolesAsync()
        {
            var roles = await _unitOfWork.Repository<Role>().GetAllAsync();
            var dtos = roles.Select(r => new RoleDto
            {
                Id = r.Id,
                Name = r.Name,
                Description = r.Description
            }).ToList();

            return ApiResponse<List<RoleDto>>.Ok(dtos, "Lấy danh mục vai trò thành công.");
        }

        public async Task<ApiResponse<ResetPasswordResponseDto>> ResetPasswordAsync(int id, ResetPasswordRequest? request, int currentUserId)
        {
            var user = await _unitOfWork.Repository<User>().GetByIdAsync(id);
            if (user == null)
            {
                return ApiResponse<ResetPasswordResponseDto>.Fail("Không tìm thấy người dùng với mã ID đã cung cấp.");
            }

            // 1. Xác định mật khẩu mới
            string newPassword;
            if (!string.IsNullOrWhiteSpace(request?.NewPassword))
            {
                var inputPassword = request.NewPassword.Trim();
                if (inputPassword.Length < 8)
                {
                    return ApiResponse<ResetPasswordResponseDto>.Fail("Mật khẩu mới phải có độ dài tối thiểu 8 ký tự.");
                }
                newPassword = inputPassword;
            }
            else
            {
                newPassword = GenerateSecureRandomPassword(10);
            }

            // 2. Hash mật khẩu và cập nhật trạng thái bảo mật
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            user.MustChangePassword = false; // Cho phép người dùng đăng nhập làm việc ngay bằng mật khẩu admin cấp
            user.RefreshToken = null;
            user.RefreshTokenExpiry = null;
            user.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Repository<User>().Update(user);

            // 3. Ghi AuditLog (TUYỆT ĐỐI KHÔNG ghi mật khẩu thô hay hash vào OldValues/NewValues)
            var auditLog = new AuditLog
            {
                UserId = currentUserId > 0 ? currentUserId : null,
                Action = "RESET_PASSWORD",
                EntityType = "User",
                EntityId = user.Id,
                OldValues = null,
                NewValues = JsonSerializer.Serialize(new { MustChangePassword = false, ResetByAdmin = true }),
                Timestamp = DateTime.UtcNow
            };
            await _unitOfWork.Repository<AuditLog>().AddAsync(auditLog);

            await _unitOfWork.SaveChangesAsync();

            // 4. Gửi email thông báo mật khẩu mới cho người dùng
            bool emailSent = false;
            try
            {
                var subject = "[Hệ thống Đấu thầu] Thông báo Đặt lại Mật khẩu Tài khoản";
                var body = $@"
                    <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 24px; border: 1px solid #e2e8f0; border-radius: 8px;'>
                        <h2 style='color: #0284c7; margin-top: 0;'>Thông báo Đặt lại Mật khẩu</h2>
                        <p>Xin chào <strong>{HtmlEncoder.Default.Encode(user.FullName)}</strong>,</p>
                        <p>Quản trị viên hệ thống đã đặt lại mật khẩu cho tài khoản của bạn (<strong>{HtmlEncoder.Default.Encode(user.Email)}</strong>).</p>
                        <div style='background: #f1f5f9; padding: 16px; border-radius: 6px; margin: 16px 0;'>
                            <p style='margin: 0; font-size: 14px; color: #475569;'>Mật khẩu mới của bạn là:</p>
                            <p style='margin: 8px 0 0 0; font-size: 18px; font-weight: bold; color: #0f172a; font-family: monospace;'>{HtmlEncoder.Default.Encode(newPassword)}</p>
                        </div>
                        <p style='color: #0369a1; font-size: 13px;'>Bạn có thể sử dụng ngay mật khẩu này để đăng nhập vào hệ thống hoặc đổi sang mật khẩu cá nhân tùy ý.</p>
                        <hr style='border: none; border-top: 1px solid #e2e8f0; margin: 20px 0;' />
                        <p style='font-size: 12px; color: #94a3b8;'>Đây là email tự động từ Hệ thống Quản lý Đấu thầu & Theo dõi Hợp đồng. Vui lòng không trả lời thư này.</p>
                    </div>";

                emailSent = await _emailService.SendEmailAsync(user.Email, subject, body, isHtml: true);
            }
            catch
            {
                // Bỏ qua lỗi gửi email để không làm gián đoạn việc trả về mật khẩu cho Admin
            }

            var response = new ResetPasswordResponseDto
            {
                UserId = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                NewPassword = newPassword,
                MustChangePassword = false,
                EmailSent = emailSent
            };

            return ApiResponse<ResetPasswordResponseDto>.Ok(response, "Đặt lại mật khẩu thành công. Mật khẩu mới đã được cập nhật.");
        }

        private static string GenerateSecureRandomPassword(int length = 10)
        {
            const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
            const string lower = "abcdefghijkmnpqrstuvwxyz";
            const string digits = "23456789";
            const string special = "!@#$%&*";

            var chars = new List<char>
            {
                upper[RandomNumberGenerator.GetInt32(upper.Length)],
                lower[RandomNumberGenerator.GetInt32(lower.Length)],
                digits[RandomNumberGenerator.GetInt32(digits.Length)],
                special[RandomNumberGenerator.GetInt32(special.Length)]
            };

            const string all = upper + lower + digits + special;
            while (chars.Count < length)
            {
                chars.Add(all[RandomNumberGenerator.GetInt32(all.Length)]);
            }

            // Thuật toán Fisher-Yates shuffle sử dụng CSPRNG RandomNumberGenerator.GetInt32
            var array = chars.ToArray();
            for (int i = array.Length - 1; i > 0; i--)
            {
                int j = RandomNumberGenerator.GetInt32(i + 1);
                (array[i], array[j]) = (array[j], array[i]);
            }

            return new string(array);
        }

        private static UserDto MapToUserDto(User user)
        {
            return new UserDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                Phone = user.Phone,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt,
                Roles = user.UserRoles.Select(ur => ur.Role.Name).ToList(),
                Contractor = user.Contractor != null ? new UserContractorSummary
                {
                    ContractorId = user.Contractor.Id,
                    CompanyName = user.Contractor.CompanyName,
                    TaxCode = user.Contractor.TaxCode,
                    Rating = user.Contractor.Rating
                } : null
            };
        }
    }
}
