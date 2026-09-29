using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProcurementSystem.Core.DTOs;
using ProcurementSystem.Core.DTOs.BidSubmission;
using ProcurementSystem.Core.Interfaces;

namespace ProcurementSystem.API.Controllers
{
    [ApiController]
    [Authorize]
    [Produces("application/json")]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status401Unauthorized)]
    public class BidSubmissionsController : ControllerBase
    {
        private readonly IBidSubmissionService _bidSubmissionService;

        public BidSubmissionsController(IBidSubmissionService bidSubmissionService)
        {
            _bidSubmissionService = bidSubmissionService;
        }

        /// <summary>
        /// Nhà thầu nộp hồ sơ dự thầu kèm file đính kèm cho một gói thầu
        /// </summary>
        /// <param name="bidPackageId">Mã gói thầu cần nộp hồ sơ.</param>
        /// <param name="request">Danh sách file hồ sơ dự thầu và loại file tương ứng.</param>
        /// <remarks>Chỉ tài khoản Contractor được nộp hồ sơ; dữ liệu gửi theo multipart/form-data.</remarks>
        [HttpPost("api/bid-packages/{bidPackageId:int}/submissions")]
        [Authorize(Roles = "Contractor")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(ApiResponse<BidSubmissionDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<BidSubmissionDto>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<BidSubmissionDto>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<BidSubmissionDto>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<BidSubmissionDto>), StatusCodes.Status409Conflict)]
        public async Task<ActionResult<ApiResponse<BidSubmissionDto>>> SubmitBid(
            int bidPackageId,
            [FromForm] CreateBidSubmissionRequest request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return BadRequest(ApiResponse<BidSubmissionDto>.Fail(errors));
            }

            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized(ApiResponse<BidSubmissionDto>.Fail("Không xác định được danh tính người dùng."));
            }

            var result = await _bidSubmissionService.SubmitBidAsync(
                bidPackageId, userId.Value, request.Files, request.FileTypes);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return StatusCode(StatusCodes.Status201Created, result);
        }

        /// <summary>
        /// Xem chi tiết hồ sơ dự thầu (chủ sở hữu hoặc Admin/Procurement/Evaluator)
        /// </summary>
        /// <param name="id">Mã hồ sơ dự thầu cần xem.</param>
        /// <remarks>Áp dụng kiểm soát IDOR: Contractor chỉ xem hồ sơ của chính mình.</remarks>
        [HttpGet("api/submissions/{id:int}")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<BidSubmissionDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<BidSubmissionDto>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<BidSubmissionDto>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<BidSubmissionDto>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiResponse<BidSubmissionDto>>> GetSubmissionById(int id)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized(ApiResponse<BidSubmissionDto>.Fail("Không xác định được danh tính người dùng."));
            }

            var isInternalStaff = User.IsInRole("Admin") || User.IsInRole("Procurement") || User.IsInRole("Evaluator");
            var result = await _bidSubmissionService.GetSubmissionByIdAsync(id, userId.Value, isInternalStaff);

            if (!result.Success)
            {
                if (result.Message.Contains("không có quyền"))
                {
                    return StatusCode(StatusCodes.Status403Forbidden, result);
                }
                return NotFound(result);
            }

            return Ok(result);
        }

        /// <summary>
        /// Xem danh sách hồ sơ dự thầu của một gói thầu (khi gói thầu đã đóng/chấm điểm)
        /// Quyền hạn: Admin, Procurement, Evaluator
        /// </summary>
        /// <param name="bidPackageId">Mã gói thầu cần xem danh sách hồ sơ.</param>
        [HttpGet("api/bid-packages/{bidPackageId:int}/submissions")]
        [Authorize(Roles = "Admin,Procurement,Evaluator")]
        [ProducesResponseType(typeof(ApiResponse<List<BidSubmissionSummaryDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<List<BidSubmissionSummaryDto>>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<List<BidSubmissionSummaryDto>>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<List<BidSubmissionSummaryDto>>), StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<ApiResponse<List<BidSubmissionSummaryDto>>>> GetSubmissionsByPackage(
            int bidPackageId)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized(ApiResponse<List<BidSubmissionSummaryDto>>.Fail("Không xác định được danh tính người dùng."));
            }

            var isInternalStaff = true;
            var result = await _bidSubmissionService.GetSubmissionsByPackageAsync(
                bidPackageId, userId.Value, isInternalStaff);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        /// <summary>
        /// Nhà thầu xem danh sách các gói thầu đã nộp hồ sơ dự thầu (có phân trang)
        /// </summary>
        /// <param name="pageNumber">Trang dữ liệu cần lấy, bắt đầu từ 1.</param>
        /// <param name="pageSize">Số hồ sơ trên mỗi trang.</param>
        [HttpGet("api/my-submissions")]
        [Authorize(Roles = "Contractor")]
        [ProducesResponseType(typeof(ApiResponse<PaginatedList<BidSubmissionSummaryDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<PaginatedList<BidSubmissionSummaryDto>>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<PaginatedList<BidSubmissionSummaryDto>>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<PaginatedList<BidSubmissionSummaryDto>>), StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<ApiResponse<PaginatedList<BidSubmissionSummaryDto>>>> GetMySubmissions(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized(ApiResponse<PaginatedList<BidSubmissionSummaryDto>>.Fail("Không xác định được danh tính người dùng."));
            }

            var result = await _bidSubmissionService.GetMySubmissionsAsync(userId.Value, pageNumber, pageSize);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        /// <summary>
        /// Nhà thầu rút hồ sơ dự thầu trước thời hạn đóng thầu
        /// </summary>
        /// <param name="id">Mã hồ sơ dự thầu cần rút.</param>
        /// <remarks>Chỉ chủ sở hữu hồ sơ được rút hồ sơ và chỉ khi còn trong thời hạn hợp lệ.</remarks>
        [HttpDelete("api/submissions/{id:int}")]
        [Authorize(Roles = "Contractor")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiResponse<bool>>> WithdrawSubmission(int id)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized(ApiResponse<bool>.Fail("Không xác định được danh tính người dùng."));
            }

            var result = await _bidSubmissionService.WithdrawSubmissionAsync(id, userId.Value);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        /// <summary>
        /// Tải tệp tài liệu đính kèm của hồ sơ dự thầu (chống IDOR)
        /// </summary>
        /// <param name="fileId">Mã file đính kèm cần tải xuống.</param>
        /// <remarks>Admin/Procurement/Evaluator được tải file phục vụ chấm thầu; Contractor chỉ tải file thuộc hồ sơ của mình.</remarks>
        [HttpGet("api/submissions/files/{fileId:int}/download")]
        [Authorize]
        [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DownloadSubmissionFile(int fileId)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized(ApiResponse<string>.Fail("Không xác định được danh tính người dùng."));
            }

            var isInternalStaff = User.IsInRole("Admin") || User.IsInRole("Procurement") || User.IsInRole("Evaluator");
            var result = await _bidSubmissionService.GetSubmissionFileForDownloadAsync(fileId, userId.Value, isInternalStaff);

            if (!result.Success)
            {
                if (result.Message.Contains("không có quyền"))
                {
                    return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<string>.Fail(result.Message));
                }
                return NotFound(ApiResponse<string>.Fail(result.Message));
            }

            return PhysicalFile(result.Data!.PhysicalPath, result.Data.ContentType, result.Data.FileName);
        }

        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out int userId) ? userId : null;
        }
    }
}
