using Microsoft.AspNetCore.Http;
using ProcurementSystem.Core.DTOs;
using ProcurementSystem.Core.DTOs.BidPackage;
using ProcurementSystem.Core.Enums;

namespace ProcurementSystem.Core.Interfaces
{
    public interface IBidPackageService
    {
        Task<ApiResponse<List<PackageEntityOptionDto>>> GetCreatableEntitiesAsync(int userId);
        Task<ApiResponse<List<BidPackageDto>>> GetOwnershipReviewsAsync();
        Task<ApiResponse<BidPackageDto>> ReviewOwnershipAsync(int id, ReviewPackageOwnershipRequest request, int userId);
        Task<ApiResponse<PaginatedList<BidPackageSummaryDto>>> GetBidPackagesAsync(BidPackageFilterParams filter);
        Task<ApiResponse<BidPackageDto>> GetBidPackageByIdAsync(int id);
        Task<ApiResponse<BidPackageDto>> CreateBidPackageAsync(CreateBidPackageRequest request, int userId);
        Task<ApiResponse<BidPackageDto>> UpdateBidPackageAsync(int id, UpdateBidPackageRequest request, int userId, bool isAdmin);
        Task<ApiResponse<BidPackageDto>> ChangeStatusAsync(int id, ChangeBidPackageStatusRequest request, int userId, bool isAdmin);
        Task<ApiResponse<List<BidDocumentDto>>> UploadDocumentsAsync(int bidPackageId, List<IFormFile> files, int userId, bool isAdmin);
        Task<ApiResponse<bool>> DeleteDocumentAsync(int bidPackageId, int documentId, int userId, bool isAdmin);
        Task<ApiResponse<List<PackageEvaluatorDto>>> GetPackageEvaluatorsAsync(int packageId);
        Task<ApiResponse<PackageEvaluatorDto>> AssignEvaluatorAsync(int packageId, AssignEvaluatorRequest request, int currentUserId, bool isAdmin);
        Task<ApiResponse<bool>> RemoveEvaluatorAsync(int packageId, int evaluatorId, int currentUserId, bool isAdmin);
        Task<ApiResponse<PackageEvaluatorDto>> ReplaceEvaluatorAsync(int packageId, ReplaceEvaluatorRequest request, int currentUserId, bool isAdmin);
        Task<ApiResponse<List<PackageStatusApprovalRequestDto>>> GetStatusApprovalRequestsAsync(int? packageId = null, ApprovalStatus? status = null);
        Task<ApiResponse<PackageStatusApprovalRequestDto>> ReviewStatusApprovalRequestAsync(int requestId, ReviewPackageStatusApprovalRequest request, int adminUserId);
    }
}
