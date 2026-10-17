using Microsoft.EntityFrameworkCore;
using ProcurementSystem.Core.DTOs;
using ProcurementSystem.Core.DTOs.User;
using ProcurementSystem.Core.Enums;
using ProcurementSystem.Core.Interfaces;
using ProcurementSystem.Infrastructure.Entities;

namespace ProcurementSystem.Infrastructure.Services
{
    public class UserService : IUserService
    {
        private readonly IUnitOfWork _unitOfWork;

        public UserService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
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
