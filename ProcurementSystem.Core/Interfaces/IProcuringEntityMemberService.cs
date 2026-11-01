using ProcurementSystem.Core.DTOs;
using ProcurementSystem.Core.DTOs.ProcuringEntity;

namespace ProcurementSystem.Core.Interfaces
{
    public interface IProcuringEntityMemberService
    {
        Task<ApiResponse<List<ProcuringEntityMemberDto>>> GetMembersAsync(int procuringEntityId, int currentUserId, bool isAdmin);
        Task<ApiResponse<List<ProcuringEntityInvitationDto>>> GetInvitationsAsync(int procuringEntityId, int currentUserId, bool isAdmin);
        Task<ApiResponse<ProcuringEntityInvitationDto>> SendInvitationAsync(int procuringEntityId, SendInvitationRequest request, int currentUserId, bool isAdmin);
        Task<ApiResponse<InvitationDetailDto>> GetInvitationByTokenAsync(string token);
        Task<ApiResponse<ProcuringEntityMemberDto>> AcceptInvitationAsync(AcceptInvitationRequest request, int? currentUserId);
        Task<ApiResponse<bool>> RevokeInvitationAsync(int procuringEntityId, int invitationId, int currentUserId, bool isAdmin);
        Task<ApiResponse<ProcuringEntityMemberDto>> UpdateMemberRoleAsync(int procuringEntityId, int memberId, UpdateMemberRoleRequest request, int currentUserId, bool isAdmin);
        Task<ApiResponse<bool>> RevokeMemberAsync(int procuringEntityId, int memberId, int currentUserId, bool isAdmin);
    }
}
