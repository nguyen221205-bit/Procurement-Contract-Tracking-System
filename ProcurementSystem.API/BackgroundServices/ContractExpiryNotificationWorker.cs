using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ProcurementSystem.Core.Enums;
using ProcurementSystem.Core.Interfaces;
using ProcurementSystem.Infrastructure.Data;
using ProcurementSystem.Infrastructure.Entities;
using ProcurementSystem.Infrastructure.Hubs;

namespace ProcurementSystem.API.BackgroundServices
{
    /// <summary>
    /// Worker định kỳ kiểm tra hạn gói thầu, hợp đồng và mốc tiến độ để sinh cảnh báo tự động
    /// Chạy 1 giờ/lần + chạy 1 lượt ngay khi ứng dụng khởi động. Có cơ chế chống duplicate notification và email (24h).
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
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

            var now = DateTime.UtcNow;
            var window24h = now.AddHours(-24);

            // 1. Lấy danh sách UserId của Procurement & Admin để gửi cảnh báo nội bộ
            var internalUserIds = await dbContext.UserRoles
                .Include(ur => ur.Role)
                .Where(ur => ur.Role.Name == "Procurement" || ur.Role.Name == "Admin")
                .Select(ur => ur.UserId)
                .Distinct()
                .ToListAsync(stoppingToken);

            // Tra cứu map UserId -> Email để gửi email song song với Notification
            var usersDict = await dbContext.Users
                .AsNoTracking()
                .ToDictionaryAsync(u => u.Id, u => u.Email, stoppingToken);

            var notificationsToAdd = new List<Notification>();
            var emailsToSend = new List<(string Email, string Title, string Message)>();

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

                    // Thêm vào hàng đợi gửi email chống trùng 24h
                    if (usersDict.TryGetValue(userId, out var email) && !string.IsNullOrWhiteSpace(email))
                    {
                        emailsToSend.Add((email, title, message));
                    }
                }
            }

            // =========================================================================
            // A. CẢNH BÁO VÀ TỰ ĐỘNG ĐÓNG GÓI THẦU QUÁ DEADLINE NHƯNG VẪN ĐANG OPEN
            // =========================================================================
            var overduePackages = await dbContext.BidPackages
                .Where(bp => bp.Status == BidPackageStatus.Open && bp.Deadline < now)
                .ToListAsync(stoppingToken);

            foreach (var bp in overduePackages)
            {
                // Tự động chuyển trạng thái đóng thầu (Closed) theo quy định Luật Đấu thầu
                bp.Status = BidPackageStatus.Closed;
                _logger.LogInformation("Gói thầu {Code} đã tự động đóng (Closed) do quá thời hạn nộp hồ sơ ({Deadline:dd/MM/yyyy HH:mm} UTC).", bp.Code, bp.Deadline);

                var title = $"[Thông báo] Gói thầu {bp.Code} đã hết hạn và tự động đóng thầu";
                var message = $"Gói thầu \"{bp.Name}\" đã kết thúc thời hạn tiếp nhận hồ sơ dự thầu lúc {bp.Deadline:dd/MM/yyyy HH:mm} UTC và đã được hệ thống tự động chuyển sang trạng thái Đóng thầu (Closed). Vui lòng phân công Tổ chuyên gia để tiến hành mở thầu và chấm điểm.";

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

            // =========================================================================
            // D. F5-7: QUÉT VÀ TỰ ĐỘNG CẬP NHẬT TRẠNG THÁI BẢO LÃNH HẾT HẠN (ContractGuarantees)
            // =========================================================================
            var validGuarantees = await dbContext.ContractGuarantees
                .Include(g => g.Contract)
                    .ThenInclude(c => c.BidPackage)
                .Where(g => g.Status == GuaranteeStatus.Valid)
                .ToListAsync(stoppingToken);

            foreach (var g in validGuarantees)
            {
                var daysRemaining = (int)Math.Ceiling((g.ExpiryDate - now).TotalDays);
                if (daysRemaining < 0)
                {
                    g.Status = GuaranteeStatus.Expired;
                    g.UpdatedAt = DateTime.UtcNow;

                    var title = $"[Khẩn cấp] Chứng thư bảo lãnh {g.GuaranteeNumber} đã hết hạn";
                    var message = $"Chứng thư bảo lãnh ngân hàng #{g.GuaranteeNumber} thuộc hợp đồng {g.Contract?.ContractNumber} đã hết hạn hiệu lực ({g.ExpiryDate:dd/MM/yyyy}). Vui lòng yêu cầu nhà thầu gia hạn hoặc xử lý biện pháp bảo đảm.";

                    if (g.Contract?.BidPackage?.CreatedBy > 0)
                    {
                        await TryAddNotification(g.Contract.BidPackage.CreatedBy, title, message, NotificationType.ContractExpiring);
                    }
                    foreach (var internalUserId in internalUserIds)
                    {
                        await TryAddNotification(internalUserId, title, message, NotificationType.ContractExpiring);
                    }
                }
                else if (daysRemaining <= 7)
                {
                    var title = $"[Cảnh báo] Chứng thư bảo lãnh {g.GuaranteeNumber} sắp hết hạn (còn {daysRemaining} ngày)";
                    var message = $"Chứng thư bảo lãnh ngân hàng #{g.GuaranteeNumber} thuộc hợp đồng {g.Contract?.ContractNumber} sẽ hết hạn vào ngày {g.ExpiryDate:dd/MM/yyyy}. Đề nghị rà soát và yêu cầu nhà thầu gia hạn bảo đảm.";

                    if (g.Contract?.BidPackage?.CreatedBy > 0)
                    {
                        await TryAddNotification(g.Contract.BidPackage.CreatedBy, title, message, NotificationType.ContractExpiring);
                    }
                    foreach (var internalUserId in internalUserIds)
                    {
                        await TryAddNotification(internalUserId, title, message, NotificationType.ContractExpiring);
                    }
                }
            }

            // =========================================================================
            // E. F5-7: CẢNH BÁO HẠN CHÓT NỘP VĂN BẢN GIẢI TRÌNH HSDT (SubmissionClarification)
            // =========================================================================
            var pendingSubmissionClarifications = await dbContext.BidClarifications
                .Include(c => c.BidPackage)
                .Include(c => c.BidSubmission)
                    .ThenInclude(s => s!.Contractor)
                .Where(c => c.Type == ClarificationType.SubmissionClarification 
                         && c.Status == ClarificationStatus.Pending 
                         && c.Deadline.HasValue 
                         && c.Deadline.Value > now)
                .ToListAsync(stoppingToken);

            foreach (var clar in pendingSubmissionClarifications)
            {
                var hoursRemaining = (clar.Deadline!.Value - now).TotalHours;
                if (hoursRemaining <= 24 && hoursRemaining > 0)
                {
                    var contractorUserId = clar.BidSubmission?.Contractor?.UserId;
                    if (contractorUserId.HasValue && contractorUserId.Value > 0)
                    {
                        var title = $"[Khẩn cấp] Thời hạn giải trình HSDT gói [{clar.BidPackage?.Code}] còn dưới 24 giờ";
                        var message = $"Yêu cầu giải trình bổ sung hồ sơ '{clar.Title}' cho gói thầu {clar.BidPackage?.Code} sẽ hết hạn vào lúc {clar.Deadline.Value:dd/MM/yyyy HH:mm} UTC. Quý nhà thầu vui lòng khẩn trương nộp văn bản giải trình.";
                        await TryAddNotification(contractorUserId.Value, title, message, NotificationType.DeadlineApproaching);
                    }
                }
            }

            // Lưu toàn bộ thông báo mới sinh vào CSDL (kèm trạng thái Closed của các gói thầu, Expired của bảo lãnh)
            if (notificationsToAdd.Count > 0 || overduePackages.Count > 0 || validGuarantees.Any(g => g.Status == GuaranteeStatus.Expired))
            {
                if (notificationsToAdd.Count > 0)
                {
                    await dbContext.Notifications.AddRangeAsync(notificationsToAdd, stoppingToken);
                }
                await dbContext.SaveChangesAsync(stoppingToken);
                _logger.LogInformation("Đã lưu thông báo và cập nhật dữ liệu quét hạn vào hệ thống.");

                // F5-7: Bắn sự kiện SignalR Real-time cho người dùng đang mở ứng dụng
                if (notificationsToAdd.Count > 0)
                {
                    try
                    {
                        var hubContext = scope.ServiceProvider.GetService<IHubContext<NotificationHub>>();
                        if (hubContext != null)
                        {
                            foreach (var notif in notificationsToAdd)
                            {
                                await hubContext.Clients.User(notif.UserId.ToString()).SendAsync("ReceiveNotification", new
                                {
                                    id = notif.Id,
                                    title = notif.Title,
                                    message = notif.Message,
                                    type = notif.Type.ToString(),
                                    createdAt = notif.CreatedAt,
                                    isRead = notif.IsRead
                                }, stoppingToken);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Lỗi xảy ra khi bắn SignalR từ ContractExpiryNotificationWorker.");
                    }
                }
            }

            // Gửi email cảnh báo chống trùng lặp (không làm sập worker nếu lỗi SMTP)
            if (emailsToSend.Count > 0)
            {
                _logger.LogInformation("Bắt đầu gửi {Count} email cảnh báo tới các tài khoản liên quan...", emailsToSend.Count);
                foreach (var (email, emailTitle, emailMessage) in emailsToSend)
                {
                    try
                    {
                        await emailService.SendEmailAsync(email, emailTitle, emailMessage, isHtml: false);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Lỗi xảy ra khi gửi email cảnh báo tới {Email}", email);
                    }
                }
            }
        }
    }
}
