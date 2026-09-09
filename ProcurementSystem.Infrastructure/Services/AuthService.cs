using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using ProcurementSystem.Core.DTOs;
using ProcurementSystem.Core.DTOs.Auth;
using ProcurementSystem.Core.Interfaces;
using ProcurementSystem.Infrastructure.Entities;

namespace ProcurementSystem.Infrastructure.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITokenService _tokenService;
        private readonly IConfiguration _configuration;

        public AuthService(IUnitOfWork unitOfWork, ITokenService tokenService, IConfiguration configuration)
        {
            _unitOfWork = unitOfWork;
            _tokenService = tokenService;
            _configuration = configuration;
        }

        public async Task<ApiResponse<LoginResponse>> LoginAsync(LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            {
                return ApiResponse<LoginResponse>.Fail("Email và mật khẩu không được để trống.");
            }

            var normalizedEmail = request.Email.Trim().ToLowerInvariant();

            var user = await _unitOfWork.Repository<User>()
                .Query()
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .Include(u => u.Contractor)
                .FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);

            if (user == null)
            {
                return ApiResponse<LoginResponse>.Fail("Email hoặc mật khẩu không chính xác.");
            }

            if (!user.IsActive)
            {
                return ApiResponse<LoginResponse>.Fail("Tài khoản của bạn đã bị khóa hoặc chưa được kích hoạt.");
            }

            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);
            if (!isPasswordValid)
            {
                return ApiResponse<LoginResponse>.Fail("Email hoặc mật khẩu không chính xác.");
            }

            var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();
            var token = _tokenService.GenerateAccessToken(user.Id, user.Email, user.FullName, roles);
            var refreshToken = _tokenService.GenerateRefreshToken();

            var refreshExpiryDays = int.TryParse(_configuration["JwtSettings:RefreshTokenExpirationInDays"], out var rDays) ? rDays : 7;
            var accessExpiryMinutes = int.TryParse(_configuration["JwtSettings:ExpirationInMinutes"], out var aMins) ? aMins : 60;

            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(refreshExpiryDays);
            user.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Repository<User>().Update(user);
            await _unitOfWork.SaveChangesAsync();

            var response = new LoginResponse
            {
                Token = token,
                RefreshToken = refreshToken,
                Expiration = DateTime.UtcNow.AddMinutes(accessExpiryMinutes),
                User = new UserInfo
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    Email = user.Email,
                    Roles = roles,
                    ContractorId = user.Contractor?.Id
                }
            };

            return ApiResponse<LoginResponse>.Ok(response, "Đăng nhập thành công.");
        }

        public async Task<ApiResponse<LoginResponse>> RefreshTokenAsync(RefreshTokenRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Token) || string.IsNullOrWhiteSpace(request.RefreshToken))
            {
                return ApiResponse<LoginResponse>.Fail("Token và RefreshToken không được để trống.");
            }

            var principal = _tokenService.GetPrincipalFromExpiredToken(request.Token);
            if (principal == null)
            {
                return ApiResponse<LoginResponse>.Fail("Token không hợp lệ.");
            }

            var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return ApiResponse<LoginResponse>.Fail("Không xác định được danh tính người dùng từ token.");
            }

            var user = await _unitOfWork.Repository<User>()
                .Query()
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .Include(u => u.Contractor)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null || !user.IsActive)
            {
                return ApiResponse<LoginResponse>.Fail("Tài khoản không tồn tại hoặc đã bị khóa.");
            }

            if (user.RefreshToken != request.RefreshToken || user.RefreshTokenExpiry == null || user.RefreshTokenExpiry <= DateTime.UtcNow)
            {
                return ApiResponse<LoginResponse>.Fail("RefreshToken không hợp lệ hoặc đã hết hạn. Vui lòng đăng nhập lại.");
            }

            var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();
            var newToken = _tokenService.GenerateAccessToken(user.Id, user.Email, user.FullName, roles);
            var newRefreshToken = _tokenService.GenerateRefreshToken();

            var refreshExpiryDays = int.TryParse(_configuration["JwtSettings:RefreshTokenExpirationInDays"], out var rDays) ? rDays : 7;
            var accessExpiryMinutes = int.TryParse(_configuration["JwtSettings:ExpirationInMinutes"], out var aMins) ? aMins : 60;

            user.RefreshToken = newRefreshToken;
            user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(refreshExpiryDays);
            user.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Repository<User>().Update(user);
            await _unitOfWork.SaveChangesAsync();

            var response = new LoginResponse
            {
                Token = newToken,
                RefreshToken = newRefreshToken,
                Expiration = DateTime.UtcNow.AddMinutes(accessExpiryMinutes),
                User = new UserInfo
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    Email = user.Email,
                    Roles = roles,
                    ContractorId = user.Contractor?.Id
                }
            };

            return ApiResponse<LoginResponse>.Ok(response, "Làm mới token thành công.");
        }

        public async Task<ApiResponse<bool>> LogoutAsync(int userId)
        {
            var user = await _unitOfWork.Repository<User>().GetByIdAsync(userId);
            if (user == null)
            {
                return ApiResponse<bool>.Fail("Người dùng không tồn tại.");
            }

            user.RefreshToken = null;
            user.RefreshTokenExpiry = null;
            user.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Repository<User>().Update(user);
            await _unitOfWork.SaveChangesAsync();

            return ApiResponse<bool>.Ok(true, "Đăng xuất thành công.");
        }
    }
}
