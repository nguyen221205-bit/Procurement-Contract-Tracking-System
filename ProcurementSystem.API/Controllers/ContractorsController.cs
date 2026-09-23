using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProcurementSystem.Core.DTOs;
using ProcurementSystem.Core.DTOs.Contractor;
using ProcurementSystem.Core.Interfaces;

namespace ProcurementSystem.API.Controllers
{
    /// <summary>
    /// Phân hệ Quản lý Nhà thầu (Contractor Management)
    /// Cung cấp các API tra cứu hồ sơ năng lực nhà thầu, quản lý giấy phép kinh doanh và đánh giá uy tín (Rating).
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    [Authorize]
    public class ContractorsController : ControllerBase
    {
        private readonly IContractorService _contractorService;

        public ContractorsController(IContractorService contractorService)
        {
            _contractorService = contractorService;
        }

        /// <summary>
        /// Lấy danh sách nhà thầu (phân trang, tìm kiếm MST/Tên công ty, lọc theo Rating)
        /// </summary>
        /// <param name="filter">Bộ lọc danh sách nhà thầu (từ khóa, mã số thuế, mức đánh giá rating tối thiểu, số trang và kích thước trang).</param>
        /// <remarks>Quyền hạn: Admin, Procurement.</remarks>
        /// <response code="200">Truy xuất thành công danh sách nhà thầu phân trang.</response>
        /// <response code="401">Chưa xác thực danh tính (Thiếu hoặc Token JWT không hợp lệ).</response>
        /// <response code="403">Từ chối truy cập (Yêu cầu vai trò Admin hoặc Procurement).</response>
        [HttpGet]
        [Authorize(Roles = "Admin,Procurement")]
        [ProducesResponseType(typeof(ApiResponse<PaginatedList<ContractorDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<ApiResponse<PaginatedList<ContractorDto>>>> GetContractors(
            [FromQuery] ContractorFilterParams filter)
        {
            var result = await _contractorService.GetContractorsAsync(filter);
            return Ok(result);
        }

        /// <summary>
        /// Xem chi tiết hồ sơ nhà thầu theo ID
        /// </summary>
        /// <param name="id">Mã định danh duy nhất của nhà thầu.</param>
        /// <remarks>Quyền hạn: Admin, Procurement.</remarks>
        /// <response code="200">Truy xuất thành công thông tin chi tiết nhà thầu.</response>
        /// <response code="401">Chưa xác thực danh tính.</response>
        /// <response code="403">Từ chối truy cập (Yêu cầu vai trò Admin hoặc Procurement).</response>
        /// <response code="404">Không tìm thấy nhà thầu với mã tương ứng.</response>
        [HttpGet("{id:int}")]
        [Authorize(Roles = "Admin,Procurement")]
        [ProducesResponseType(typeof(ApiResponse<ContractorDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<ContractorDto>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiResponse<ContractorDto>>> GetContractorById(int id)
        {
            var result = await _contractorService.GetContractorByIdAsync(id);
            if (!result.Success)
            {
                return NotFound(result);
            }
            return Ok(result);
        }

        /// <summary>
        /// Nhà thầu tự xem thông tin hồ sơ của chính mình từ JWT Token
        /// </summary>
        /// <remarks>Quyền hạn: Contractor.</remarks>
        /// <response code="200">Truy xuất thành công hồ sơ của chính nhà thầu đăng nhập.</response>
        /// <response code="401">Chưa xác thực hoặc không xác định được danh tính từ Token.</response>
        /// <response code="403">Từ chối truy cập (Chỉ tài khoản vai trò Contractor).</response>
        /// <response code="404">Không tìm thấy hồ sơ nhà thầu liên kết với tài khoản này.</response>
        [HttpGet("me")]
        [Authorize(Roles = "Contractor")]
        [ProducesResponseType(typeof(ApiResponse<ContractorDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<ContractorDto>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<ContractorDto>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiResponse<ContractorDto>>> GetMyProfile()
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized(ApiResponse<ContractorDto>.Fail("Không xác định được danh tính người dùng từ Token."));
            }

            var result = await _contractorService.GetCurrentContractorProfileAsync(userId.Value);
            if (!result.Success)
            {
                return NotFound(result);
            }
            return Ok(result);
        }

        /// <summary>
        /// Nhà thầu tự cập nhật thông tin công ty và tải lên file GPKD mới thay thế (nếu có)
        /// </summary>
        /// <param name="request">Thông tin cập nhật tên công ty, địa chỉ, mã số thuế và tệp giấy phép kinh doanh mới.</param>
        /// <remarks>Chỉ tài khoản Contractor được phép tự cập nhật hồ sơ của chính mình; dữ liệu gửi theo multipart/form-data.</remarks>
        /// <response code="200">Cập nhật hồ sơ nhà thầu thành công.</response>
        /// <response code="400">Dữ liệu gửi lên không hợp lệ.</response>
        /// <response code="401">Chưa xác thực hoặc không xác định được danh tính từ Token.</response>
        /// <response code="403">Từ chối truy cập (Chỉ vai trò Contractor).</response>
        [HttpPut("me")]
        [Authorize(Roles = "Contractor")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(ApiResponse<ContractorDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<ContractorDto>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<ContractorDto>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<ApiResponse<ContractorDto>>> UpdateMyProfile(
            [FromForm] UpdateContractorProfileRequest request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return BadRequest(ApiResponse<ContractorDto>.Fail(errors));
            }

            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized(ApiResponse<ContractorDto>.Fail("Không xác định được danh tính người dùng từ Token."));
            }

            var result = await _contractorService.UpdateCurrentContractorProfileAsync(userId.Value, request);
            if (!result.Success)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }

        /// <summary>
        /// Cập nhật điểm đánh giá uy tín năng lực nhà thầu (0.0 đến 5.0 sao)
        /// </summary>
        /// <param name="id">Mã định danh duy nhất của nhà thầu.</param>
        /// <param name="request">Điểm đánh giá uy tín năng lực mới (từ 0.0 đến 5.0).</param>
        /// <remarks>Quyền hạn: Admin, Procurement.</remarks>
        /// <response code="200">Cập nhật điểm đánh giá uy tín nhà thầu thành công.</response>
        /// <response code="400">Dữ liệu đánh giá không hợp lệ.</response>
        /// <response code="401">Chưa xác thực danh tính.</response>
        /// <response code="403">Từ chối truy cập (Yêu cầu vai trò Admin hoặc Procurement).</response>
        /// <response code="404">Không tìm thấy nhà thầu tương ứng.</response>
        [HttpPatch("{id:int}/rating")]
        [Authorize(Roles = "Admin,Procurement")]
        [ProducesResponseType(typeof(ApiResponse<ContractorDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<ContractorDto>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<ApiResponse<ContractorDto>>> UpdateRating(
            int id,
            [FromBody] UpdateContractorRatingRequest request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return BadRequest(ApiResponse<ContractorDto>.Fail(errors));
            }

            var result = await _contractorService.UpdateContractorRatingAsync(id, request);
            if (!result.Success)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }

        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out int userId) ? userId : null;
        }
    }
}
