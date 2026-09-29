using Microsoft.EntityFrameworkCore;
using ProcurementSystem.Core.DTOs;
using ProcurementSystem.Core.DTOs.User;
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

            user.IsActive = !user.IsActive;
            user.UpdatedAt = DateTime.UtcNow;

            // Nếu khóa tài khoản thì đồng thời thu hồi token đăng nhập
            if (!user.IsActive)
            {
                user.RefreshToken = null;
                user.RefreshTokenExpiry = null;
            }

            _unitOfWork.Repository<User>().Update(user);
            await _unitOfWork.SaveChangesAsync();

            var message = user.IsActive ? "Đã kích hoạt tài khoản thành công." : "Đã khóa tài khoản thành công.";
            return ApiResponse<bool>.Ok(user.IsActive, message);
        }

        public async Task<ApiResponse<UserDto>> AssignRolesAsync(int id, AssignRolesRequest request)
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
