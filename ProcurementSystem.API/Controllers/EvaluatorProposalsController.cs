using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProcurementSystem.Core.DTOs;
using ProcurementSystem.Core.DTOs.EvaluatorProposal;
using ProcurementSystem.Core.Interfaces;

namespace ProcurementSystem.API.Controllers
{
    /// <summary>
    /// Phân hệ Đề xuất &amp; Phê duyệt cấp tài khoản Giám khảo (Evaluator Proposals)
    /// Cho phép Bên mời thầu (Procurement) đề xuất chuyên gia và Quản trị viên (Admin) duyệt, cấp tài khoản.
    /// </summary>
    [ApiController]
    [Route("api/evaluator-proposals")]
    [Produces("application/json")]
    [Authorize]
    public class EvaluatorProposalsController : ControllerBase
    {
        private readonly IEvaluatorProposalService _proposalService;

        public EvaluatorProposalsController(IEvaluatorProposalService proposalService)
        {
            _proposalService = proposalService;
        }

        /// <summary>
        /// Lấy danh sách đề xuất Giám khảo (phân trang, tìm kiếm, lọc theo trạng thái)
        /// </summary>
        /// <param name="filter">Bộ lọc danh sách đề xuất (từ khóa, trạng thái, trang)</param>
        /// <remarks>Quyền hạn: Admin (xem tất cả), Procurement (xem đề xuất do mình gửi).</remarks>
        [HttpGet]
        [Authorize(Roles = "Admin,Procurement")]
        [ProducesResponseType(typeof(ApiResponse<PaginatedList<EvaluatorProposalDto>>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<PaginatedList<EvaluatorProposalDto>>>> GetProposals(
            [FromQuery] EvaluatorProposalFilterParams filter)
        {
            var userId = GetCurrentUserId() ?? 0;
            var userRole = GetCurrentUserRole() ?? string.Empty;

            var result = await _proposalService.GetProposalsAsync(filter, userId, userRole);
            return Ok(result);
        }

        /// <summary>
        /// Xem chi tiết một đề xuất Giám khảo theo ID
        /// </summary>
        /// <param name="id">Mã định danh đề xuất</param>
        /// <remarks>Quyền hạn: Admin, Procurement (người tạo đề xuất).</remarks>
        [HttpGet("{id:int}")]
        [Authorize(Roles = "Admin,Procurement")]
        [ProducesResponseType(typeof(ApiResponse<EvaluatorProposalDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<EvaluatorProposalDto>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiResponse<EvaluatorProposalDto>>> GetProposalById(int id)
        {
            var userId = GetCurrentUserId() ?? 0;
            var userRole = GetCurrentUserRole() ?? string.Empty;

            var result = await _proposalService.GetProposalByIdAsync(id, userId, userRole);
            if (!result.Success)
            {
                return NotFound(result);
            }
            return Ok(result);
        }

        /// <summary>
        /// Bên mời thầu gửi đề xuất Giám khảo mới kèm thông tin cá nhân và Chứng chỉ nghiệp vụ đấu thầu
        /// </summary>
        /// <param name="request">Dữ liệu đề xuất (Họ tên, Email, SĐT, Chuyên môn, Nơi công tác, Tệp chứng chỉ...)</param>
        /// <remarks>Quyền hạn: Chỉ Bên mời thầu (Procurement).</remarks>
        [HttpPost]
        [Authorize(Roles = "Procurement")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(ApiResponse<EvaluatorProposalDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<EvaluatorProposalDto>), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ApiResponse<EvaluatorProposalDto>>> CreateProposal(
            [FromForm] CreateEvaluatorProposalRequest request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return BadRequest(ApiResponse<EvaluatorProposalDto>.Fail(errors));
            }

            var userId = GetCurrentUserId() ?? 0;
            var result = await _proposalService.CreateProposalAsync(request, userId);
            if (!result.Success)
            {
                return BadRequest(result);
            }

            return StatusCode(StatusCodes.Status201Created, result);
        }

        /// <summary>
        /// Quản trị viên (Admin) phê duyệt đề xuất, ấn định mật khẩu khởi tạo và tạo tài khoản Giám khảo
        /// </summary>
        /// <param name="id">Mã đề xuất</param>
        /// <param name="request">Mật khẩu khởi tạo và ghi chú phê duyệt</param>
        /// <remarks>Quyền hạn: Chỉ Quản trị viên (Admin).</remarks>
        [HttpPost("{id:int}/approve")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ApiResponse<EvaluatorProposalDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<EvaluatorProposalDto>), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ApiResponse<EvaluatorProposalDto>>> ApproveProposal(
            int id,
            [FromBody] ApproveEvaluatorProposalRequest request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return BadRequest(ApiResponse<EvaluatorProposalDto>.Fail(errors));
            }

            var adminUserId = GetCurrentUserId() ?? 0;
            var result = await _proposalService.ApproveProposalAsync(id, request, adminUserId);
            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        /// <summary>
        /// Quản trị viên (Admin) từ chối đề xuất Giám khảo kèm lý do giải trình
        /// </summary>
        /// <param name="id">Mã đề xuất</param>
        /// <param name="request">Lý do từ chối của Admin</param>
        /// <remarks>Quyền hạn: Chỉ Quản trị viên (Admin).</remarks>
        [HttpPost("{id:int}/reject")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ApiResponse<EvaluatorProposalDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<EvaluatorProposalDto>), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ApiResponse<EvaluatorProposalDto>>> RejectProposal(
            int id,
            [FromBody] RejectEvaluatorProposalRequest request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return BadRequest(ApiResponse<EvaluatorProposalDto>.Fail(errors));
            }

            var adminUserId = GetCurrentUserId() ?? 0;
            var result = await _proposalService.RejectProposalAsync(id, request, adminUserId);
            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        /// <summary>
        /// Tải về hoặc xem tệp Chứng chỉ nghiệp vụ đấu thầu / Bằng cấp chuyên môn của đề xuất
        /// </summary>
        /// <param name="id">Mã đề xuất</param>
        /// <remarks>Quyền hạn: Admin, Procurement (người tạo đề xuất).</remarks>
        [HttpGet("{id:int}/certificate-file")]
        [Authorize(Roles = "Admin,Procurement")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DownloadCertificateFile(int id)
        {
            var userId = GetCurrentUserId() ?? 0;
            var userRole = GetCurrentUserRole() ?? string.Empty;

            var result = await _proposalService.GetCertificateFileForDownloadAsync(id, userId, userRole);
            if (!result.Success || result.Data == default)
            {
                return NotFound(ApiResponse<string>.Fail(result.Message));
            }

            return PhysicalFile(result.Data.PhysicalPath, result.Data.ContentType, result.Data.FileName);
        }

        private int? GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(claim, out var id) ? id : null;
        }

        private string? GetCurrentUserRole()
        {
            return User.FindFirst(ClaimTypes.Role)?.Value;
        }
    }
}
