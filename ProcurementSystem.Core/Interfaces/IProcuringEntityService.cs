using ProcurementSystem.Core.DTOs;
using ProcurementSystem.Core.DTOs.ProcuringEntity;

namespace ProcurementSystem.Core.Interfaces
{
    public interface IProcuringEntityService
    {
        Task<ApiResponse<PaginatedList<ProcuringEntityDto>>> GetProcuringEntitiesAsync(ProcuringEntityFilterParams filter);
        Task<ApiResponse<ProcuringEntityDto>> GetProcuringEntityByIdAsync(int id);
        Task<ApiResponse<ProcuringEntityDto>> GetProcuringEntityByUserIdAsync(int userId);
        Task<ApiResponse<ProcuringEntityDto>> UpdateCurrentProfileAsync(int userId, UpdateProcuringEntityProfileRequest request);
        Task<ApiResponse<ProcuringEntityDto>> VerifyProcuringEntityAsync(int id, int adminUserId, VerifyProcuringEntityRequest request);
        Task<(string PhysicalPath, string ContentType, string FileName)?> GetEstablishmentFileAsync(int id);
        Task<(string PhysicalPath, string ContentType, string FileName)?> GetAppointmentFileAsync(int id);
    }
}
