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

        private class OtpCacheEntry
        {
            public string Code { get; set; } = string.Empty;
            public int Attempts { get; set; }
            public DateTime CreatedAt { get; set; }
        }

        public OtpService(
            IMemoryCache cache,
            ILogger<OtpService> logger,
            IConfiguration configuration)
        {
            _cache = cache;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task<ApiResponse<SendOtpResponse>> SendOtpAsync(SendOtpRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email))
            {
                return ApiResponse<SendOtpResponse>.Fail("Email không được để trống.");
            }

            var normalizedEmail = request.Email.Trim().ToLowerInvariant();
            var purpose = string.IsNullOrWhiteSpace(request.Purpose) ? "Register" : request.Purpose.Trim();

            var cooldownKey = $"OTP_COOLDOWN_{purpose}_{normalizedEmail}";
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

            // Lưu cache: Thời hạn hiệu lực 5 phút, Cooldown gửi lại 60 giây
            _cache.Set(otpKey, otpEntry, TimeSpan.FromMinutes(5));
            _cache.Set(cooldownKey, DateTime.UtcNow.AddSeconds(60), TimeSpan.FromSeconds(60));

            // Thử gửi Email qua SMTP nếu cấu hình có sẵn
            var emailSent = await TrySendEmailAsync(normalizedEmail, otpCode, purpose);

            // Ghi log bảo mật vào hệ thống
            _logger.LogInformation("=================================================================");
            _logger.LogInformation(">>> [EMAIL OTP] Gửi tới: {Email} | Mã OTP: {Code} | Mục đích: {Purpose} | SMTP Sent: {Sent}",
                normalizedEmail, otpCode, purpose, emailSent);
            _logger.LogInformation("=================================================================");

            var response = new SendOtpResponse
            {
                Email = normalizedEmail,
                CooldownSeconds = 60,
                ExpiryMinutes = 5,
                // Trong môi trường Development trả về DevOtpCode để phục vụ kiểm thử nhanh
                DevOtpCode = IsDevelopmentEnvironment() ? otpCode : null
            };

            return ApiResponse<SendOtpResponse>.Ok(response, "Mã xác thực OTP đã được gửi đến email của bạn (hiệu lực 5 phút).");
        }

        private bool IsDevelopmentEnvironment()
        {
            var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") 
                      ?? _configuration["ASPNETCORE_ENVIRONMENT"];
            return string.Equals(env, "Development", StringComparison.OrdinalIgnoreCase);
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

            if (entry.Attempts > 5)
            {
                _cache.Remove(otpKey);
                errorMessage = "Mã OTP đã bị hủy do nhập sai quá 5 lần. Vui lòng yêu cầu mã mới.";
                return false;
            }

            if (!string.Equals(entry.Code, otpCode.Trim(), StringComparison.Ordinal))
            {
                var remaining = 5 - entry.Attempts;
                errorMessage = remaining > 0 
                    ? $"Mã xác thực OTP không chính xác. Bạn còn {remaining} lần thử." 
                    : "Mã xác thực OTP không chính xác. Mã đã bị hủy.";
                return false;
            }

            // Xác thực thành công -> Xóa khỏi cache để chống Replay Attack
            _cache.Remove(otpKey);
            errorMessage = string.Empty;
            return true;
        }

        private async Task<bool> TrySendEmailAsync(string toEmail, string otpCode, string purpose)
        {
            var smtpHost = _configuration["Smtp:Host"];
            var smtpPortStr = _configuration["Smtp:Port"];
            var smtpUser = _configuration["Smtp:Username"];
            var smtpPass = _configuration["Smtp:Password"];
            var smtpSender = _configuration["Smtp:SenderEmail"] ?? "no-reply@procurement.gov.vn";

            if (string.IsNullOrWhiteSpace(smtpHost) || !int.TryParse(smtpPortStr, out var smtpPort))
            {
                // Chưa cấu hình SMTP -> fallback sang logging
                return false;
            }

            try
            {
                using var client = new System.Net.Mail.SmtpClient(smtpHost, smtpPort)
                {
                    EnableSsl = true,
                    Credentials = new System.Net.NetworkCredential(smtpUser, smtpPass)
                };

                var subject = "[Hệ thống Đấu thầu] Mã xác thực OTP đăng ký tài khoản";
                var body = $@"
                    <div style='font-family: Arial, sans-serif; max-width: 600px; margin: auto; padding: 24px; border: 1px solid #e2e8f0; rounded: 12px;'>
                        <h2 style='color: #0284c7; text-align: center;'>HỆ THỐNG ĐẤU THẦU ĐIỆN TỬ</h2>
                        <p>Xin chào,</p>
                        <p>Bạn đang thực hiện đăng ký tài khoản trên Hệ thống Đấu thầu điện tử. Mã xác thực OTP của bạn là:</p>
                        <div style='text-align: center; margin: 24px 0;'>
                            <span style='display: inline-block; font-size: 32px; font-weight: bold; letter-spacing: 8px; color: #0f172a; background: #f1f5f9; padding: 12px 28px; border-radius: 8px; border: 1px dashed #94a3b8;'>
                                {otpCode}
                            </span>
                        </div>
                        <p style='color: #64748b; font-size: 13px;'>* Mã OTP này có hiệu lực trong vòng <strong>5 phút</strong>. Tuyệt đối không chia sẻ mã này cho bất kỳ ai.</p>
                        <hr style='border: none; border-top: 1px solid #e2e8f0; margin: 20px 0;' />
                        <p style='font-size: 11px; color: #94a3b8; text-align: center;'>Đây là email tự động, vui lòng không trả lời thư này.</p>
                    </div>";

                var mailMessage = new System.Net.Mail.MailMessage
                {
                    From = new System.Net.Mail.MailAddress(smtpSender, "Hệ Thống Đấu Thầu"),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };
                mailMessage.To.Add(toEmail);

                await client.SendMailAsync(mailMessage);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Không thể gửi email OTP qua SMTP: {Message}", ex.Message);
                return false;
            }
        }
    }
}
