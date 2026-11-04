using ProcurementSystem.Core.DTOs;
using ProcurementSystem.Core.DTOs.Common;
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
        Task<FileDownloadResult?> GetEstablishmentFileAsync(int id);
        Task<FileDownloadResult?> GetAppointmentFileAsync(int id);
    }
}
