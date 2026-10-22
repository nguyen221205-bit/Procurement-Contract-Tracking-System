using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace ProcurementSystem.Infrastructure.Hubs
{
    /// <summary>
    /// Hub SignalR quản lý kết nối thời gian thực cho hệ thống thông báo
    /// </summary>
    [Authorize]
    public class NotificationHub : Hub
    {
        private readonly ILogger<NotificationHub> _logger;

        public NotificationHub(ILogger<NotificationHub> logger)
        {
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            _logger.LogInformation("SignalR: Người dùng {UserId} đã kết nối vào NotificationHub (ConnectionId: {ConnectionId})", userId, Context.ConnectionId);

            // Tự động gia nhập group theo danh sách vai trò để hỗ trợ broadcast theo nhóm quyền
            var roleClaims = Context.User?.FindAll(ClaimTypes.Role) ?? Enumerable.Empty<Claim>();
            foreach (var roleClaim in roleClaims)
            {
                if (!string.IsNullOrWhiteSpace(roleClaim.Value))
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, $"Role_{roleClaim.Value}");
                }
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (exception != null)
            {
                _logger.LogWarning(exception, "SignalR: Người dùng {UserId} ngắt kết nối với lỗi", userId);
            }
            else
            {
                _logger.LogInformation("SignalR: Người dùng {UserId} đã ngắt kết nối (ConnectionId: {ConnectionId})", userId, Context.ConnectionId);
            }

            await base.OnDisconnectedAsync(exception);
        }
    }

    /// <summary>
    /// Provider trích xuất UserId từ JWT token cho SignalR
    /// </summary>
    public class CustomUserIdProvider : IUserIdProvider
    {
        public string? GetUserId(HubConnectionContext connection)
        {
            return connection.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? connection.User?.FindFirst("sub")?.Value
                ?? connection.User?.FindFirst("userId")?.Value;
        }
    }
}
