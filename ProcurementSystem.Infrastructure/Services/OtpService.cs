using System.Security.Cryptography;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ProcurementSystem.Core.DTOs;
using ProcurementSystem.Core.DTOs.Auth;
using ProcurementSystem.Core.Interfaces;

namespace ProcurementSystem.Infrastructure.Services
{
    public class OtpService : IOtpService
    {
        private readonly IMemoryCache _cache;
        private readonly ILogger<OtpService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IEmailService _emailService;

        private class OtpCacheEntry
        {
            public string Code { get; set; } = string.Empty;
            public int Attempts { get; set; }
            public DateTime CreatedAt { get; set; }
        }

        public OtpService(
            IMemoryCache cache,
            ILogger<OtpService> logger,
            IConfiguration configuration,
            IEmailService emailService)
        {
            _cache = cache;
            _logger = logger;
            _configuration = configuration;
            _emailService = emailService;
        }

        public async Task<ApiResponse<SendOtpResponse>> SendOtpAsync(SendOtpRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email))
            {
                return ApiResponse<SendOtpResponse>.Fail("Email không được để trống.");
            }

            var normalizedEmail = request.Email.Trim().ToLowerInvariant();
            var purpose = string.IsNullOrWhiteSpace(request.Purpose) ? "Register" : request.Purpose.Trim();

            // Kiểm tra cấu hình máy chủ SMTP thực tế
            var smtpHost = _configuration["Smtp:Host"];
            var smtpUser = _configuration["Smtp:Username"];
            var smtpPass = _configuration["Smtp:Password"];
            var isSmtpConfigured = !string.IsNullOrWhiteSpace(smtpHost) &&
                                   !string.IsNullOrWhiteSpace(smtpUser) &&
                                   !string.IsNullOrWhiteSpace(smtpPass);

            if (!isSmtpConfigured)
            {
                _logger.LogWarning("Yêu cầu gửi OTP thất bại: Máy chủ chưa được cấu hình tài khoản gửi email (Smtp:Username hoặc Smtp:Password trong appsettings.json đang để trống).");
                return ApiResponse<SendOtpResponse>.Fail(
                    "Máy chủ chưa được cấu hình tài khoản gửi email (Smtp:Username và Smtp:Password trong appsettings.json đang để trống). Vui lòng cấu hình tài khoản SMTP thực tế để gửi mã xác thực qua email.");
            }

            // Khóa Cooldown gắn chặt với email để ngăn chặn Email Flooding / Cooldown Bypass qua tham số Purpose (SEC-03)
            var cooldownKey = $"OTP_COOLDOWN_{normalizedEmail}";
            if (_cache.TryGetValue(cooldownKey, out DateTime nextAllowedTime))
            {
                var remainingSeconds = Math.Max(1, (int)(nextAllowedTime - DateTime.UtcNow).TotalSeconds);
                return ApiResponse<SendOtpResponse>.Fail($"Vui lòng chờ {remainingSeconds} giây trước khi yêu cầu mã OTP mới.");
            }

            // Sinh mã OTP ngẫu nhiên 6 chữ số bảo mật
            var otpCode = RandomNumberGenerator.GetInt32(100000, 1000000).ToString("D6");
            var otpKey = $"OTP_{purpose}_{normalizedEmail}";
            var otpEntry = new OtpCacheEntry
            {
                Code = otpCode,
                Attempts = 0,
                CreatedAt = DateTime.UtcNow
            };

            // Chuẩn bị nội dung email
            var purposeLabel = purpose.Contains("Procuring", StringComparison.OrdinalIgnoreCase)
                ? "đăng ký tài khoản Bên mời thầu"
                : "đăng ký tài khoản Nhà thầu";
            var subject = "[Hệ thống Đấu thầu & Quản lý Hợp đồng] Mã xác thực OTP đăng ký tài khoản";
            var body = $@"
                <div style='font-family: -apple-system, BlinkMacSystemFont, ""Segoe UI"", Roboto, Helvetica, Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 32px 24px; background: #ffffff; border: 1px solid #e2e8f0; border-radius: 12px; box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.05);'>
                    <div style='text-align: center; margin-bottom: 24px; padding-bottom: 16px; border-bottom: 2px solid #0284c7;'>
                        <h1 style='color: #0369a1; font-size: 20px; font-weight: 800; margin: 0; text-transform: uppercase; letter-spacing: 0.5px;'>
                            HỆ THỐNG ĐẤU THẦU & QUẢN LÝ HỢP ĐỒNG
                        </h1>
                        <p style='color: #64748b; font-size: 13px; margin: 6px 0 0 0;'>Cổng tiếp nhận hồ sơ & thẩm định năng lực trực tuyến</p>
                    </div>
                    
                    <p style='color: #1e293b; font-size: 15px; line-height: 1.6;'>Xin chào,</p>
                    <p style='color: #334155; font-size: 14px; line-height: 1.6;'>
                        Bạn đang thực hiện thao tác <strong>{purposeLabel}</strong> trên Hệ thống Đấu thầu điện tử. Để xác minh quyền sở hữu email, vui lòng sử dụng mã OTP dưới đây:
                    </p>

                    <div style='text-align: center; margin: 28px 0;'>
                        <div style='display: inline-block; font-size: 36px; font-weight: 800; letter-spacing: 10px; color: #0284c7; background: #f0f9ff; padding: 14px 36px; border-radius: 10px; border: 2px dashed #38bdf8;'>
                            {otpCode}
                        </div>
                    </div>

                    <div style='background: #fffbeb; border-left: 4px solid #f59e0b; padding: 12px 16px; border-radius: 4px; margin-bottom: 24px;'>
                        <p style='color: #b45309; font-size: 13px; margin: 0; line-height: 1.5;'>
                            ⏳ <strong>Thời hạn hiệu lực:</strong> Mã OTP có giá trị trong vòng <strong>5 phút</strong>.<br/>
                            🔒 <strong>Bảo mật:</strong> Tuyệt đối không chia sẻ mã này cho bất kỳ ai để bảo vệ tài khoản của bạn.
                        </p>
                    </div>

                    <hr style='border: none; border-top: 1px solid #e2e8f0; margin: 24px 0;' />
                    <p style='font-size: 12px; color: #94a3b8; text-align: center; margin: 0;'>
                        Nếu bạn không yêu cầu mã này, vui lòng bỏ qua thư.<br/>
                        Đây là email tự động gửi từ máy chủ hệ thống, vui lòng không phản hồi thư này.
                    </p>
                </div>";

            // Gửi email thực tế qua IEmailService
            var emailSent = await _emailService.SendEmailAsync(normalizedEmail, subject, body, isHtml: true);
            if (!emailSent)
            {
                _logger.LogError("Không thể gửi email OTP qua máy chủ SMTP tới địa chỉ: {Email}", normalizedEmail);
                return ApiResponse<SendOtpResponse>.Fail(
                    "Không thể gửi email OTP qua máy chủ SMTP. Vui lòng kiểm tra lại cấu hình tài khoản gửi thư (Username/Password hoặc kết nối mạng).");
            }

            // Chỉ lưu cache khi gửi email thành công
            _cache.Set(otpKey, otpEntry, TimeSpan.FromMinutes(5));
            _cache.Set(cooldownKey, DateTime.UtcNow.AddSeconds(60), TimeSpan.FromSeconds(60));

            _logger.LogInformation(">>> [REAL EMAIL OTP SENT] Đã gửi mã OTP thành công đến {Email} | Mục đích: {Purpose}",
                normalizedEmail, purpose);

            var response = new SendOtpResponse
            {
                Email = normalizedEmail,
                CooldownSeconds = 60,
                ExpiryMinutes = 5,
                // Tuyệt đối không trả về mã OTP trong phản hồi API
                DevOtpCode = null
            };

            return ApiResponse<SendOtpResponse>.Ok(response, "Mã xác thực OTP đã được gửi đến email của bạn (hiệu lực 5 phút). Vui lòng kiểm tra hộp thư đến hoặc mục thư rác.");
        }

        public bool VerifyOtp(string email, string otpCode, string purpose, out string errorMessage)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(otpCode))
            {
                errorMessage = "Vui lòng cung cấp đầy đủ email và mã OTP.";
                return false;
            }

            var normalizedEmail = email.Trim().ToLowerInvariant();
            var targetPurpose = string.IsNullOrWhiteSpace(purpose) ? "Register" : purpose.Trim();
            var otpKey = $"OTP_{targetPurpose}_{normalizedEmail}";

            if (!_cache.TryGetValue(otpKey, out OtpCacheEntry? entry) || entry == null)
            {
                errorMessage = "Mã OTP đã hết hạn hoặc không tồn tại. Vui lòng yêu cầu mã xác thực mới.";
                return false;
            }

            entry.Attempts++;

            var expectedBytes = System.Text.Encoding.UTF8.GetBytes(entry.Code);
            var actualBytes = System.Text.Encoding.UTF8.GetBytes(otpCode.Trim());
            var isMatch = expectedBytes.Length == actualBytes.Length &&
                          CryptographicOperations.FixedTimeEquals(expectedBytes, actualBytes);

            if (!isMatch)
            {
                // Ngay khi đạt ngưỡng 5 lần thử sai, hủy mã ngay lập tức khỏi bộ nhớ cache (REV-03 / SEC-07)
                if (entry.Attempts >= 5)
                {
                    _cache.Remove(otpKey);
                    errorMessage = "Mã xác thực OTP không chính xác. Mã đã bị hủy do nhập sai quá 5 lần. Vui lòng yêu cầu mã mới.";
                    return false;
                }

                var remaining = 5 - entry.Attempts;
                errorMessage = $"Mã xác thực OTP không chính xác. Bạn còn {remaining} lần thử.";
                return false;
            }

            // Xác thực thành công -> Xóa khỏi cache để chống Replay Attack
            _cache.Remove(otpKey);
            errorMessage = string.Empty;
            return true;
        }
    }
}
