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

        /// <summary>
        /// Thẩm định và phê duyệt / từ chối hồ sơ năng lực nhà thầu (Chỉ Admin)
        /// </summary>
        Task<ApiResponse<ContractorDto>> VerifyContractorAsync(int contractorId, VerifyContractorRequest request, int reviewerUserId);

        /// <summary>
        /// Tự động tính toán lại điểm đánh giá uy tín (Rating) nhà thầu dựa trên hợp đồng thực tế (P2-11)
        /// </summary>
        Task<ApiResponse<ContractorDto>> RecalculateContractorRatingAsync(int contractorId);

        /// <summary>
        /// Nhập danh sách nhà thầu từ file Excel (.xlsx), kiểm tra trùng MST/Email và báo lỗi chi tiết theo dòng
        /// </summary>
        Task<ApiResponse<ImportContractorResultDto>> ImportContractorsFromExcelAsync(Microsoft.AspNetCore.Http.IFormFile file);

        /// <summary>
        /// Tạo và tải về file mẫu Excel (.xlsx) chuẩn để nhập danh sách nhà thầu
        /// </summary>
        Task<byte[]> GenerateImportTemplateExcelAsync();
    }
}
