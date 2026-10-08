using ProcurementSystem.Core.DTOs;
using ProcurementSystem.Core.DTOs.Auth;

namespace ProcurementSystem.Core.Interfaces
{
    public interface IProcuringEntityAuthService
    {
        Task<ApiResponse<ProcuringEntityRegisterResponse>> RegisterProcuringEntityAsync(RegisterProcuringEntityRequest request);
    }
}
