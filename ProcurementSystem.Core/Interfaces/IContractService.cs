using Microsoft.AspNetCore.Http;
using ProcurementSystem.Core.DTOs;
using ProcurementSystem.Core.DTOs.Contract;

namespace ProcurementSystem.Core.Interfaces
{
    public interface IContractService
    {
        /// <summary>
        /// Danh sách hợp đồng phân trang (Admin/Procurement xem tất cả, Contractor chỉ xem hợp đồng của mình)
        /// </summary>
        Task<ApiResponse<PaginatedList<ContractSummaryDto>>> GetContractsAsync(ContractFilterParams filter, int userId, bool isInternalStaff);

        /// <summary>
        /// Xem chi tiết hợp đồng kèm mốc thanh toán
        /// </summary>
        Task<ApiResponse<ContractDto>> GetContractByIdAsync(int id, int userId, bool isInternalStaff);

        /// <summary>
        /// Lấy thông tin nhà thầu trúng thầu để pre-fill sang hợp đồng
        /// </summary>
        Task<ApiResponse<AwardedBidInfoDto>> GetAwardedBidForContractAsync(int packageId);

        /// <summary>
        /// Tạo hợp đồng từ kết quả trúng thầu (chỉ Admin/Procurement phụ trách gói thầu)
        /// </summary>
        Task<ApiResponse<ContractDto>> CreateContractFromAwardedBidAsync(CreateContractRequest request, int userId, bool isAdmin);

        /// <summary>
        /// Cập nhật điều khoản, giá trị, thời hạn hợp đồng (chỉ Admin/Procurement phụ trách gói thầu)
        /// </summary>
        Task<ApiResponse<ContractDto>> UpdateContractAsync(int id, UpdateContractRequest request, int userId, bool isAdmin);

        /// <summary>
        /// Chuyển trạng thái hợp đồng (Draft → Active → Completed / Terminated) (chỉ Admin/Procurement phụ trách gói thầu)
        /// </summary>
        Task<ApiResponse<ContractDto>> ChangeStatusAsync(int id, ChangeContractStatusRequest request, int userId, bool isAdmin);

        /// <summary>
        /// Upload file PDF hợp đồng có chữ ký scan (chỉ Admin/Procurement phụ trách gói thầu)
        /// </summary>
        Task<ApiResponse<string>> UploadScannedContractAsync(int contractId, IFormFile file, int userId, bool isAdmin);

        /// <summary>
        /// Thêm mốc thanh toán nghiệm thu (chỉ Admin/Procurement phụ trách gói thầu)
        /// </summary>
        Task<ApiResponse<ContractMilestoneDto>> AddMilestoneAsync(int contractId, CreateMilestoneRequest request, int userId, bool isAdmin);

        /// <summary>
        /// Cập nhật mốc thanh toán nghiệm thu (chỉ Admin/Procurement phụ trách gói thầu)
        /// </summary>
        Task<ApiResponse<ContractMilestoneDto>> UpdateMilestoneAsync(int contractId, int milestoneId, CreateMilestoneRequest request, int userId, bool isAdmin);

        /// <summary>
        /// Xóa mốc thanh toán (chỉ Admin/Procurement phụ trách gói thầu)
        /// </summary>
        Task<ApiResponse<bool>> DeleteMilestoneAsync(int contractId, int milestoneId, int userId, bool isAdmin);

        /// <summary>
        /// Tra cứu toàn bộ lịch sử hợp đồng theo mã nhà thầu (kiểm tra phân quyền chống IDOR)
        /// </summary>
        Task<ApiResponse<List<ContractSummaryDto>>> GetContractsByContractorIdAsync(int contractorId, int userId, bool isInternalStaff);

        /// <summary>
        /// Nhà thầu gửi báo cáo tiến độ tuần cho hợp đồng
        /// </summary>
        Task<ApiResponse<ProgressUpdateDto>> AddProgressUpdateAsync(int contractId, CreateProgressUpdateRequest request, int userId);

        /// <summary>
        /// Nhà thầu gửi báo cáo tiến độ tuần theo mốc thanh toán
        /// </summary>
        Task<ApiResponse<ProgressUpdateDto>> AddMilestoneProgressUpdateAsync(int milestoneId, CreateProgressUpdateRequest request, int userId);

        /// <summary>
        /// Phê duyệt hoặc từ chối biên bản nghiệm thu mốc thanh toán (chỉ Admin/Procurement phụ trách gói thầu)
        /// </summary>
        Task<ApiResponse<MilestoneAcceptanceDto>> ApproveMilestoneAcceptanceAsync(int milestoneId, ApproveMilestoneAcceptanceRequest request, int userId, bool isAdmin);

        #region Contract Guarantees (Bảo lãnh hợp đồng)
        /// <summary>
        /// Lấy danh sách bảo lãnh của hợp đồng
        /// </summary>
        Task<ApiResponse<List<ContractGuaranteeDto>>> GetContractGuaranteesAsync(int contractId, int userId, bool isInternalStaff, bool isAdmin);

        /// <summary>
        /// Thêm mới chứng thư bảo lãnh (chỉ Admin/Procurement phụ trách gói thầu)
        /// </summary>
        Task<ApiResponse<ContractGuaranteeDto>> AddContractGuaranteeAsync(int contractId, CreateContractGuaranteeRequest request, int userId, bool isAdmin);

        /// <summary>
        /// Cập nhật trạng thái chứng thư bảo lãnh (Valid, Released, Claimed, Expired)
        /// </summary>
        Task<ApiResponse<ContractGuaranteeDto>> UpdateGuaranteeStatusAsync(int contractId, int guaranteeId, UpdateGuaranteeStatusRequest request, int userId, bool isAdmin);

        /// <summary>
        /// Upload file scan chứng thư bảo lãnh ngân hàng
        /// </summary>
        Task<ApiResponse<string>> UploadGuaranteeDocumentAsync(int contractId, int guaranteeId, IFormFile file, int userId, bool isAdmin);

        /// <summary>
        /// Tải/xem file chứng thư bảo lãnh an toàn
        /// </summary>
        Task<(string PhysicalPath, string ContentType, string FileName)?> GetGuaranteeDocumentForDownloadAsync(int contractId, int guaranteeId, int userId, bool isInternalStaff, bool isAdmin);
        #endregion

        #region Contract Addenda (Phụ lục hợp đồng)
        /// <summary>
        /// Lấy danh sách phụ lục của hợp đồng
        /// </summary>
        Task<ApiResponse<List<ContractAddendumDto>>> GetContractAddendaAsync(int contractId, int userId, bool isInternalStaff, bool isAdmin);

        /// <summary>
        /// Tạo mới phụ lục hợp đồng (gia hạn thời gian, điều chỉnh giá trị, thay đổi phạm vi)
        /// </summary>
        Task<ApiResponse<ContractAddendumDto>> CreateContractAddendumAsync(int contractId, CreateContractAddendumRequest request, int userId, bool isAdmin);

        /// <summary>
        /// Phê duyệt phụ lục hợp đồng (tự động cập nhật giá trị/ngày kết thúc hợp đồng)
        /// </summary>
        Task<ApiResponse<ContractAddendumDto>> ApproveContractAddendumAsync(int contractId, int addendumId, ApproveContractAddendumRequest request, int userId, bool isAdmin);

        /// <summary>
        /// Upload file scan phụ lục hợp đồng đã ký
        /// </summary>
        Task<ApiResponse<string>> UploadAddendumDocumentAsync(int contractId, int addendumId, IFormFile file, int userId, bool isAdmin);

        /// <summary>
        /// Tải/xem file phụ lục hợp đồng an toàn
        /// </summary>
        Task<(string PhysicalPath, string ContentType, string FileName)?> GetAddendumDocumentForDownloadAsync(int contractId, int addendumId, int userId, bool isInternalStaff, bool isAdmin);
        #endregion

        #region Contractor Evaluation (Đánh giá uy tín & xếp hạng nhà thầu)
        /// <summary>
        /// Đánh giá nghiệm thu tổng kết nhà thầu khi hợp đồng hoàn thành (thang 100 điểm)
        /// </summary>
        Task<ApiResponse<ContractorEvaluationDto>> EvaluateContractorAsync(int contractId, CreateContractorEvaluationRequest request, int userId, bool isAdmin);

        /// <summary>
        /// Xem đánh giá nghiệm thu của hợp đồng
        /// </summary>
        Task<ApiResponse<ContractorEvaluationDto?>> GetContractEvaluationAsync(int contractId, int userId, bool isInternalStaff, bool isAdmin);

        /// <summary>
        /// Tra cứu hồ sơ uy tín tín nhiệm tổng hợp của nhà thầu (Credit Profile và Performance Rating)
        /// </summary>
        Task<ApiResponse<ContractorCreditProfileDto>> GetContractorCreditProfileAsync(int contractorId, int userId, bool isInternalStaff);
        #endregion
    }
}
