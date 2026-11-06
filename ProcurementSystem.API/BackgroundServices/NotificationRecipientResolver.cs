using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProcurementSystem.Core.Enums;
using ProcurementSystem.Infrastructure.Data;

namespace ProcurementSystem.API.BackgroundServices
{
    /// <summary>
    /// Helper giải quyết phạm vi người nhận cảnh báo nội bộ và nhà thầu theo đơn vị (ProcuringEntity),
    /// trạng thái thành viên (MemberStatus), trạng thái tài khoản (IsActive) và chính sách quản trị viên (Admin).
    /// </summary>
    public static class NotificationRecipientResolver
    {
        /// <summary>
        /// Danh sách vai trò thành viên nội bộ trong đơn vị được phép nhận cảnh báo vận hành.
        /// Mặc định: Manager, Officer, Finance, Signer đang Active.
        /// Việc nhận cảnh báo không trao quyền phê duyệt nghiệp vụ hay duyệt chi.
        /// Sử dụng mảng array để EF Core dịch chính xác sang mệnh đề SQL IN (...).
        /// </summary>
        public static readonly ProcuringEntityMemberRole[] OperationalAlertRoles =
        {
            ProcuringEntityMemberRole.Manager,
            ProcuringEntityMemberRole.Officer,
            ProcuringEntityMemberRole.Finance,
            ProcuringEntityMemberRole.Signer
        };

        /// <summary>
        /// Context chứa snapshot dữ liệu người dùng và thành viên trong một lượt quét hạn tự động.
        /// Context này chỉ có giá trị trong lượt quét hiện tại, không giữ cache xuyên lượt để
        /// bảo đảm việc thu hồi quyền (Revoked/Suspended) có hiệu lực tức thì ở lượt tiếp theo.
        /// </summary>
        public class RecipientResolutionContext
        {
            public IReadOnlyList<int> ActiveAdminUserIds { get; }
            public IReadOnlyDictionary<int, List<int>> EntityMembersMap { get; }
            public IReadOnlyDictionary<int, string> ActiveUsersEmailMap { get; }

            public RecipientResolutionContext(
                IEnumerable<int> activeAdminUserIds,
                IDictionary<int, List<int>> entityMembersMap,
                IDictionary<int, string> activeUsersEmailMap)
            {
                ActiveAdminUserIds = activeAdminUserIds.Distinct().ToList();
                EntityMembersMap = new Dictionary<int, List<int>>(entityMembersMap);
                ActiveUsersEmailMap = new Dictionary<int, string>(activeUsersEmailMap);
            }
        }

        /// <summary>
        /// Khởi tạo RecipientResolutionContext từ cơ sở dữ liệu cho một lượt quét.
        /// Thực thi tối ưu bằng các truy vấn gom cụm (batching), tránh query từng user riêng lẻ.
        /// </summary>
        public static async Task<RecipientResolutionContext> BuildContextAsync(
            AppDbContext dbContext,
            CancellationToken cancellationToken = default)
        {
            // 1. Quản trị viên hệ thống (Admin) đang hoạt động (IsActive == true)
            var activeAdminUserIds = await dbContext.UserRoles
                .AsNoTracking()
                .Where(ur => ur.Role.Name == "Admin" && ur.User.IsActive)
                .Select(ur => ur.UserId)
                .Distinct()
                .ToListAsync(cancellationToken);

            // 2. Thành viên nội bộ theo từng đơn vị:
            // - User đang hoạt động (IsActive == true)
            // - Có vai trò hệ thống Procurement
            // - Trạng thái thành viên Active (không bị Suspended hay Revoked)
            // - Vai trò trong đơn vị thuộc danh mục OperationalAlertRoles
            var activeMembers = await dbContext.ProcuringEntityMembers
                .AsNoTracking()
                .Where(m => m.Status == MemberStatus.Active
                         && m.User.IsActive
                         && OperationalAlertRoles.Contains(m.RoleInEntity)
                         && m.User.UserRoles.Any(ur => ur.Role.Name == "Procurement"))
                .Select(m => new { m.ProcuringEntityId, m.UserId })
                .ToListAsync(cancellationToken);

            var entityMembersMap = activeMembers
                .GroupBy(m => m.ProcuringEntityId)
                .ToDictionary(g => g.Key, g => g.Select(m => m.UserId).Distinct().ToList());

            // 3. Toàn bộ người dùng đang hoạt động (IsActive == true) kèm Email để tra cứu gửi thư và lọc tài khoản bị khóa
            var activeUsersEmailMap = await dbContext.Users
                .AsNoTracking()
                .Where(u => u.IsActive)
                .ToDictionaryAsync(u => u.Id, u => u.Email, cancellationToken);

            return new RecipientResolutionContext(activeAdminUserIds, entityMembersMap, activeUsersEmailMap);
        }

        /// <summary>
        /// Giải quyết danh sách UserId nhận cảnh báo nội bộ cho một thực thể (Gói thầu / Hợp đồng / Mốc / Bảo lãnh).
        /// Quy tắc bảo mật:
        /// - Nếu thiếu ProcuringEntityId hoặc cờ OwnershipNeedsReview == true: Chỉ gửi Quản trị viên vận hành (Admin),
        ///   không đoán theo CreatedBy, đồng thời ghi log thông báo bản ghi cần đối soát.
        /// - Nếu hợp lệ: Gửi tới các thành viên hợp lệ của chính ProcuringEntity đó + Quản trị viên hệ thống.
        /// - Deduplicate UserId nếu một người đồng thời có nhiều vai trò hoặc là Admin.
        /// </summary>
        public static IReadOnlyList<int> ResolveInternalRecipients(
            int? procuringEntityId,
            bool ownershipNeedsReview,
            RecipientResolutionContext context,
            ILogger? logger = null,
            string? recordType = null,
            string? recordIdentifier = null)
        {
            // Trường hợp 1: Thiếu ProcuringEntityId hoặc mang cờ OwnershipNeedsReview
            if (!procuringEntityId.HasValue || procuringEntityId.Value <= 0 || ownershipNeedsReview)
            {
                if (logger != null && logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation(
                        "Bản ghi {RecordType} [{RecordIdentifier}] thiếu ProcuringEntityId hoặc mang cờ OwnershipNeedsReview=true. Chỉ gửi thông báo nội bộ tới {AdminCount} Quản trị viên hệ thống để đối soát.",
                        recordType ?? "Nghiệp vụ",
                        recordIdentifier ?? "N/A",
                        context.ActiveAdminUserIds.Count);
                }

                return context.ActiveAdminUserIds;
            }

            // Trường hợp 2: Gói thầu / hợp đồng gắn với đơn vị xác định và không có cờ cần đối soát
            var result = new HashSet<int>();

            // Thành viên hợp lệ của chính đơn vị đó
            if (context.EntityMembersMap.TryGetValue(procuringEntityId.Value, out var memberUserIds))
            {
                foreach (var userId in memberUserIds)
                {
                    result.Add(userId);
                }
            }

            // Quản trị viên hệ thống nhận cảnh báo vận hành theo policy riêng
            foreach (var adminId in context.ActiveAdminUserIds)
            {
                result.Add(adminId);
            }

            return result.ToList();
        }

        /// <summary>
        /// Kiểm tra và lấy UserId của nhà thầu nếu tài khoản còn hoạt động (IsActive == true).
        /// Trả về null nếu tài khoản không tồn tại hoặc đã bị khóa.
        /// </summary>
        public static int? ResolveActiveContractorRecipient(
            int? contractorUserId,
            RecipientResolutionContext context)
        {
            if (contractorUserId.HasValue && contractorUserId.Value > 0 && context.ActiveUsersEmailMap.ContainsKey(contractorUserId.Value))
            {
                return contractorUserId.Value;
            }

            return null;
        }
    }
}
