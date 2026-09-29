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
        /// Tạo hợp đồng từ kết quả trúng thầu (chỉ Admin/Procurement)
        /// </summary>
        Task<ApiResponse<ContractDto>> CreateContractFromAwardedBidAsync(CreateContractRequest request, int userId);

        /// <summary>
        /// Cập nhật điều khoản, giá trị, thời hạn hợp đồng
        /// </summary>
        Task<ApiResponse<ContractDto>> UpdateContractAsync(int id, UpdateContractRequest request, int userId);

        /// <summary>
        /// Chuyển trạng thái hợp đồng (Draft → Active → Completed / Terminated)
        /// </summary>
        Task<ApiResponse<ContractDto>> ChangeStatusAsync(int id, ChangeContractStatusRequest request, int userId);

        /// <summary>
        /// Upload file PDF hợp đồng có chữ ký scan
        /// </summary>
        Task<ApiResponse<string>> UploadScannedContractAsync(int contractId, IFormFile file, int userId);

        /// <summary>
        /// Thêm mốc thanh toán nghiệm thu
        /// </summary>
        Task<ApiResponse<ContractMilestoneDto>> AddMilestoneAsync(int contractId, CreateMilestoneRequest request, int userId);

        /// <summary>
        /// Cập nhật mốc thanh toán nghiệm thu
        /// </summary>
        Task<ApiResponse<ContractMilestoneDto>> UpdateMilestoneAsync(int contractId, int milestoneId, CreateMilestoneRequest request, int userId);

        /// <summary>
        /// Xóa mốc thanh toán
        /// </summary>
        Task<ApiResponse<bool>> DeleteMilestoneAsync(int contractId, int milestoneId, int userId);

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
        /// Phê duyệt hoặc từ chối biên bản nghiệm thu mốc thanh toán
        /// </summary>
        Task<ApiResponse<MilestoneAcceptanceDto>> ApproveMilestoneAcceptanceAsync(int milestoneId, ApproveMilestoneAcceptanceRequest request, int userId);
    }
}
