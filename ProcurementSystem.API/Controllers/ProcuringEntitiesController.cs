using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProcurementSystem.Core.DTOs;
using ProcurementSystem.Core.DTOs.ProcuringEntity;
using ProcurementSystem.Core.Interfaces;

namespace ProcurementSystem.API.Controllers
{
    /// <summary>
    /// Phân hệ Quản lý Bên mời thầu / Chủ đầu tư (Procuring Entity Management)
    /// Cung cấp các API tra cứu hồ sơ pháp nhân, thẩm định quyết định thành lập và phê duyệt tài khoản Bên mời thầu.
    /// </summary>
    [ApiController]
    [Route("api/procuring-entities")]
    [Route("api/[controller]")]
    [Produces("application/json")]
    [Authorize]
    public class ProcuringEntitiesController : ControllerBase
    {
        private readonly IProcuringEntityService _procuringEntityService;

        public ProcuringEntitiesController(IProcuringEntityService procuringEntityService)
        {
            _procuringEntityService = procuringEntityService;
        }

        /// <summary>
        /// Lấy danh sách Bên mời thầu (phân trang, tìm kiếm từ khóa, mã số thuế, loại hình, trạng thái duyệt)
        /// </summary>
        /// <remarks>Quyền hạn: Chỉ Admin.</remarks>
        [HttpGet]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ApiResponse<PaginatedList<ProcuringEntityDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<ApiResponse<PaginatedList<ProcuringEntityDto>>>> GetProcuringEntities(
            [FromQuery] ProcuringEntityFilterParams filter)
        {
            var result = await _procuringEntityService.GetProcuringEntitiesAsync(filter);
            return Ok(result);
        }

        /// <summary>
        /// Xem chi tiết hồ sơ Bên mời thầu theo ID
        /// </summary>
        /// <remarks>Quyền hạn: Chỉ Admin.</remarks>
        [HttpGet("{id:int}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ApiResponse<ProcuringEntityDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<ProcuringEntityDto>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiResponse<ProcuringEntityDto>>> GetProcuringEntityById(int id)
        {
            var result = await _procuringEntityService.GetProcuringEntityByIdAsync(id);
            if (!result.Success)
            {
                return NotFound(result);
            }
            return Ok(result);
        }

        /// <summary>
        /// Bên mời thầu tự xem thông tin hồ sơ của chính mình từ Token
        /// </summary>
        /// <remarks>Quyền hạn: Procurement, Admin.</remarks>
        [HttpGet("me")]
        [Authorize(Roles = "Procurement,Admin")]
        [ProducesResponseType(typeof(ApiResponse<ProcuringEntityDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<ProcuringEntityDto>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<ProcuringEntityDto>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiResponse<ProcuringEntityDto>>> GetMyProfile()
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized(ApiResponse<ProcuringEntityDto>.Fail("Không xác định được danh tính người dùng từ Token."));
            }

            var result = await _procuringEntityService.GetProcuringEntityByUserIdAsync(userId.Value);
            if (!result.Success)
            {
                return NotFound(result);
            }
            return Ok(result);
        }

        /// <summary>
        /// Thẩm định và phê duyệt hoặc từ chối hồ sơ Bên mời thầu
        /// </summary>
        /// <remarks>Quyền hạn: Chỉ Admin.</remarks>
        [HttpPatch("{id:int}/verify")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ApiResponse<ProcuringEntityDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<ProcuringEntityDto>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<ProcuringEntityDto>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiResponse<ProcuringEntityDto>>> VerifyProcuringEntity(
            int id,
            [FromBody] VerifyProcuringEntityRequest request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return BadRequest(ApiResponse<ProcuringEntityDto>.Fail(errors));
            }

            var adminUserId = GetCurrentUserId();
            if (!adminUserId.HasValue)
            {
                return Unauthorized(ApiResponse<ProcuringEntityDto>.Fail("Không xác định được danh tính quản trị viên từ Token."));
            }

            var result = await _procuringEntityService.VerifyProcuringEntityAsync(id, adminUserId.Value, request);
            if (!result.Success)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }

        /// <summary>
        /// Tải hoặc xem trước tệp Quyết định thành lập / Giấy phép của Bên mời thầu
        /// </summary>
        /// <remarks>Quyền hạn: Admin.</remarks>
        [HttpGet("{id:int}/establishment-file")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetEstablishmentFile(int id)
        {
            var file = await _procuringEntityService.GetEstablishmentFileAsync(id);
            if (file == null)
            {
                return NotFound(ApiResponse<string>.Fail("Tệp Quyết định thành lập không tồn tại hoặc đã bị xóa."));
            }

            return File(file.Value.FileBytes, file.Value.ContentType, file.Value.FileName);
        }

        /// <summary>
        /// Tải hoặc xem trước tệp Quyết định bổ nhiệm người đứng đầu của Bên mời thầu (nếu có)
        /// </summary>
        /// <remarks>Quyền hạn: Admin.</remarks>
        [HttpGet("{id:int}/appointment-file")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetAppointmentFile(int id)
        {
            var file = await _procuringEntityService.GetAppointmentFileAsync(id);
            if (file == null)
            {
                return NotFound(ApiResponse<string>.Fail("Tệp Quyết định bổ nhiệm không tồn tại hoặc đã bị xóa."));
            }

            return File(file.Value.FileBytes, file.Value.ContentType, file.Value.FileName);
        }

        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out int userId) ? userId : null;
        }
    }
}
