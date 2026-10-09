using Microsoft.EntityFrameworkCore;
using ProcurementSystem.Core.DTOs;
using ProcurementSystem.Core.DTOs.Notification;
using ProcurementSystem.Core.Interfaces;
using ProcurementSystem.Infrastructure.Entities;

namespace ProcurementSystem.Infrastructure.Services
{
    public class NotificationService : INotificationService
    {
        private readonly IUnitOfWork _unitOfWork;

        public NotificationService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
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
    }
}
