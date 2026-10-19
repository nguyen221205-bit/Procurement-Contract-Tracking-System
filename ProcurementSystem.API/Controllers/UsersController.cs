using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProcurementSystem.Core.DTOs;
using ProcurementSystem.Core.DTOs.User;
using ProcurementSystem.Core.Interfaces;

namespace ProcurementSystem.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        /// <summary>
        /// Lấy danh sách người dùng (Phân trang, tìm kiếm từ khóa, lọc theo vai trò và trạng thái hoạt động)
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin,Procurement")]
        [ProducesResponseType(typeof(ApiResponse<PaginatedList<UserDto>>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<PaginatedList<UserDto>>>> GetUsers([FromQuery] UserFilterParams filter)
        {
            var result = await _userService.GetUsersAsync(filter);
            return Ok(result);
        }

        /// <summary>
        /// Xem chi tiết thông tin tài khoản người dùng theo ID
        /// </summary>
        [HttpGet("{id:int}")]
        [Authorize(Roles = "Admin,Procurement")]
        [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiResponse<UserDto>>> GetUserById(int id)
        {
            var result = await _userService.GetUserByIdAsync(id);
            if (!result.Success) return NotFound(result);
            return Ok(result);
        }

        /// <summary>
        /// Cập nhật thông tin cơ bản của người dùng (Họ tên, Số điện thoại)
        /// </summary>
        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ApiResponse<UserDto>>> UpdateUser(int id, [FromBody] UpdateUserRequest request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<UserDto>.Fail(errors));
            }

            var result = await _userService.UpdateUserAsync(id, request);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        /// <summary>
        /// Khóa hoặc Kích hoạt tài khoản người dùng (Không cho phép Admin tự khóa tài khoản của mình)
        /// </summary>
        [HttpPatch("{id:int}/status")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ApiResponse<bool>>> ToggleUserStatus(int id)
        {
            var currentUserIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            int.TryParse(currentUserIdClaim, out int currentUserId);

            var result = await _userService.ToggleUserStatusAsync(id, currentUserId);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        /// <summary>
        /// Phân quyền / Gán danh sách vai trò cho người dùng (Ví dụ: Procurement, Evaluator)
        /// </summary>
        [HttpPost("{id:int}/roles")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ApiResponse<UserDto>>> AssignRoles(int id, [FromBody] AssignRolesRequest request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<UserDto>.Fail(errors));
            }

            var currentUserIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            int.TryParse(currentUserIdClaim, out int currentUserId);

            var result = await _userService.AssignRolesAsync(id, request, currentUserId);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        /// <summary>
        /// Đặt lại mật khẩu tài khoản người dùng bởi Quản trị viên (Admin Reset Password)
        /// </summary>
        /// <param name="id">ID của người dùng cần đặt lại mật khẩu</param>
        /// <param name="request">Mật khẩu mới tùy chọn (nếu để trống hệ thống tự sinh ngẫu nhiên 10 ký tự)</param>
        /// <response code="200">Đặt lại mật khẩu thành công. Trả về mật khẩu mới để Admin có thể copy trực tiếp.</response>
        /// <response code="400">Dữ liệu yêu cầu không hợp lệ hoặc lỗi nghiệp vụ.</response>
        /// <response code="404">Không tìm thấy người dùng.</response>
        [HttpPost("{id:int}/reset-password")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ApiResponse<ResetPasswordResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<ResetPasswordResponseDto>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<ResetPasswordResponseDto>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiResponse<ResetPasswordResponseDto>>> ResetPassword(
            int id, [FromBody] ResetPasswordRequest? request)
        {
            if (request != null && !ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<ResetPasswordResponseDto>.Fail(errors));
            }

            var currentUserIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            int.TryParse(currentUserIdClaim, out int currentUserId);

            var result = await _userService.ResetPasswordAsync(id, request, currentUserId);
            if (!result.Success)
            {
                if (result.Message.Contains("Không tìm thấy"))
                {
                    return NotFound(result);
                }
                return BadRequest(result);
            }
            return Ok(result);
        }
    }
}
