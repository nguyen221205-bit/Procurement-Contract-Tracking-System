using ProcurementSystem.Core.DTOs;
using ProcurementSystem.Core.DTOs.User;

namespace ProcurementSystem.Core.Interfaces
{
    public interface IUserService
    {
        Task<ApiResponse<PaginatedList<UserDto>>> GetUsersAsync(UserFilterParams filter);
        Task<ApiResponse<UserDto>> GetUserByIdAsync(int id);
        Task<ApiResponse<UserDto>> UpdateUserAsync(int id, UpdateUserRequest request);
        Task<ApiResponse<bool>> ToggleUserStatusAsync(int id, int currentUserId);
        Task<ApiResponse<UserDto>> AssignRolesAsync(int id, AssignRolesRequest request);
        Task<ApiResponse<List<RoleDto>>> GetRolesAsync();
    }
}
