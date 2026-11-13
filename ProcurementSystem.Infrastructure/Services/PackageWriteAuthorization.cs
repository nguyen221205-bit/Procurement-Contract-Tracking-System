using Microsoft.EntityFrameworkCore;
using ProcurementSystem.Core.Enums;
using ProcurementSystem.Core.Interfaces;
using ProcurementSystem.Infrastructure.Entities;

namespace ProcurementSystem.Infrastructure.Services;

internal static class PackageWriteAuthorization
{
    // CreatedBy records authorship, never permanent permission. Managers may take over
    // work belonging to their entity; other officers must still be the assigned creator.
    public static async Task<bool> CanWriteAsync(IUnitOfWork unitOfWork, BidPackage? package, int userId, bool isAdmin)
    {
        if (isAdmin) return true;
        if (package == null || package.OwnershipNeedsReview) return false;
        if (package.ProcuringEntityId is not int entityId)
        {
            // Fallback cho gói thầu lịch sử chưa gắn ProcuringEntityId hoặc test fixtures
            return package.CreatedBy == userId;
        }

        var member = await unitOfWork.Repository<ProcuringEntityMember>().Query()
            .FirstOrDefaultAsync(m => m.ProcuringEntityId == entityId && m.UserId == userId && m.Status == MemberStatus.Active);
        if (member == null || (member.RoleInEntity != ProcuringEntityMemberRole.Manager &&
            !(member.RoleInEntity == ProcuringEntityMemberRole.Officer && package.CreatedBy == userId))) return false;

        // Include membership RowVersion in the same SaveChanges as the business write.
        // A revoke committed after this read causes that write to fail atomically.
        unitOfWork.Repository<ProcuringEntityMember>().UpdateScalar(member);
        return true;
    }

    /// <summary>
    /// Kiểm tra quyền chỉnh sửa/quản lý gói thầu cho luồng chỉ đọc (không gắn entity vào ChangeTracker).
    /// </summary>
    public static async Task<bool> CanWriteReadOnlyAsync(IUnitOfWork unitOfWork, BidPackage? package, int userId, bool isAdmin)
    {
        if (isAdmin) return true;
        if (package == null || package.OwnershipNeedsReview) return false;
        if (package.ProcuringEntityId is not int entityId)
        {
            return package.CreatedBy == userId;
        }

        var member = await unitOfWork.Repository<ProcuringEntityMember>().Query()
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.ProcuringEntityId == entityId && m.UserId == userId && m.Status == MemberStatus.Active);
        if (member == null) return false;

        return member.RoleInEntity == ProcuringEntityMemberRole.Manager ||
               (member.RoleInEntity == ProcuringEntityMemberRole.Officer && package.CreatedBy == userId);
    }

    /// <summary>
    /// Kiểm tra quyền đọc nội bộ của nhân sự Bên mời thầu đối với gói thầu/hợp đồng thuộc đơn vị mình.
    /// Cho phép Admin, hoặc bất kỳ thành viên đang hoạt động (Active) nào thuộc ProcuringEntity sở hữu gói thầu
    /// (hoặc người tạo gói thầu nếu gói thầu chưa gắn ProcuringEntityId).
    /// </summary>
    public static async Task<bool> CanReadEntityStaffAsync(IUnitOfWork unitOfWork, BidPackage? package, int userId, bool isAdmin)
    {
        if (isAdmin) return true;
        if (package == null || package.OwnershipNeedsReview) return false;
        if (package.ProcuringEntityId is not int entityId)
        {
            return package.CreatedBy == userId;
        }

        return await unitOfWork.Repository<ProcuringEntityMember>().Query()
            .AsNoTracking()
            .AnyAsync(m => m.ProcuringEntityId == entityId && m.UserId == userId && m.Status == MemberStatus.Active);
    }
}
