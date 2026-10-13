using ProcurementSystem.Core.DTOs;
using ProcurementSystem.Core.DTOs.Auth;

namespace ProcurementSystem.Core.Interfaces
{
    public interface IOtpService
    {
        /// <summary>
        /// Tạo và gửi mã OTP 6 số đến email đăng ký (có cooldown 60s và thời hạn 5 phút)
        /// </summary>
        Task<ApiResponse<SendOtpResponse>> SendOtpAsync(SendOtpRequest request);

        /// <summary>
        /// Xác thực mã OTP 6 số. Nếu hợp lệ thì xóa mã khỏi cache để tránh dùng lại.
        /// </summary>
        bool VerifyOtp(string email, string otpCode, string purpose, out string errorMessage);
    }
}
