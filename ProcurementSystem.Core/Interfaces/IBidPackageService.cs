using Microsoft.AspNetCore.Http;
using ProcurementSystem.Core.DTOs;
using ProcurementSystem.Core.DTOs.BidPackage;

namespace ProcurementSystem.Core.Interfaces
{
    public interface IBidPackageService
    {
        Task<ApiResponse<PaginatedList<BidPackageSummaryDto>>> GetBidPackagesAsync(BidPackageFilterParams filter);
        Task<ApiResponse<BidPackageDto>> GetBidPackageByIdAsync(int id);
        Task<ApiResponse<BidPackageDto>> CreateBidPackageAsync(CreateBidPackageRequest request, int userId);
        Task<ApiResponse<BidPackageDto>> UpdateBidPackageAsync(int id, UpdateBidPackageRequest request, int userId, bool isAdmin);
        Task<ApiResponse<BidPackageDto>> ChangeStatusAsync(int id, ChangeBidPackageStatusRequest request, int userId, bool isAdmin);
        Task<ApiResponse<List<BidDocumentDto>>> UploadDocumentsAsync(int bidPackageId, List<IFormFile> files, int userId, bool isAdmin);
        Task<ApiResponse<bool>> DeleteDocumentAsync(int bidPackageId, int documentId, int userId, bool isAdmin);
    }
}
