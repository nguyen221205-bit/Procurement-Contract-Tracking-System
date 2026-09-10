using ProcurementSystem.Core.DTOs;
using ProcurementSystem.Core.DTOs.Contractor;

namespace ProcurementSystem.Core.Interfaces
{
    public interface IContractorService
    {
        /// <summary>
        /// Lấy danh sách nhà thầu có phân trang, tìm kiếm và sắp xếp
        /// </summary>
        Task<ApiResponse<PaginatedList<ContractorDto>>> GetContractorsAsync(ContractorFilterParams filter);

        /// <summary>
        /// Xem chi tiết hồ sơ nhà thầu theo ID
        /// </summary>
        Task<ApiResponse<ContractorDto>> GetContractorByIdAsync(int id);

        /// <summary>
        /// Lấy hồ sơ nhà thầu hiện tại theo UserId đăng nhập
        /// </summary>
        Task<ApiResponse<ContractorDto>> GetCurrentContractorProfileAsync(int userId);

        /// <summary>
        /// Nhà thầu tự cập nhật hồ sơ của chính mình
        /// </summary>
        Task<ApiResponse<ContractorDto>> UpdateCurrentContractorProfileAsync(int userId, UpdateContractorProfileRequest request);

        /// <summary>
        /// Cập nhật điểm đánh giá năng lực nhà thầu
        /// </summary>
        Task<ApiResponse<ContractorDto>> UpdateContractorRatingAsync(int contractorId, UpdateContractorRatingRequest request);
    }
}
