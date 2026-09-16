using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProcurementSystem.Core.DTOs;
using ProcurementSystem.Core.DTOs.Evaluation;
using ProcurementSystem.Core.Interfaces;

namespace ProcurementSystem.API.Controllers
{
    [ApiController]
    [Route("api/evaluations")]
    public class EvaluationController : ControllerBase
    {
        private readonly IEvaluationService _evaluationService;

        public EvaluationController(IEvaluationService evaluationService)
        {
            _evaluationService = evaluationService;
        }

        /// <summary>
        /// Xem danh sách tiêu chí đánh giá của một gói thầu
        /// </summary>
        [HttpGet("packages/{packageId:int}/criteria")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<List<EvaluationCriteriaDto>>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<List<EvaluationCriteriaDto>>>> GetCriteriaByPackage(int packageId)
        {
            var result = await _evaluationService.GetCriteriaByPackageAsync(packageId);
            return Ok(result);
        }

        /// <summary>
        /// Tạo mới tiêu chí đánh giá cho gói thầu (tên, thang điểm, trọng số)
        /// Quyền hạn: Admin, Procurement
        /// </summary>
        [HttpPost("packages/{packageId:int}/criteria")]
        [Authorize(Roles = "Admin,Procurement")]
        [ProducesResponseType(typeof(ApiResponse<EvaluationCriteriaDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<EvaluationCriteriaDto>), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ApiResponse<EvaluationCriteriaDto>>> CreateCriteria(int packageId, [FromBody] CreateCriteriaRequest request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<EvaluationCriteriaDto>.Fail(errors));
            }

            var userId = GetCurrentUserId();
            var isAdmin = User.IsInRole("Admin");

            var result = await _evaluationService.CreateCriteriaAsync(packageId, request, userId, isAdmin);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        /// <summary>
        /// Cập nhật thông tin tiêu chí đánh giá (khi gói thầu chưa bắt đầu chấm điểm)
        /// Quyền hạn: Admin, Procurement
        /// </summary>
        [HttpPut("criteria/{id:int}")]
        [Authorize(Roles = "Admin,Procurement")]
        [ProducesResponseType(typeof(ApiResponse<EvaluationCriteriaDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<EvaluationCriteriaDto>), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ApiResponse<EvaluationCriteriaDto>>> UpdateCriteria(int id, [FromBody] UpdateCriteriaRequest request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<EvaluationCriteriaDto>.Fail(errors));
            }

            var userId = GetCurrentUserId();
            var isAdmin = User.IsInRole("Admin");

            var result = await _evaluationService.UpdateCriteriaAsync(id, request, userId, isAdmin);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        /// <summary>
        /// Xóa tiêu chí đánh giá khỏi gói thầu
        /// Quyền hạn: Admin, Procurement
        /// </summary>
        [HttpDelete("criteria/{id:int}")]
        [Authorize(Roles = "Admin,Procurement")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ApiResponse<bool>>> DeleteCriteria(int id)
        {
            var userId = GetCurrentUserId();
            var isAdmin = User.IsInRole("Admin");

            var result = await _evaluationService.DeleteCriteriaAsync(id, userId, isAdmin);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        /// <summary>
        /// Giám khảo / Ban thẩm định chấm điểm chi tiết cho một hồ sơ dự thầu
        /// Tự động tính điểm tổng hợp theo trọng số và phân định thứ hạng (Rank)
        /// Quyền hạn: Admin, Evaluator
        /// </summary>
        [HttpPost("submissions/{submissionId:int}/scores")]
        [Authorize(Roles = "Admin,Evaluator")]
        [ProducesResponseType(typeof(ApiResponse<List<EvaluationScoreDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<List<EvaluationScoreDto>>), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ApiResponse<List<EvaluationScoreDto>>>> ScoreSubmission(int submissionId, [FromBody] ScoreSubmissionRequest request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<List<EvaluationScoreDto>>.Fail(errors));
            }

            var evaluatorId = GetCurrentUserId();
            var result = await _evaluationService.ScoreSubmissionAsync(submissionId, request, evaluatorId);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        /// <summary>
        /// Xem bảng điểm chi tiết và nhận xét của từng tiêu chí cho một hồ sơ dự thầu
        /// Quyền hạn: Admin, Procurement, Evaluator
        /// </summary>
        [HttpGet("submissions/{submissionId:int}/scores")]
        [Authorize(Roles = "Admin,Procurement,Evaluator")]
        [ProducesResponseType(typeof(ApiResponse<List<EvaluationScoreDto>>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<List<EvaluationScoreDto>>>> GetScoresBySubmission(int submissionId)
        {
            var result = await _evaluationService.GetScoresBySubmissionAsync(submissionId);
            return Ok(result);
        }

        /// <summary>
        /// Xuất bảng xếp hạng (Rankings) của tất cả hồ sơ dự thầu trong gói thầu
        /// Bao gồm điểm tổng hợp, thứ hạng (Rank 1, 2, 3...) và trạng thái
        /// Quyền hạn: Admin, Procurement, Evaluator
        /// </summary>
        [HttpGet("packages/{packageId:int}/rankings")]
        [Authorize(Roles = "Admin,Procurement,Evaluator")]
        [ProducesResponseType(typeof(ApiResponse<List<SubmissionRankingDto>>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<List<SubmissionRankingDto>>>> GetPackageRankings(int packageId)
        {
            var result = await _evaluationService.GetPackageRankingsAsync(packageId);
            return Ok(result);
        }

        /// <summary>
        /// Phê duyệt nhà thầu trúng thầu (Selected) và từ chối các hồ sơ còn lại
        /// Sẵn sàng chuyển sang giai đoạn ký kết Hợp đồng
        /// Quyền hạn: Admin, Procurement
        /// </summary>
        [HttpPost("packages/{packageId:int}/finalize")]
        [Authorize(Roles = "Admin,Procurement")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ApiResponse<bool>>> FinalizeEvaluation(int packageId, [FromQuery] int selectedSubmissionId)
        {
            var userId = GetCurrentUserId();
            var isAdmin = User.IsInRole("Admin");

            var result = await _evaluationService.FinalizeEvaluationAsync(packageId, selectedSubmissionId, userId, isAdmin);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            int.TryParse(claim, out int userId);
            return userId;
        }
    }
}
