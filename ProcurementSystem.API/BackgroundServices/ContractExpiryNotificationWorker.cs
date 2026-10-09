using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ProcurementSystem.Core.Enums;
using ProcurementSystem.Infrastructure.Data;
using ProcurementSystem.Infrastructure.Entities;

namespace ProcurementSystem.API.BackgroundServices
{
    /// <summary>
    /// Worker định kỳ kiểm tra hạn gói thầu, hợp đồng và mốc tiến độ để sinh cảnh báo tự động
    /// Chạy 1 giờ/lần + chạy 1 lượt ngay khi ứng dụng khởi động. Có cơ chế chống duplicate notification (24h).
    /// </summary>
    public class ContractExpiryNotificationWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ContractExpiryNotificationWorker> _logger;
        private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(1);

        public ContractExpiryNotificationWorker(
            IServiceScopeFactory scopeFactory,
            ILogger<ContractExpiryNotificationWorker> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("ContractExpiryNotificationWorker đã được khởi động.");

            // Vòng lặp định kỳ: Chạy ngay một lần tại startup, sau đó lặp mỗi 1 giờ
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("ContractExpiryNotificationWorker bắt đầu quét kiểm tra hạn tại {Time} UTC...", DateTime.UtcNow);
                    await RunNotificationCheckAsync(stoppingToken);
                    _logger.LogInformation("ContractExpiryNotificationWorker hoàn thành lượt quét kiểm tra hạn.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Đã xảy ra lỗi trong quá trình quét hạn tự động của ContractExpiryNotificationWorker.");
                }

                try
                {
                    await Task.Delay(CheckInterval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }

            _logger.LogInformation("ContractExpiryNotificationWorker đang dừng.");
        }

        private async Task RunNotificationCheckAsync(CancellationToken stoppingToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var now = DateTime.UtcNow;
            var window24h = now.AddHours(-24);

            // 1. Lấy danh sách UserId của Procurement & Admin để gửi cảnh báo nội bộ
            var internalUserIds = await dbContext.UserRoles
                .Include(ur => ur.Role)
                .Where(ur => ur.Role.Name == "Procurement" || ur.Role.Name == "Admin")
                .Select(ur => ur.UserId)
                .Distinct()
                .ToListAsync(stoppingToken);

            var notificationsToAdd = new List<Notification>();

            // Helper kiểm tra trùng lặp trong 24 giờ (cả trong DB lẫn trong mẻ notificationsToAdd hiện tại)
            async Task TryAddNotification(int userId, string title, string message, NotificationType type)
            {
                if (userId <= 0) return;

                // Kiểm tra trong lô đang chuẩn bị add
                if (notificationsToAdd.Any(n => n.UserId == userId && n.Title == title))
                {
                    return;
                }

                // Kiểm tra trong database 24 giờ qua
                var existsInDb = await dbContext.Notifications.AnyAsync(n =>
                    n.UserId == userId &&
                    n.Title == title &&
                    n.CreatedAt >= window24h, stoppingToken);

                if (!existsInDb)
                {
                    notificationsToAdd.Add(new Notification
                    {
                        UserId = userId,
                        Title = title,
                        Message = message,
                        Type = type,
                        CreatedAt = DateTime.UtcNow,
                        IsRead = false
                    });
                }
            }

            // =========================================================================
            // A. CẢNH BÁO GÓI THẦU QUÁ DEADLINE NHƯNG VẪN ĐANG OPEN
            // =========================================================================
            var overduePackages = await dbContext.BidPackages
                .Where(bp => bp.Status == BidPackageStatus.Open && bp.Deadline < now)
                .ToListAsync(stoppingToken);

            foreach (var bp in overduePackages)
            {
                var title = $"[Cảnh báo] Gói thầu {bp.Code} đã hết hạn nộp hồ sơ";
                var message = $"Gói thầu \"{bp.Name}\" đã kết thúc thời hạn tiếp nhận hồ sơ dự thầu lúc {bp.Deadline:dd/MM/yyyy HH:mm} UTC. Vui lòng đóng thầu để chuyển sang giai đoạn chấm điểm.";

                // Gửi cho người tạo
                if (bp.CreatedBy > 0)
                {
                    await TryAddNotification(bp.CreatedBy, title, message, NotificationType.DeadlineApproaching);
                }

                // Gửi cho các chuyên viên Mua sắm
                foreach (var internalUserId in internalUserIds)
                {
                    await TryAddNotification(internalUserId, title, message, NotificationType.DeadlineApproaching);
                }
            }

            // =========================================================================
            // B. CẢNH BÁO HỢP ĐỒNG SẮP HẾT HẠN HOẶC ĐÃ QUÁ HẠN (Active Contracts)
            // =========================================================================
            var activeContracts = await dbContext.Contracts
                .Include(c => c.BidPackage)
                .Include(c => c.Contractor)
                .Where(c => c.Status == ContractStatus.Active)
                .ToListAsync(stoppingToken);

            foreach (var contract in activeContracts)
            {
                var daysRemaining = (int)Math.Ceiling((contract.EndDate - now).TotalDays);
                string? title = null;
                string? message = null;

                if (daysRemaining < 0)
                {
                    title = $"[Khẩn cấp] Hợp đồng {contract.ContractNumber} đã quá hạn kết thúc";
                    message = $"Hợp đồng \"{contract.ContractNumber}\" (Gói: {contract.BidPackage?.Name}) đã quá ngày kết thúc ({contract.EndDate:dd/MM/yyyy}). Vui lòng tiến hành nghiệm thu hoàn thành hoặc lập phụ lục gia hạn hợp đồng.";
                }
                else if (daysRemaining <= 7)
                {
                    title = $"[Khẩn cấp] Hợp đồng {contract.ContractNumber} còn {daysRemaining} ngày là hết hạn";
                    message = $"Hợp đồng \"{contract.ContractNumber}\" sẽ kết thúc vào ngày {contract.EndDate:dd/MM/yyyy} (còn {daysRemaining} ngày). Vui lòng hoàn tất các thủ tục nghiệm thu thanh lý hoặc gia hạn.";
                }
                else if (daysRemaining <= 15)
                {
                    title = $"[Cảnh báo] Hợp đồng {contract.ContractNumber} còn {daysRemaining} ngày là hết hạn";
                    message = $"Hợp đồng \"{contract.ContractNumber}\" sẽ kết thúc vào ngày {contract.EndDate:dd/MM/yyyy}. Đề nghị các bên khẩn trương đẩy nhanh công tác hoàn thiện giai đoạn cuối.";
                }
                else if (daysRemaining <= 30)
                {
                    title = $"[Thông báo] Hợp đồng {contract.ContractNumber} sắp đến hạn (còn {daysRemaining} ngày)";
                    message = $"Hợp đồng \"{contract.ContractNumber}\" có hạn hoàn thành đến ngày {contract.EndDate:dd/MM/yyyy}. Vui lòng rà soát tiến độ thực hiện.";
                }

                if (title != null && message != null)
                {
                    // Gửi cho nhà thầu
                    if (contract.Contractor?.UserId > 0)
                    {
                        await TryAddNotification(contract.Contractor.UserId, title, message, NotificationType.ContractExpiring);
                    }

                    // Gửi cho Ban QLDA / Chuyên viên mua sắm
                    foreach (var internalUserId in internalUserIds)
                    {
                        await TryAddNotification(internalUserId, title, message, NotificationType.ContractExpiring);
                    }
                }
            }

            // =========================================================================
            // C. CẢNH BÁO MỐC TIẾN ĐỘ (Milestones) CHƯA HOÀN THÀNH SẮP/TRỄ HẠN
            // =========================================================================
            var pendingMilestones = await dbContext.ContractMilestones
                .Include(m => m.Contract)
                    .ThenInclude(c => c.Contractor)
                .Where(m => m.Status != MilestoneStatus.Completed && m.Contract.Status == ContractStatus.Active)
                .ToListAsync(stoppingToken);

            foreach (var m in pendingMilestones)
            {
                var daysRemaining = (int)Math.Ceiling((m.DueDate - now).TotalDays);
                string? title = null;
                string? message = null;

                if (daysRemaining < 0)
                {
                    title = $"[Chậm tiến độ] Mốc \"{m.Title}\" hợp đồng {m.Contract.ContractNumber} đã quá hạn";
                    message = $"Mốc tiến độ \"{m.Title}\" có hạn hoàn thành vào ngày {m.DueDate:dd/MM/yyyy} nhưng hiện vẫn chưa hoàn thành nghiệm thu.";
                }
                else if (daysRemaining <= 3)
                {
                    title = $"[Khẩn cấp] Mốc \"{m.Title}\" hợp đồng {m.Contract.ContractNumber} còn {daysRemaining} ngày là đến hạn";
                    message = $"Mốc tiến độ \"{m.Title}\" có hạn đến ngày {m.DueDate:dd/MM/yyyy} (còn {daysRemaining} ngày). Đề nghị khẩn trương hoàn thiện các nội dung công việc.";
                }
                else if (daysRemaining <= 7)
                {
                    title = $"[Nhắc nhở] Mốc \"{m.Title}\" hợp đồng {m.Contract.ContractNumber} còn {daysRemaining} ngày là đến hạn";
                    message = $"Mốc tiến độ \"{m.Title}\" có hạn đến ngày {m.DueDate:dd/MM/yyyy}. Vui lòng chuẩn bị công tác nghiệm thu giai đoạn.";
                }

                if (title != null && message != null)
                {
                    if (m.Contract.Contractor?.UserId > 0)
                    {
                        await TryAddNotification(m.Contract.Contractor.UserId, title, message, NotificationType.ProgressOverdue);
                    }

                    foreach (var internalUserId in internalUserIds)
                    {
                        await TryAddNotification(internalUserId, title, message, NotificationType.ProgressOverdue);
                    }
                }
            }

            // Lưu toàn bộ thông báo mới sinh vào CSDL
            if (notificationsToAdd.Count > 0)
            {
                await dbContext.Notifications.AddRangeAsync(notificationsToAdd, stoppingToken);
                await dbContext.SaveChangesAsync(stoppingToken);
                _logger.LogInformation("Đã sinh {Count} thông báo cảnh báo mới vào hệ thống.", notificationsToAdd.Count);
            }
        }
    }
}
