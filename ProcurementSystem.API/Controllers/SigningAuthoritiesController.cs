using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProcurementSystem.Core.DTOs;
using ProcurementSystem.Core.DTOs.SigningAuthority;
using ProcurementSystem.Core.Interfaces;

namespace ProcurementSystem.API.Controllers
{
    [ApiController]
    public class SigningAuthoritiesController : ControllerBase
    {
        private readonly ISigningAuthorityService _authorityService;
        private readonly ILogger<SigningAuthoritiesController> _logger;

        public SigningAuthoritiesController(
            ISigningAuthorityService authorityService,
            ILogger<SigningAuthoritiesController> logger)
        {
            _authorityService = authorityService;
            _logger = logger;
        }

        private int? GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(claim, out var id) ? id : null;
        }

        /// <summary>
        /// Quản trị viên Bên mời thầu lập hồ sơ đề nghị cấp thẩm quyền ký cho thành viên
        /// </summary>
        [HttpPost("api/procuring-entities/{procuringEntityId:int}/signing-authorities")]
        [Authorize(Roles = "Procurement,Admin")]
        public async Task<ActionResult<ApiResponse<SigningAuthorityDto>>> RequestAuthority(
            int procuringEntityId, [FromBody] RequestSigningAuthorityRequest request)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized(ApiResponse<SigningAuthorityDto>.Fail("Vui lòng đăng nhập.", statusCode: 401));

            try
            {
                var result = await _authorityService.RequestAuthorityAsync(procuringEntityId, userId.Value, request);
                return Ok(ApiResponse<SigningAuthorityDto>.Ok(result, "Lập hồ sơ đề nghị cấp thẩm quyền ký thành công. Hồ sơ đang chờ Quản trị viên hệ thống thẩm định."));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiResponse<SigningAuthorityDto>.Fail(ex.Message, statusCode: 404));
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, ApiResponse<SigningAuthorityDto>.Forbidden(ex.Message));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponse<SigningAuthorityDto>.Fail(ex.Message, statusCode: 400));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<SigningAuthorityDto>.Fail(ex.Message, statusCode: 400));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lập hồ sơ đề nghị cấp thẩm quyền ký cho đơn vị {EntityId}", procuringEntityId);
                return StatusCode(500, ApiResponse<SigningAuthorityDto>.Fail("Lỗi hệ thống khi xử lý đề nghị cấp thẩm quyền ký.", statusCode: 500));
            }
        }

        /// <summary>
        /// Danh sách hồ sơ thẩm quyền ký của Bên mời thầu
        /// </summary>
        [HttpGet("api/procuring-entities/{procuringEntityId:int}/signing-authorities")]
        [Authorize(Roles = "Procurement,Admin")]
        public async Task<ActionResult<ApiResponse<List<SigningAuthorityDto>>>> GetAuthoritiesByEntity(
            int procuringEntityId)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized(ApiResponse<List<SigningAuthorityDto>>.Fail("Vui lòng đăng nhập.", statusCode: 401));

            try
            {
                var result = await _authorityService.GetAuthoritiesByEntityAsync(procuringEntityId, userId.Value);
                return Ok(ApiResponse<List<SigningAuthorityDto>>.Ok(result));
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, ApiResponse<List<SigningAuthorityDto>>.Forbidden(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách thẩm quyền ký của đơn vị {EntityId}", procuringEntityId);
                return StatusCode(500, ApiResponse<List<SigningAuthorityDto>>.Fail("Lỗi hệ thống khi tải danh sách thẩm quyền ký.", statusCode: 500));
            }
        }

        /// <summary>
        /// Danh sách hồ sơ thẩm quyền ký đang chờ Quản trị viên hệ thống thẩm định
        /// </summary>
        [HttpGet("api/signing-authorities/pending")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<List<SigningAuthorityDto>>>> GetPendingVerifications()
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized(ApiResponse<List<SigningAuthorityDto>>.Fail("Vui lòng đăng nhập.", statusCode: 401));

            try
            {
                var result = await _authorityService.GetPendingVerificationsAsync(userId.Value);
                return Ok(ApiResponse<List<SigningAuthorityDto>>.Ok(result));
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, ApiResponse<List<SigningAuthorityDto>>.Forbidden(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách thẩm quyền chờ thẩm định");
                return StatusCode(500, ApiResponse<List<SigningAuthorityDto>>.Fail("Lỗi hệ thống khi tải danh sách thẩm quyền chờ thẩm định.", statusCode: 500));
            }
        }

        /// <summary>
        /// Chi tiết một hồ sơ thẩm quyền ký
        /// </summary>
        [HttpGet("api/signing-authorities/{id:int}")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<SigningAuthorityDto>>> GetAuthorityById(int id)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized(ApiResponse<SigningAuthorityDto>.Fail("Vui lòng đăng nhập.", statusCode: 401));

            try
            {
                var result = await _authorityService.GetAuthorityByIdAsync(id, userId.Value);
                if (result == null)
                    return NotFound(ApiResponse<SigningAuthorityDto>.Fail("Không tìm thấy thông tin hồ sơ thẩm quyền ký.", statusCode: 404));

                return Ok(ApiResponse<SigningAuthorityDto>.Ok(result));
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, ApiResponse<SigningAuthorityDto>.Forbidden(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xem chi tiết hồ sơ thẩm quyền {AuthorityId}", id);
                return StatusCode(500, ApiResponse<SigningAuthorityDto>.Fail("Lỗi hệ thống khi tải chi tiết hồ sơ thẩm quyền.", statusCode: 500));
            }
        }

        /// <summary>
        /// Quản trị viên hệ thống thẩm định phê duyệt hoặc từ chối hồ sơ thẩm quyền ký
        /// </summary>
        [HttpPut("api/signing-authorities/{id:int}/verify")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<SigningAuthorityDto>>> VerifyAuthority(
            int id, [FromBody] VerifySigningAuthorityRequest request)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized(ApiResponse<SigningAuthorityDto>.Fail("Vui lòng đăng nhập.", statusCode: 401));

            try
            {
                var result = await _authorityService.VerifyAuthorityAsync(id, userId.Value, request);
                var actionMsg = request.IsApproved ? "Phê duyệt thẩm quyền ký thành công." : "Đã từ chối cấp thẩm quyền ký.";
                return Ok(ApiResponse<SigningAuthorityDto>.Ok(result, actionMsg));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiResponse<SigningAuthorityDto>.Fail(ex.Message, statusCode: 404));
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, ApiResponse<SigningAuthorityDto>.Forbidden(ex.Message));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponse<SigningAuthorityDto>.Fail(ex.Message, statusCode: 400));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<SigningAuthorityDto>.Fail(ex.Message, statusCode: 400));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi thẩm định hồ sơ thẩm quyền {AuthorityId}", id);
                return StatusCode(500, ApiResponse<SigningAuthorityDto>.Fail("Lỗi hệ thống khi thẩm định hồ sơ thẩm quyền.", statusCode: 500));
            }
        }

        /// <summary>
        /// Thu hồi thẩm quyền ký đã cấp
        /// </summary>
        [HttpPut("api/signing-authorities/{id:int}/revoke")]
        [Authorize(Roles = "Procurement,Admin")]
        public async Task<ActionResult<ApiResponse<SigningAuthorityDto>>> RevokeAuthority(
            int id, [FromBody] RevokeSigningAuthorityRequest request)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized(ApiResponse<SigningAuthorityDto>.Fail("Vui lòng đăng nhập.", statusCode: 401));

            try
            {
                var result = await _authorityService.RevokeAuthorityAsync(id, userId.Value, request);
                return Ok(ApiResponse<SigningAuthorityDto>.Ok(result, "Thu hồi thẩm quyền ký thành công."));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiResponse<SigningAuthorityDto>.Fail(ex.Message, statusCode: 404));
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, ApiResponse<SigningAuthorityDto>.Forbidden(ex.Message));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<SigningAuthorityDto>.Fail(ex.Message, statusCode: 400));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi thu hồi hồ sơ thẩm quyền {AuthorityId}", id);
                return StatusCode(500, ApiResponse<SigningAuthorityDto>.Fail("Lỗi hệ thống khi thu hồi hồ sơ thẩm quyền.", statusCode: 500));
            }
        }
    }
}
