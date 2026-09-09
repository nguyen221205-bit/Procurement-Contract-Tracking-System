using ProcurementSystem.Core.DTOs;
using ProcurementSystem.Core.DTOs.Auth;

namespace ProcurementSystem.Core.Interfaces
{
    public interface IContractorAuthService
    {
        Task<ApiResponse<ContractorRegisterResponse>> RegisterContractorAsync(RegisterContractorRequest request);
    }
}
