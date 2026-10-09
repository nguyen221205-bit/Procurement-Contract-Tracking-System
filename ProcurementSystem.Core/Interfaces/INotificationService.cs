using ProcurementSystem.Core.DTOs;
using ProcurementSystem.Core.DTOs.Notification;

namespace ProcurementSystem.Core.Interfaces
{
    public interface INotificationService
    {
        /// <summary>
        /// Lấy danh sách thông báo của người dùng đăng nhập (có phân trang)
        /// </summary>
        Task<ApiResponse<PaginatedList<NotificationDto>>> GetMyNotificationsAsync(int userId, int pageIndex = 1, int pageSize = 20);

        /// <summary>
        /// Lấy số lượng thông báo chưa đọc của người dùng
        /// </summary>
        Task<ApiResponse<int>> GetUnreadCountAsync(int userId);

        /// <summary>
        /// Đánh dấu một thông báo là đã đọc
        /// </summary>
        Task<ApiResponse<bool>> MarkAsReadAsync(int notificationId, int userId);

        /// <summary>
        /// Đánh dấu tất cả thông báo của người dùng là đã đọc
        /// </summary>
        Task<ApiResponse<bool>> MarkAllAsReadAsync(int userId);
    }
}
