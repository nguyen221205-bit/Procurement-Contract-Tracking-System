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
        Task<ApiResponse<List<EvaluationScoreDto>>> GetScoresBySubmissionAsync(int submissionId);
        Task<ApiResponse<List<SubmissionRankingDto>>> GetPackageRankingsAsync(int packageId);
        Task<ApiResponse<bool>> FinalizeEvaluationAsync(int packageId, int selectedSubmissionId, int userId, bool isAdmin);
    }
}
