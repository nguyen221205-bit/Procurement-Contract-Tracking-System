using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProcurementSystem.Core.DTOs;
using ProcurementSystem.Core.DTOs.Contractor;
using ProcurementSystem.Core.Interfaces;

namespace ProcurementSystem.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
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
        /// Quyền hạn: Admin, Procurement
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin,Procurement")]
        [ProducesResponseType(typeof(ApiResponse<PaginatedList<ContractorDto>>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<PaginatedList<ContractorDto>>>> GetContractors(
            [FromQuery] ContractorFilterParams filter)
        {
            var result = await _contractorService.GetContractorsAsync(filter);
            return Ok(result);
        }

        /// <summary>
        /// Xem chi tiết hồ sơ nhà thầu theo ID
        /// Quyền hạn: Admin, Procurement
        /// </summary>
        [HttpGet("{id:int}")]
        [Authorize(Roles = "Admin,Procurement")]
        [ProducesResponseType(typeof(ApiResponse<ContractorDto>), StatusCodes.Status200OK)]
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
        /// Quyền hạn: Contractor
        /// </summary>
        [HttpGet("me")]
        [Authorize(Roles = "Contractor")]
        [ProducesResponseType(typeof(ApiResponse<ContractorDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<ContractorDto>), StatusCodes.Status401Unauthorized)]
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
        /// Quyền hạn: Contractor
        /// </summary>
        [HttpPut("me")]
        [Authorize(Roles = "Contractor")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(ApiResponse<ContractorDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<ContractorDto>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<ContractorDto>), StatusCodes.Status401Unauthorized)]
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
        /// Quyền hạn: Admin, Procurement
        /// </summary>
        [HttpPatch("{id:int}/rating")]
        [Authorize(Roles = "Admin,Procurement")]
        [ProducesResponseType(typeof(ApiResponse<ContractorDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<ContractorDto>), StatusCodes.Status400BadRequest)]
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
