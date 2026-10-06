using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProcurementSystem.Core.DTOs;
using ProcurementSystem.Core.DTOs.Notification;
using ProcurementSystem.Core.Interfaces;

namespace ProcurementSystem.API.Controllers
{
    /// <summary>
    /// Phân hệ Quản lý Thông báo cá nhân (Notifications)
    /// </summary>
    [ApiController]
    [Route("api/notifications")]
    [Produces("application/json")]
    [Authorize]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public NotificationsController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        /// <summary>
        /// Lấy danh sách thông báo của tài khoản đăng nhập
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<PaginatedList<NotificationDto>>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<PaginatedList<NotificationDto>>>> GetMyNotifications(
            [FromQuery] int pageIndex = 1,
            [FromQuery] int pageSize = 20)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue) return Unauthorized();

            var result = await _notificationService.GetMyNotificationsAsync(userId.Value, pageIndex, pageSize);
            return Ok(result);
        }

        /// <summary>
        /// Lấy số lượng thông báo chưa đọc
        /// </summary>
        [HttpGet("unread-count")]
        [ProducesResponseType(typeof(ApiResponse<int>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<int>>> GetUnreadCount()
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue) return Unauthorized();

            var result = await _notificationService.GetUnreadCountAsync(userId.Value);
            return Ok(result);
        }

        /// <summary>
        /// Đánh dấu một thông báo là đã đọc
        /// </summary>
        [HttpPatch("{id:int}/read")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<bool>>> MarkAsRead(int id)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue) return Unauthorized();

            var result = await _notificationService.MarkAsReadAsync(id, userId.Value);
            if (!result.Success) return NotFound(result);
            return Ok(result);
        }

        /// <summary>
        /// Đánh dấu tất cả thông báo là đã đọc
        /// </summary>
        [HttpPost("mark-all-read")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<bool>>> MarkAllAsRead()
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue) return Unauthorized();

            var result = await _notificationService.MarkAllAsReadAsync(userId.Value);
            return Ok(result);
        }

        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out int userId) ? userId : null;
        }
    }
}
