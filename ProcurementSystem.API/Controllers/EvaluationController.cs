using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProcurementSystem.Core.DTOs;
using ProcurementSystem.Core.DTOs.Evaluation;
using ProcurementSystem.Core.Interfaces;

namespace ProcurementSystem.API.Controllers
{
    /// <summary>
    /// Phân hệ Đánh giá &amp; Chấm điểm Hồ sơ Dự thầu (Bid Evaluation &amp; Weighted Scoring)
    /// Quản lý thiết lập tiêu chí đánh giá, chấm điểm có trọng số, tự động xếp hạng nhà thầu, phê duyệt trúng thầu và xuất dữ liệu liên thông sang phân hệ Hợp đồng.
    /// </summary>
    [ApiController]
    [Route("api/evaluations")]
    [Produces("application/json")]
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
        /// <param name="packageId">Định danh gói thầu (BidPackageId)</param>
        /// <response code="200">Lấy danh sách tiêu chí đánh giá thành công.</response>
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
        /// </summary>
        /// <remarks>
        /// Quyền hạn: Bắt buộc **Admin** hoặc **Procurement**.
        /// Lưu ý: Tổng trọng số (Weight) của tất cả tiêu chí trong một gói thầu phải bảo đảm đạt 100%.
        /// </remarks>
        /// <param name="packageId">Định danh gói thầu (BidPackageId)</param>
        /// <param name="request">Thông tin tiêu chí mới (tên, mô tả, trọng số, điểm tối đa)</param>
        /// <response code="200">Tạo tiêu chí thành công.</response>
        /// <response code="400">Dữ liệu không hợp lệ hoặc gói thầu đã chuyển sang giai đoạn chấm điểm/hợp đồng.</response>
        /// <response code="401">Chưa đăng nhập.</response>
        /// <response code="403">Từ chối truy cập (không có quyền Admin/Procurement).</response>
        [HttpPost("packages/{packageId:int}/criteria")]
        [Authorize(Roles = "Admin,Procurement")]
        [ProducesResponseType(typeof(ApiResponse<EvaluationCriteriaDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<EvaluationCriteriaDto>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status403Forbidden)]
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
        /// </summary>
        /// <param name="id">Định danh tiêu chí đánh giá</param>
        /// <param name="request">Dữ liệu cập nhật tiêu chí</param>
        /// <response code="200">Cập nhật tiêu chí thành công.</response>
        /// <response code="400">Dữ liệu không hợp lệ hoặc gói thầu đã bị khóa chỉnh sửa.</response>
        /// <response code="401">Chưa đăng nhập.</response>
        /// <response code="403">Từ chối truy cập.</response>
        [HttpPut("criteria/{id:int}")]
        [Authorize(Roles = "Admin,Procurement")]
        [ProducesResponseType(typeof(ApiResponse<EvaluationCriteriaDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<EvaluationCriteriaDto>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status403Forbidden)]
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
        /// </summary>
        /// <param name="id">Định danh tiêu chí đánh giá cần xóa</param>
        /// <response code="200">Xóa tiêu chí thành công.</response>
        /// <response code="400">Không thể xóa khi gói thầu đã bắt đầu chấm điểm hoặc ký hợp đồng.</response>
        /// <response code="401">Chưa đăng nhập.</response>
        /// <response code="403">Từ chối truy cập.</response>
        [HttpDelete("criteria/{id:int}")]
        [Authorize(Roles = "Admin,Procurement")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status403Forbidden)]
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
        /// </summary>
        /// <remarks>
        /// Tự động tính toán điểm tổng hợp theo công thức trọng số chuẩn:
        /// TotalScore = Sum(Tiêu chí điểm trung bình * Trọng số / 100)
        /// Hệ thống tự động cập nhật xếp hạng thứ hạng (Rank 1, 2, 3...) cho toàn bộ hồ sơ trong gói thầu.
        /// Quyền hạn: **Admin**, **Evaluator**.
        /// </remarks>
        /// <param name="submissionId">Định danh hồ sơ dự thầu (BidSubmissionId)</param>
        /// <param name="request">Danh sách điểm và nhận xét cho từng tiêu chí</param>
        /// <response code="200">Ghi nhận phiếu chấm điểm và tính điểm trọng số thành công.</response>
        /// <response code="400">Dữ liệu điểm vượt thang điểm hoặc hồ sơ không ở trạng thái chấm thầu.</response>
        /// <response code="401">Chưa đăng nhập.</response>
        /// <response code="403">Từ chối truy cập (không có quyền Evaluator/Admin).</response>
        [HttpPost("submissions/{submissionId:int}/scores")]
        [Authorize(Roles = "Admin,Evaluator")]
        [ProducesResponseType(typeof(ApiResponse<List<EvaluationScoreDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<List<EvaluationScoreDto>>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status403Forbidden)]
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
        /// </summary>
        /// <param name="submissionId">Định danh hồ sơ dự thầu</param>
        /// <response code="200">Truy xuất bảng điểm chi tiết thành công.</response>
        /// <response code="401">Chưa đăng nhập.</response>
        /// <response code="403">Từ chối truy cập.</response>
        [HttpGet("submissions/{submissionId:int}/scores")]
        [Authorize(Roles = "Admin,Procurement,Evaluator")]
        [ProducesResponseType(typeof(ApiResponse<List<EvaluationScoreDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<ApiResponse<List<EvaluationScoreDto>>>> GetScoresBySubmission(int submissionId)
        {
            var result = await _evaluationService.GetScoresBySubmissionAsync(submissionId);
            return Ok(result);
        }

        /// <summary>
        /// Xuất bảng xếp hạng (Rankings) của tất cả hồ sơ dự thầu trong gói thầu
        /// </summary>
        /// <remarks>
        /// Trả về danh sách hồ sơ kèm điểm tổng kết trọng số, thứ hạng (Rank 1, 2, 3...) và trạng thái thẩm định.
        /// </remarks>
        /// <param name="packageId">Định danh gói thầu</param>
        /// <response code="200">Lấy bảng xếp hạng thành công.</response>
        /// <response code="401">Chưa đăng nhập.</response>
        /// <response code="403">Từ chối truy cập.</response>
        [HttpGet("packages/{packageId:int}/rankings")]
        [Authorize(Roles = "Admin,Procurement,Evaluator")]
        [ProducesResponseType(typeof(ApiResponse<List<SubmissionRankingDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<ApiResponse<List<SubmissionRankingDto>>>> GetPackageRankings(int packageId)
        {
            var result = await _evaluationService.GetPackageRankingsAsync(packageId);
            return Ok(result);
        }

        /// <summary>
        /// Lấy báo cáo thống kê đánh giá tổng hợp của gói thầu (Evaluation Dashboard Summary)
        /// </summary>
        /// <remarks>
        /// Thống kê tổng số lượng hồ sơ, tiến độ chấm điểm, điểm cao nhất/thấp nhất/trung bình và kết quả trúng thầu sơ bộ.
        /// </remarks>
        /// <param name="packageId">Định danh gói thầu</param>
        /// <response code="200">Truy xuất báo cáo tổng hợp thành công.</response>
        /// <response code="401">Chưa đăng nhập.</response>
        /// <response code="403">Từ chối truy cập.</response>
        /// <response code="404">Không tìm thấy gói thầu.</response>
        [HttpGet("packages/{packageId:int}/summary")]
        [Authorize(Roles = "Admin,Procurement,Evaluator")]
        [ProducesResponseType(typeof(ApiResponse<EvaluationSummaryDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<EvaluationSummaryDto>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<ApiResponse<EvaluationSummaryDto>>> GetEvaluationSummary(int packageId)
        {
            var result = await _evaluationService.GetEvaluationSummaryAsync(packageId);
            if (!result.Success) return NotFound(result);
            return Ok(result);
        }

        /// <summary>
        /// Lấy thông tin nhà thầu trúng thầu để bàn giao sang phân hệ Quản lý Hợp đồng (Awarded Bid Export)
        /// </summary>
        /// <remarks>
        /// Cầu nối liên thông phân hệ (Module Integration Bridge):
        /// Đóng gói thông tin gói thầu, nhà thầu trúng thầu (Selected), điểm số và cờ kiểm soát **isReadyForContract** phục vụ phân hệ Hợp đồng lập hợp đồng kinh tế.
        /// </remarks>
        /// <param name="packageId">Định danh gói thầu</param>
        /// <response code="200">Lấy dữ liệu nhà thầu trúng thầu thành công.</response>
        /// <response code="400">Gói thầu chưa hoàn tất quá trình phê duyệt trúng thầu.</response>
        /// <response code="401">Chưa đăng nhập.</response>
        /// <response code="403">Từ chối truy cập.</response>
        /// <response code="404">Không tìm thấy gói thầu.</response>
        [HttpGet("packages/{packageId:int}/awarded-bid")]
        [Authorize(Roles = "Admin,Procurement")]
        [ProducesResponseType(typeof(ApiResponse<AwardedBidDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<AwardedBidDto>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<AwardedBidDto>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<ApiResponse<AwardedBidDto>>> GetAwardedBid(int packageId)
        {
            var result = await _evaluationService.GetAwardedBidAsync(packageId);
            if (!result.Success)
            {
                if (result.Message.Contains("Không tìm thấy"))
                    return NotFound(result);
                return BadRequest(result);
            }
            return Ok(result);
        }

        /// <summary>
        /// Phê duyệt nhà thầu trúng thầu (Selected) và từ chối các hồ sơ còn lại
        /// </summary>
        /// <remarks>
        /// Phê duyệt chính thức hồ sơ có điểm cao nhất trúng thầu (chuyển sang 'Selected'), đánh dấu các hồ sơ khác là 'Rejected'.
        /// Gói thầu sẵn sàng chuyển sang giai đoạn ký kết Hợp đồng.
        /// </remarks>
        /// <param name="packageId">Định danh gói thầu</param>
        /// <param name="selectedSubmissionId">Mã hồ sơ dự thầu được chọn trúng thầu</param>
        /// <response code="200">Phê duyệt kết quả trúng thầu thành công.</response>
        /// <response code="400">Hồ sơ chưa hoàn tất chấm điểm hoặc gói thầu đã bị khóa.</response>
        /// <response code="401">Chưa đăng nhập.</response>
        /// <response code="403">Từ chối truy cập.</response>
        [HttpPost("packages/{packageId:int}/finalize")]
        [Authorize(Roles = "Admin,Procurement")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status403Forbidden)]
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
