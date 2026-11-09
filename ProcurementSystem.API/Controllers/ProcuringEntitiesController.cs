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
        private readonly IProcuringEntityMemberService _memberService;

        public ProcuringEntitiesController(
            IProcuringEntityService procuringEntityService,
            IProcuringEntityMemberService memberService)
        {
            _procuringEntityService = procuringEntityService;
            _memberService = memberService;
        }

        /// <summary>
        /// Lấy danh sách Bên mời thầu (phân trang, tìm kiếm từ khóa, mã số thuế, loại hình, trạng thái duyệt)
        /// </summary>
        /// <remarks>Quyền hạn: Chỉ Admin.</remarks>
        [HttpGet]
        [Authorize(Roles = "Admin,Procurement")]
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
        [Authorize(Roles = "Admin,Procurement")]
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
        /// Bên mời thầu cập nhật hồ sơ pháp nhân của chính mình (hỗ trợ nộp lại hồ sơ khi bị từ chối)
        /// </summary>
        /// <remarks>Quyền hạn: Procurement.</remarks>
        [HttpPut("me")]
        [Authorize(Roles = "Procurement")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(ApiResponse<ProcuringEntityDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<ProcuringEntityDto>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<ProcuringEntityDto>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<ProcuringEntityDto>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiResponse<ProcuringEntityDto>>> UpdateMyProfile(
            [FromForm] UpdateProcuringEntityProfileRequest request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return BadRequest(ApiResponse<ProcuringEntityDto>.Fail(errors));
            }

            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized(ApiResponse<ProcuringEntityDto>.Fail("Không xác định được danh tính người dùng từ Token."));
            }

            var result = await _procuringEntityService.UpdateCurrentProfileAsync(userId.Value, request);
            if (!result.Success)
            {
                return BadRequest(result);
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
        /// <remarks>Quyền hạn: Admin, Procurement (đại diện bên mời thầu sở hữu hồ sơ).</remarks>
        [HttpGet("{id:int}/establishment-file")]
        [Authorize(Roles = "Admin,Procurement")]
        [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetEstablishmentFile(int id)
        {
            if (User.IsInRole("Procurement") && !User.IsInRole("Admin"))
            {
                var currentUserId = GetCurrentUserId();
                var entityResult = await _procuringEntityService.GetProcuringEntityByIdAsync(id);
                if (!entityResult.Success || entityResult.Data == null || entityResult.Data.UserId != currentUserId)
                {
                    return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<string>.Fail("Bạn không có quyền xem tệp quyết định thành lập của bên mời thầu khác."));
                }
            }

            var file = await _procuringEntityService.GetEstablishmentFileAsync(id);
            if (file == null)
            {
                return NotFound(ApiResponse<string>.Fail("Tệp Quyết định thành lập không tồn tại hoặc đã bị xóa."));
            }

            if (file.IsRedirect)
            {
                return Redirect(file.RedirectUrl!);
            }

            if (file.IsStream)
            {
                return File(file.FileStream!, file.ContentType, file.FileName);
            }

            if (!System.IO.File.Exists(file.PhysicalPath))
            {
                return NotFound(ApiResponse<string>.Fail("Tệp Quyết định thành lập không tồn tại trên hệ thống lưu trữ."));
            }

            return PhysicalFile(file.PhysicalPath!, file.ContentType, file.FileName);
        }

        /// <summary>
        /// Tải hoặc xem trước tệp Quyết định bổ nhiệm người đứng đầu của Bên mời thầu (nếu có)
        /// </summary>
        /// <remarks>Quyền hạn: Admin, Procurement (đại diện bên mời thầu sở hữu hồ sơ).</remarks>
        [HttpGet("{id:int}/appointment-file")]
        [Authorize(Roles = "Admin,Procurement")]
        [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetAppointmentFile(int id)
        {
            if (User.IsInRole("Procurement") && !User.IsInRole("Admin"))
            {
                var currentUserId = GetCurrentUserId();
                var entityResult = await _procuringEntityService.GetProcuringEntityByIdAsync(id);
                if (!entityResult.Success || entityResult.Data == null || entityResult.Data.UserId != currentUserId)
                {
                    return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<string>.Fail("Bạn không có quyền xem tệp quyết định bổ nhiệm của bên mời thầu khác."));
                }
            }

            var file = await _procuringEntityService.GetAppointmentFileAsync(id);
            if (file == null)
            {
                return NotFound(ApiResponse<string>.Fail("Tệp Quyết định bổ nhiệm không tồn tại hoặc đã bị xóa."));
            }

            if (file.IsRedirect)
            {
                return Redirect(file.RedirectUrl!);
            }

            if (file.IsStream)
            {
                return File(file.FileStream!, file.ContentType, file.FileName);
            }

            if (!System.IO.File.Exists(file.PhysicalPath))
            {
                return NotFound(ApiResponse<string>.Fail("Tệp Quyết định bổ nhiệm không tồn tại trên hệ thống lưu trữ."));
            }

            return PhysicalFile(file.PhysicalPath!, file.ContentType, file.FileName);
        }

        // ==========================================
        // PHÂN HỆ QUẢN LÝ THÀNH VIÊN & LỜI MỜI (MEMBERSHIP & INVITATIONS)
        // ==========================================

        /// <summary>
        /// Lấy danh sách thành viên của Bên mời thầu
        /// </summary>
        [HttpGet("{id:int}/members")]
        [Authorize(Roles = "Admin,Procurement")]
        public async Task<ActionResult<ApiResponse<List<ProcuringEntityMemberDto>>>> GetMembers(int id)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue) return Unauthorized(ApiResponse<List<ProcuringEntityMemberDto>>.Fail("Vui lòng đăng nhập.", statusCode: 401));

            var result = await _memberService.GetMembersAsync(id, userId.Value, User.IsInRole("Admin"));
            if (!result.Success) return StatusCode(result.StatusCode, result);
            return Ok(result);
        }

        /// <summary>
        /// Lấy danh sách lời mời tham gia của Bên mời thầu
        /// </summary>
        [HttpGet("{id:int}/invitations")]
        [Authorize(Roles = "Admin,Procurement")]
        public async Task<ActionResult<ApiResponse<List<ProcuringEntityInvitationDto>>>> GetInvitations(int id)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue) return Unauthorized(ApiResponse<List<ProcuringEntityInvitationDto>>.Fail("Vui lòng đăng nhập.", statusCode: 401));

            var result = await _memberService.GetInvitationsAsync(id, userId.Value, User.IsInRole("Admin"));
            if (!result.Success) return StatusCode(result.StatusCode, result);
            return Ok(result);
        }

        /// <summary>
        /// Gửi lời mời nhân sự tham gia Bên mời thầu (Gửi email kèm token xác thực)
        /// </summary>
        [HttpPost("{id:int}/invitations")]
        [Authorize(Roles = "Admin,Procurement")]
        public async Task<ActionResult<ApiResponse<ProcuringEntityInvitationDto>>> SendInvitation(
            int id, [FromBody] SendInvitationRequest request)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue) return Unauthorized(ApiResponse<ProcuringEntityInvitationDto>.Fail("Vui lòng đăng nhập.", statusCode: 401));

            var result = await _memberService.SendInvitationAsync(id, request, userId.Value, User.IsInRole("Admin"));
            if (!result.Success) return StatusCode(result.StatusCode, result);
            return Ok(result);
        }

        /// <summary>
        /// Thu hồi lời mời tham gia Bên mời thầu
        /// </summary>
        [HttpDelete("{id:int}/invitations/{invitationId:int}")]
        [Authorize(Roles = "Admin,Procurement")]
        public async Task<ActionResult<ApiResponse<bool>>> RevokeInvitation(int id, int invitationId)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue) return Unauthorized(ApiResponse<bool>.Fail("Vui lòng đăng nhập.", statusCode: 401));

            var result = await _memberService.RevokeInvitationAsync(id, invitationId, userId.Value, User.IsInRole("Admin"));
            if (!result.Success) return StatusCode(result.StatusCode, result);
            return Ok(result);
        }

        /// <summary>
        /// Cập nhật vai trò / quyền hạn của thành viên trong đơn vị
        /// </summary>
        [HttpPut("{id:int}/members/{memberId:int}/role")]
        [HttpPut("{id:int}/members/{memberId:int}")]
        [Authorize(Roles = "Admin,Procurement")]
        public async Task<ActionResult<ApiResponse<ProcuringEntityMemberDto>>> UpdateMemberRole(
            int id, int memberId, [FromBody] UpdateMemberRoleRequest request)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue) return Unauthorized(ApiResponse<ProcuringEntityMemberDto>.Fail("Vui lòng đăng nhập.", statusCode: 401));

            var result = await _memberService.UpdateMemberRoleAsync(id, memberId, request, userId.Value, User.IsInRole("Admin"));
            if (!result.Success) return StatusCode(result.StatusCode, result);
            return Ok(result);
        }

        /// <summary>
        /// Thu hồi tư cách thành viên của đơn vị (khi nghỉ việc / chuyển công tác)
        /// </summary>
        [HttpDelete("{id:int}/members/{memberId:int}")]
        [Authorize(Roles = "Admin,Procurement")]
        public async Task<ActionResult<ApiResponse<bool>>> RevokeMember(int id, int memberId)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue) return Unauthorized(ApiResponse<bool>.Fail("Vui lòng đăng nhập.", statusCode: 401));

            var result = await _memberService.RevokeMemberAsync(id, memberId, userId.Value, User.IsInRole("Admin"));
            if (!result.Success) return StatusCode(result.StatusCode, result);
            return Ok(result);
        }

        /// <summary>
        /// Xem thông tin chi tiết lời mời tham gia qua Token (Public Endpoint)
        /// </summary>
        [HttpGet("invitations/{token}")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<InvitationDetailDto>>> GetInvitationDetails(string token)
        {
            var result = await _memberService.GetInvitationByTokenAsync(token);
            if (!result.Success) return StatusCode(result.StatusCode, result);
            return Ok(result);
        }

        /// <summary>
        /// Xác nhận chấp nhận lời mời tham gia Bên mời thầu (Người mới tự đặt mật khẩu hoặc tài khoản hiện có liên kết)
        /// </summary>
        [HttpPost("invitations/accept")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<ProcuringEntityMemberDto>>> AcceptInvitation(
            [FromBody] AcceptInvitationRequest request)
        {
            var currentUserId = GetCurrentUserId();
            var result = await _memberService.AcceptInvitationAsync(request, currentUserId);
            if (!result.Success) return StatusCode(result.StatusCode, result);
            return Ok(result);
        }

        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out int userId) ? userId : null;
        }
    }
}
