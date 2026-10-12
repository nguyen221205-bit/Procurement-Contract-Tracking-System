using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ProcurementSystem.Core.Interfaces;

namespace ProcurementSystem.Infrastructure.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<bool> SendEmailAsync(string toEmail, string subject, string body, bool isHtml = true)
        {
            if (string.IsNullOrWhiteSpace(toEmail))
            {
                _logger.LogWarning("Không thể gửi email vì địa chỉ nhận bị rỗng.");
                return false;
            }

            var smtpSection = _configuration.GetSection("Smtp");
            var host = smtpSection["Host"];
            var portString = smtpSection["Port"];
            var username = smtpSection["Username"];
            var password = smtpSection["Password"];
            var senderEmail = smtpSection["SenderEmail"] ?? "noreply@procurementsystem.com";
            var senderName = smtpSection["SenderName"] ?? "Hệ thống Đấu Thầu & Hợp Đồng";
            var enableSsl = bool.TryParse(smtpSection["EnableSsl"], out var ssl) ? ssl : true;
            var port = int.TryParse(portString, out var p) ? p : 587;

            // Nếu chưa cấu hình máy chủ SMTP thực tế, ghi log giả lập để phục vụ kiểm thử phát triển
            if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                _logger.LogInformation("[Email Simulation] Đến: {To} | Tiêu đề: {Subject} | Nội dung: {BodySnippet}...",
                    toEmail, subject, body.Length > 100 ? body[..100] : body);
                return true;
            }

            try
            {
                using var client = new SmtpClient(host, port)
                {
                    EnableSsl = enableSsl,
                    Credentials = new NetworkCredential(username, password),
                    Timeout = 10000 // 10 giây timeout tránh treo luồng worker
                };

                using var mailMessage = new MailMessage
                {
                    From = new MailAddress(senderEmail, senderName),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = isHtml
                };

                mailMessage.To.Add(toEmail);

                await client.SendMailAsync(mailMessage);
                _logger.LogInformation("Gửi email thành công đến {To} | Tiêu đề: {Subject}", toEmail, subject);
                return true;
            }
            catch (Exception ex)
            {
                // Tuyệt đối không ném ngoại lệ làm sập worker hay nghiệp vụ chính
                _logger.LogError(ex, "Lỗi xảy ra khi gửi email đến {To} qua máy chủ SMTP {Host}", toEmail, host);
                return false;
            }
        }
    }
}
