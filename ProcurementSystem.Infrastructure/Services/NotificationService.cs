using Microsoft.EntityFrameworkCore;
using ProcurementSystem.Core.DTOs;
using ProcurementSystem.Core.DTOs.Notification;
using ProcurementSystem.Core.Interfaces;
using ProcurementSystem.Infrastructure.Entities;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using System.Text.Encodings.Web;
using ProcurementSystem.Core.Enums;

namespace ProcurementSystem.Infrastructure.Services
{
    public class NotificationService : INotificationService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(
            IUnitOfWork unitOfWork,
            IServiceScopeFactory scopeFactory,
            IConfiguration configuration,
            ILogger<NotificationService> logger)
        {
            _unitOfWork = unitOfWork;
            _scopeFactory = scopeFactory;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<ApiResponse<PaginatedList<NotificationDto>>> GetMyNotificationsAsync(int userId, int pageIndex = 1, int pageSize = 20)
        {
            var query = _unitOfWork.Repository<Notification>()
                .Query()
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .AsNoTracking()
                .Select(n => new NotificationDto
                {
                    Id = n.Id,
                    UserId = n.UserId,
                    Title = n.Title,
                    Message = n.Message,
                    IsRead = n.IsRead,
                    Type = n.Type,
                    TypeName = n.Type.ToString(),
                    CreatedAt = n.CreatedAt
                });

            var result = await PaginatedList<NotificationDto>.CreateAsync(query, pageIndex, pageSize);
            return ApiResponse<PaginatedList<NotificationDto>>.Ok(result);
        }

        public async Task<ApiResponse<int>> GetUnreadCountAsync(int userId)
        {
            var count = await _unitOfWork.Repository<Notification>()
                .Query()
                .Where(n => n.UserId == userId && !n.IsRead)
                .CountAsync();

            return ApiResponse<int>.Ok(count);
        }

        public async Task<ApiResponse<bool>> MarkAsReadAsync(int notificationId, int userId)
        {
            var notif = await _unitOfWork.Repository<Notification>()
                .Query()
                .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);

            if (notif == null)
            {
                return ApiResponse<bool>.Fail("Không tìm thấy thông báo.");
            }

            notif.IsRead = true;
            _unitOfWork.Repository<Notification>().Update(notif);
            await _unitOfWork.SaveChangesAsync();

            return ApiResponse<bool>.Ok(true, "Đã đánh dấu thông báo là đã đọc.");
        }

        public async Task<ApiResponse<bool>> MarkAllAsReadAsync(int userId)
        {
            var unreadNotifs = await _unitOfWork.Repository<Notification>()
                .Query()
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();

            if (unreadNotifs.Count != 0)
            {
                foreach (var n in unreadNotifs)
                {
                    n.IsRead = true;
                    _unitOfWork.Repository<Notification>().Update(n);
                }
                await _unitOfWork.SaveChangesAsync();
            }

            return ApiResponse<bool>.Ok(true, "Đã đánh dấu tất cả thông báo là đã đọc.");
        }

        public async Task<NotificationDto> CreateNotificationAsync(
            int userId,
            string title,
            string message,
            NotificationType type = NotificationType.Info,
            bool sendEmail = true)
        {
            var notif = new Notification
            {
                UserId = userId,
                Title = title,
                Message = message,
                Type = type,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Repository<Notification>().AddAsync(notif);
            await _unitOfWork.SaveChangesAsync();

            if (sendEmail)
            {
                // Gửi email an toàn chạy ngầm (fire-and-forget) với Scope độc lập tránh ObjectDisposedException
                _ = Task.Run(async () =>
                {
                    await SendNotificationEmailSafeAsync(userId, title, message, type);
                });
            }

            return new NotificationDto
            {
                Id = notif.Id,
                UserId = notif.UserId,
                Title = notif.Title,
                Message = notif.Message,
                Type = notif.Type,
                TypeName = notif.Type.ToString(),
                IsRead = notif.IsRead,
                CreatedAt = notif.CreatedAt
            };
        }

        public async Task CreateBulkNotificationsAsync(
            IEnumerable<int> userIds,
            string title,
            string message,
            NotificationType type = NotificationType.Info,
            bool sendEmail = false)
        {
            var distinctIds = userIds.Distinct().ToList();
            if (distinctIds.Count == 0) return;

            var notifs = distinctIds.Select(id => new Notification
            {
                UserId = id,
                Title = title,
                Message = message,
                Type = type,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            }).ToList();

            foreach (var n in notifs)
            {
                await _unitOfWork.Repository<Notification>().AddAsync(n);
            }
            await _unitOfWork.SaveChangesAsync();

            if (sendEmail)
            {
                foreach (var id in distinctIds)
                {
                    var targetId = id;
                    _ = Task.Run(async () =>
                    {
                        await SendNotificationEmailSafeAsync(targetId, title, message, type);
                    });
                }
            }
        }

        private async Task SendNotificationEmailSafeAsync(int userId, string title, string message, NotificationType type)
        {
            try
            {
                // Khởi tạo scope độc lập, giải phóng hoàn toàn sự phụ thuộc vào HttpContext của request cha
                using var scope = _scopeFactory.CreateScope();
                var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

                var user = await unitOfWork.Repository<User>().GetByIdAsync(userId);
                if (user == null || string.IsNullOrWhiteSpace(user.Email))
                {
                    return;
                }

                var frontendUrl = _configuration["FrontendUrl"] ?? "http://localhost:5173";

                // Mã hóa HTML ngăn ngừa HTML Injection / Email XSS
                var encodedTitle = HtmlEncoder.Default.Encode(title ?? string.Empty);
                var encodedMessage = HtmlEncoder.Default.Encode(message ?? string.Empty)
                    .Replace("&#xA;", "<br/>")
                    .Replace("\r\n", "<br/>")
                    .Replace("\n", "<br/>");

                // Loại bỏ CRLF khỏi Subject phòng chống Email Header Injection
                var cleanSubjectTitle = (title ?? string.Empty).Replace("\r", "").Replace("\n", " ").Trim();
                var emailSubject = $"[Hệ thống Đấu thầu] {cleanSubjectTitle}";

                var typeBadge = type switch
                {
                    NotificationType.Warning => "<span style='background:#fef3c7;color:#b45309;padding:4px 10px;border-radius:6px;font-size:12px;font-weight:bold;'>Cảnh báo</span>",
                    NotificationType.ContractExpiring => "<span style='background:#fee2e2;color:#b91c1c;padding:4px 10px;border-radius:6px;font-size:12px;font-weight:bold;'>Hợp đồng sắp hết hạn</span>",
                    NotificationType.DeadlineApproaching => "<span style='background:#fee2e2;color:#b91c1c;padding:4px 10px;border-radius:6px;font-size:12px;font-weight:bold;'>Sắp hết hạn nộp thầu</span>",
                    NotificationType.ProgressOverdue => "<span style='background:#fee2e2;color:#b91c1c;padding:4px 10px;border-radius:6px;font-size:12px;font-weight:bold;'>Quá hạn tiến độ</span>",
                    _ => "<span style='background:#e0f2fe;color:#0369a1;padding:4px 10px;border-radius:6px;font-size:12px;font-weight:bold;'>Thông báo</span>"
                };

                var emailBody = $@"
                    <div style='font-family: -apple-system, BlinkMacSystemFont, ""Segoe UI"", Roboto, Helvetica, Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 32px 24px; background: #ffffff; border: 1px solid #e2e8f0; border-radius: 12px; box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.05);'>
                        <div style='text-align: center; margin-bottom: 24px; padding-bottom: 16px; border-bottom: 2px solid #0284c7;'>
                            <h1 style='color: #0369a1; font-size: 20px; font-weight: 800; margin: 0; text-transform: uppercase;'>
                                HỆ THỐNG ĐẤU THẦU & QUẢN LÝ HỢP ĐỒNG
                            </h1>
                            <p style='color: #64748b; font-size: 13px; margin: 6px 0 0 0;'>Cổng thông tin quản lý & giám sát hợp đồng điện tử</p>
                        </div>
                        
                        <div style='margin-bottom: 16px;'>
                            {typeBadge}
                        </div>

                        <h2 style='color: #0f172a; font-size: 17px; font-weight: 700; margin: 12px 0;'>
                            {encodedTitle}
                        </h2>

                        <div style='background: #f8fafc; border: 1px solid #e2e8f0; border-radius: 8px; padding: 16px; margin: 20px 0;'>
                            <p style='color: #334155; font-size: 14px; line-height: 1.6; margin: 0;'>
                                {encodedMessage}
                            </p>
                        </div>

                        <div style='text-align: center; margin: 28px 0;'>
                            <a href='{frontendUrl}/notifications' style='display: inline-block; background: #0284c7; color: #ffffff; font-weight: 600; font-size: 14px; padding: 12px 28px; border-radius: 8px; text-decoration: none;'>
                                Xem chi tiết trên hệ thống &rarr;
                            </a>
                        </div>

                        <hr style='border: none; border-top: 1px solid #e2e8f0; margin: 24px 0;' />
                        <p style='font-size: 12px; color: #94a3b8; text-align: center; margin: 0;'>
                            Bạn nhận được thư này vì là người dùng có liên quan trên Hệ thống Đấu thầu & Quản lý Hợp đồng.<br/>
                            Vui lòng không trả lời trực tiếp email tự động này.
                        </p>
                    </div>";

                await emailService.SendEmailAsync(user.Email, emailSubject, emailBody, isHtml: true);
                _logger.LogInformation("Đã gửi email thông báo thành công tới {Email} | Tiêu đề: {Title}", user.Email, title);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi gửi email thông báo ngầm cho UserId: {UserId}", userId);
            }
        }
    }
}
