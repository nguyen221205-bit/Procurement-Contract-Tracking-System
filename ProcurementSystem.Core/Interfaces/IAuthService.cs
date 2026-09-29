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
