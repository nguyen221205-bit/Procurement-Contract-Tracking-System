using Microsoft.AspNetCore.Http;
using ProcurementSystem.Core.DTOs;
using ProcurementSystem.Core.DTOs.Common;
using ProcurementSystem.Core.DTOs.Contract;

namespace ProcurementSystem.Core.Interfaces
{
    /// <summary>
    /// Giao diện chuyên biệt quản lý Mốc thanh toán, Báo cáo tiến độ và Nghiệm thu Maker-Checker (ISP).
    /// </summary>
    public interface IContractMilestoneAcceptanceService
    {
        Task<ApiResponse<ContractMilestoneDto>> AddMilestoneAsync(int contractId, CreateMilestoneRequest request, int userId, bool isAdmin);
        Task<ApiResponse<ContractMilestoneDto>> UpdateMilestoneAsync(int contractId, int milestoneId, CreateMilestoneRequest request, int userId, bool isAdmin);
        Task<ApiResponse<bool>> DeleteMilestoneAsync(int contractId, int milestoneId, int userId, bool isAdmin);
        Task<ApiResponse<ProgressUpdateDto>> AddProgressUpdateAsync(int contractId, CreateProgressUpdateRequest request, int userId);
        Task<ApiResponse<ProgressUpdateDto>> AddMilestoneProgressUpdateAsync(int milestoneId, CreateProgressUpdateRequest request, int userId);
        Task<ApiResponse<MilestoneAcceptanceDto>> TechnicalReviewMilestoneAsync(int milestoneId, TechnicalReviewMilestoneRequest request, int userId, bool isAdmin);
        Task<ApiResponse<MilestoneAcceptanceDto>> ApproveMilestoneAcceptanceAsync(int milestoneId, ApproveMilestoneAcceptanceRequest request, int userId, bool isAdmin);
        Task<ApiResponse<FileDownloadResult>> GetAcceptanceSnapshotDocumentDownloadAsync(
            int milestoneId, int acceptanceId, int userId, bool isAdmin, bool isProcurement, bool isContractor);
    }

    /// <summary>
    /// Giao diện chuyên biệt quản lý Chứng thư bảo lãnh thực hiện hợp đồng (ISP).
    /// </summary>
    public interface IContractGuaranteeService
    {
        Task<ApiResponse<List<ContractGuaranteeDto>>> GetContractGuaranteesAsync(int contractId, int userId, bool isInternalStaff, bool isAdmin);
        Task<ApiResponse<ContractGuaranteeDto>> AddContractGuaranteeAsync(int contractId, CreateContractGuaranteeRequest request, int userId, bool isAdmin);
        Task<ApiResponse<ContractGuaranteeDto>> UpdateGuaranteeStatusAsync(int contractId, int guaranteeId, UpdateGuaranteeStatusRequest request, int userId, bool isAdmin);
        Task<ApiResponse<string>> UploadGuaranteeDocumentAsync(int contractId, int guaranteeId, IFormFile file, int userId, bool isAdmin);
        Task<FileDownloadResult?> GetGuaranteeDocumentForDownloadAsync(int contractId, int guaranteeId, int userId, bool isInternalStaff, bool isAdmin);
    }

    /// <summary>
    /// Giao diện chuyên biệt quản lý Phụ lục hợp đồng kinh tế (ISP).
    /// </summary>
    public interface IContractAddendumService
    {
        Task<ApiResponse<List<ContractAddendumDto>>> GetContractAddendaAsync(int contractId, int userId, bool isInternalStaff, bool isAdmin);
        Task<ApiResponse<ContractAddendumDto>> CreateContractAddendumAsync(int contractId, CreateContractAddendumRequest request, int userId, bool isAdmin);
        Task<ApiResponse<ContractAddendumDto>> ApproveContractAddendumAsync(int contractId, int addendumId, ApproveContractAddendumRequest request, int userId, bool isAdmin);
        Task<ApiResponse<string>> UploadAddendumDocumentAsync(int contractId, int addendumId, IFormFile file, int userId, bool isAdmin);
        Task<FileDownloadResult?> GetAddendumDocumentForDownloadAsync(int contractId, int addendumId, int userId, bool isInternalStaff, bool isAdmin);
    }

    /// <summary>
    /// Giao diện chuyên biệt quản lý Đánh giá uy tín và Hồ sơ tín nhiệm nhà thầu cuối kỳ (ISP).
    /// </summary>
    public interface IContractorEvaluationService
    {
        Task<ApiResponse<ContractorEvaluationDto>> EvaluateContractorAsync(int contractId, CreateContractorEvaluationRequest request, int userId, bool isAdmin);
        Task<ApiResponse<ContractorEvaluationDto?>> GetContractEvaluationAsync(int contractId, int userId, bool isInternalStaff, bool isAdmin);
        Task<ApiResponse<ContractorCreditProfileDto>> GetContractorCreditProfileAsync(int contractorId, int userId, bool isInternalStaff);
    }

    /// <summary>
    /// Giao diện quản lý vòng đời Hợp đồng kinh tế (kế thừa các giao diện phân tách theo nguyên lý ISP để đảm bảo tương thích ngược).
    /// </summary>
    public interface IContractService :
        IContractMilestoneAcceptanceService,
        IContractGuaranteeService,
        IContractAddendumService,
        IContractorEvaluationService
    {
        Task<ApiResponse<ContractListDto>> GetContractsAsync(ContractFilterParams filter, int userId, bool isInternalStaff);
        Task<ApiResponse<ContractDto>> GetContractByIdAsync(int id, int userId, bool isInternalStaff, bool isAdmin = false);
        Task<ApiResponse<FileDownloadResult>> GetScannedContractForDownloadAsync(
            int contractId, int userId, bool isInternalStaff, bool isAdmin = false);
        Task<ApiResponse<AwardedBidInfoDto>> GetAwardedBidForContractAsync(int packageId);
        Task<ApiResponse<ContractDto>> CreateContractFromAwardedBidAsync(CreateContractRequest request, int userId, bool isAdmin);
        Task<ApiResponse<ContractDto>> UpdateContractAsync(int id, UpdateContractRequest request, int userId, bool isAdmin);
        Task<ApiResponse<ContractDto>> ChangeStatusAsync(int id, ChangeContractStatusRequest request, int userId, bool isAdmin);
        Task<ApiResponse<string>> UploadScannedContractAsync(int contractId, IFormFile file, int userId, bool isAdmin);
        Task<ApiResponse<List<ContractSummaryDto>>> GetContractsByContractorIdAsync(int contractorId, int userId, bool isInternalStaff);
    }
}
