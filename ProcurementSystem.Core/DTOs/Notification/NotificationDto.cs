using ProcurementSystem.Core.Enums;

namespace ProcurementSystem.Core.DTOs.Notification
{
    public class NotificationDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public bool IsRead { get; set; }
        public NotificationType Type { get; set; }
        public string TypeName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
