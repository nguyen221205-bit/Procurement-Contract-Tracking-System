using Microsoft.AspNetCore.Http;
using ProcurementSystem.Core.DTOs;
using ProcurementSystem.Core.DTOs.BidSubmission;
using ProcurementSystem.Core.Enums;

namespace ProcurementSystem.Core.Interfaces
{
    public interface IBidSubmissionService
    {
        /// <summary>
        /// Nhà thầu nộp hồ sơ dự thầu kèm file đính kèm
        /// </summary>
        Task<ApiResponse<BidSubmissionDto>> SubmitBidAsync(int bidPackageId, int userId, List<IFormFile> files, List<SubmissionFileType> fileTypes);

        /// <summary>
        /// Xem chi tiết hồ sơ dự thầu (phân quyền: chủ sở hữu hoặc nhân sự nội bộ)
        /// </summary>
        Task<ApiResponse<BidSubmissionDto>> GetSubmissionByIdAsync(int id, int userId, bool isInternalStaff);

        /// <summary>
        /// Xem danh sách hồ sơ dự thầu của một gói thầu (bảo mật giá thầu khi gói thầu còn Open)
        /// </summary>
        Task<ApiResponse<List<BidSubmissionSummaryDto>>> GetSubmissionsByPackageAsync(int bidPackageId, int userId, bool isInternalStaff);

        /// <summary>
        /// Nhà thầu xem danh sách các gói thầu đã nộp hồ sơ (phân trang)
        /// </summary>
        Task<ApiResponse<PaginatedList<BidSubmissionSummaryDto>>> GetMySubmissionsAsync(int userId, int pageNumber, int pageSize);

        /// <summary>
        /// Nhà thầu rút hồ sơ dự thầu trước thời hạn đóng thầu
        /// </summary>
        Task<ApiResponse<bool>> WithdrawSubmissionAsync(int submissionId, int userId);
    }
}
