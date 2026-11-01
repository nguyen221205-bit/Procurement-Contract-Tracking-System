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
        if (package?.ProcuringEntityId is not int entityId || package.OwnershipNeedsReview) return false;
        var member = await unitOfWork.Repository<ProcuringEntityMember>().Query()
            .FirstOrDefaultAsync(m => m.ProcuringEntityId == entityId && m.UserId == userId && m.Status == MemberStatus.Active);
        if (member == null || (member.RoleInEntity != ProcuringEntityMemberRole.Manager &&
            !(member.RoleInEntity == ProcuringEntityMemberRole.Officer && package.CreatedBy == userId))) return false;

        // Include membership RowVersion in the same SaveChanges as the business write.
        // A revoke committed after this read causes that write to fail atomically.
        unitOfWork.Repository<ProcuringEntityMember>().UpdateScalar(member);
        return true;
    }
}
