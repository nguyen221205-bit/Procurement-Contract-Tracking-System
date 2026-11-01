using ProcurementSystem.Core.DTOs.SigningAuthority;
using ProcurementSystem.Core.Enums;

namespace ProcurementSystem.Core.Interfaces
{
    public interface ISigningAuthorityService
    {
        /// <summary>
        /// Quản trị viên Bên mời thầu (Manager) gửi hồ sơ đề nghị cấp thẩm quyền ký duyệt cho một thành viên.
        /// </summary>
        Task<SigningAuthorityDto> RequestAuthorityAsync(int procuringEntityId, int requestedByUserId, RequestSigningAuthorityRequest request);

        /// <summary>
        /// Quản trị viên hệ thống (Admin) thẩm định và phê duyệt hoặc từ chối hồ sơ thẩm quyền ký duyệt.
        /// </summary>
        Task<SigningAuthorityDto> VerifyAuthorityAsync(int authorityId, int adminUserId, VerifySigningAuthorityRequest request);

        /// <summary>
        /// Thu hồi thẩm quyền ký duyệt (do Admin hoặc Manager của Bên mời thầu thực hiện).
        /// </summary>
        Task<SigningAuthorityDto> RevokeAuthorityAsync(int authorityId, int currentUserId, RevokeSigningAuthorityRequest request);

        /// <summary>
        /// Danh sách hồ sơ thẩm quyền ký duyệt của Bên mời thầu.
        /// </summary>
        Task<List<SigningAuthorityDto>> GetAuthoritiesByEntityAsync(int procuringEntityId, int currentUserId);

        /// <summary>
        /// Danh sách hồ sơ thẩm quyền ký duyệt đang chờ Admin thẩm định.
        /// </summary>
        Task<List<SigningAuthorityDto>> GetPendingVerificationsAsync(int adminUserId);

        /// <summary>
        /// Chi tiết một hồ sơ thẩm quyền ký duyệt.
        /// </summary>
        Task<SigningAuthorityDto?> GetAuthorityByIdAsync(int authorityId, int currentUserId);

        /// <summary>
        /// Kiểm tra nhân sự có thẩm quyền ký phê duyệt giải ngân (DisbursementApproval hoặc FullLegalRepresentative) hợp lệ tại thời điểm hiện tại hay không.
        /// </summary>
        Task<bool> HasActiveDisbursementAuthorityAsync(int procuringEntityId, int userId, int? contractId = null);

        /// <summary>
        /// Lấy ID của thẩm quyền ký phê duyệt giải ngân đang hoạt động phù hợp nhất (hợp đồng cụ thể hoặc EntityWide).
        /// </summary>
        Task<int?> GetActiveDisbursementAuthorityIdAsync(int procuringEntityId, int userId, int? contractId = null);
    }
}
