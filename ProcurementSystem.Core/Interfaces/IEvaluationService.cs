using ProcurementSystem.Core.DTOs;
using ProcurementSystem.Core.DTOs.Evaluation;

namespace ProcurementSystem.Core.Interfaces
{
    public interface IEvaluationService
    {
        Task<ApiResponse<List<EvaluationCriteriaDto>>> GetCriteriaByPackageAsync(int packageId);
        Task<ApiResponse<EvaluationCriteriaDto>> CreateCriteriaAsync(int packageId, CreateCriteriaRequest request, int userId, bool isAdmin);
        Task<ApiResponse<EvaluationCriteriaDto>> UpdateCriteriaAsync(int criteriaId, UpdateCriteriaRequest request, int userId, bool isAdmin);
        Task<ApiResponse<bool>> DeleteCriteriaAsync(int criteriaId, int userId, bool isAdmin);

        Task<ApiResponse<List<EvaluationScoreDto>>> ScoreSubmissionAsync(int submissionId, ScoreSubmissionRequest request, int evaluatorId);
        Task<ApiResponse<List<EvaluationScoreDto>>> GetScoresBySubmissionAsync(int submissionId, int currentUserId, bool isInternalStaff);
        Task<ApiResponse<List<SubmissionRankingDto>>> GetPackageRankingsAsync(int packageId);
        Task<ApiResponse<EvaluationSummaryDto>> GetEvaluationSummaryAsync(int packageId);
        Task<ApiResponse<AwardedBidDto>> GetAwardedBidAsync(int packageId);
        Task<ApiResponse<bool>> FinalizeEvaluationAsync(int packageId, FinalizeEvaluationRequest request, int userId, bool isAdmin);
    }
}
