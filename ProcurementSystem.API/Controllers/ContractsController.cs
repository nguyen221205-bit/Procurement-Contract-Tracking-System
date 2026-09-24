using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProcurementSystem.Core.DTOs;
using ProcurementSystem.Core.DTOs.Contract;
using ProcurementSystem.Core.Interfaces;

namespace ProcurementSystem.API.Controllers
{
    [ApiController]
    [Authorize]
    [Produces("application/json")]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status401Unauthorized)]
    public class ContractsController : ControllerBase
    {
        private readonly IContractService _contractService;

        public ContractsController(IContractService contractService)
        {
            _contractService = contractService;
        }

        /// <summary>
        /// Danh sách hợp đồng phân trang (Admin/Procurement xem tất cả; Contractor xem của mình)
        /// </summary>
        /// <param name="filter">Bộ lọc theo số hợp đồng, trạng thái, thời hạn và phân trang.</param>
        /// <remarks>Nhân sự nội bộ xem toàn bộ hợp đồng; nhà thầu chỉ xem hợp đồng thuộc hồ sơ của mình.</remarks>
        [HttpGet("api/contracts")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<PaginatedList<ContractSummaryDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<PaginatedList<ContractSummaryDto>>), StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<ApiResponse<PaginatedList<ContractSummaryDto>>>> GetContracts(
            [FromQuery] ContractFilterParams filter)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized(ApiResponse<PaginatedList<ContractSummaryDto>>.Fail("Không xác định được danh tính người dùng."));
            }

            var isInternalStaff = User.IsInRole("Admin") || User.IsInRole("Procurement");
            var result = await _contractService.GetContractsAsync(filter, userId.Value, isInternalStaff);

            return Ok(result);
        }

        /// <summary>
        /// Tra cứu lịch sử hợp đồng theo nhà thầu (chống IDOR)
        /// </summary>
        /// <param name="contractorId">Mã nhà thầu cần tra cứu lịch sử hợp đồng.</param>
        /// <remarks>Contractor chỉ được tra cứu chính mình; Admin/Procurement được tra cứu tất cả.</remarks>
        [HttpGet("api/contracts/contractor/{contractorId:int}")]
        [Authorize(Roles = "Admin,Procurement,Contractor")]
        [ProducesResponseType(typeof(ApiResponse<List<ContractSummaryDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<List<ContractSummaryDto>>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<List<ContractSummaryDto>>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<List<ContractSummaryDto>>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiResponse<List<ContractSummaryDto>>>> GetContractsByContractor(int contractorId)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized(ApiResponse<List<ContractSummaryDto>>.Fail("Không xác định được danh tính người dùng."));
            }

            var isInternalStaff = User.IsInRole("Admin") || User.IsInRole("Procurement");
            var result = await _contractService.GetContractsByContractorIdAsync(contractorId, userId.Value, isInternalStaff);

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
        /// Xem chi tiết hợp đồng kèm mốc thanh toán
        /// </summary>
        /// <param name="id">Mã hợp đồng cần xem chi tiết.</param>
        /// <remarks>Response bao gồm các mốc thanh toán và tổng tiền đã giải ngân từ các mốc đã nghiệm thu.</remarks>
        [HttpGet("api/contracts/{id:int}")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<ContractDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<ContractDto>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<ContractDto>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<ContractDto>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiResponse<ContractDto>>> GetContractById(int id)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized(ApiResponse<ContractDto>.Fail("Không xác định được danh tính người dùng."));
            }

            var isInternalStaff = User.IsInRole("Admin") || User.IsInRole("Procurement");
            var result = await _contractService.GetContractByIdAsync(id, userId.Value, isInternalStaff);

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
        /// Lấy thông tin nhà thầu trúng thầu để pre-fill dữ liệu sang hợp đồng
        /// Gọi trước khi POST /api/contracts để lấy ContractorId, giá đề xuất và số HĐ tự động
        /// Quyền hạn: Admin, Procurement
        /// </summary>
        /// <param name="packageId">Mã gói thầu đã có hồ sơ trúng thầu.</param>
        [HttpGet("api/contracts/awarded-bid/{packageId:int}")]
        [Authorize(Roles = "Admin,Procurement")]
        [ProducesResponseType(typeof(ApiResponse<AwardedBidInfoDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<AwardedBidInfoDto>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<AwardedBidInfoDto>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<AwardedBidInfoDto>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<AwardedBidInfoDto>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiResponse<AwardedBidInfoDto>>> GetAwardedBidForContract(int packageId)
        {
            var result = await _contractService.GetAwardedBidForContractAsync(packageId);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        /// <summary>
        /// Tạo hợp đồng kinh tế từ kết quả trúng thầu
        /// </summary>
        /// <param name="request">Thông tin gói thầu, nhà thầu trúng thầu, giá trị và thời hạn hợp đồng.</param>
        /// <remarks>Giá trị hợp đồng không được vượt ngân sách gói thầu và mỗi gói thầu chỉ có một hợp đồng chính thức.</remarks>
        [HttpPost("api/contracts")]
        [Authorize(Roles = "Admin,Procurement")]
        [ProducesResponseType(typeof(ApiResponse<ContractDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<ContractDto>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<ContractDto>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<ContractDto>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<ContractDto>), StatusCodes.Status409Conflict)]
        public async Task<ActionResult<ApiResponse<ContractDto>>> CreateContract(
            [FromBody] CreateContractRequest request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return BadRequest(ApiResponse<ContractDto>.Fail(errors));
            }

            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized(ApiResponse<ContractDto>.Fail("Không xác định được danh tính người dùng."));
            }

            var result = await _contractService.CreateContractFromAwardedBidAsync(request, userId.Value);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return StatusCode(StatusCodes.Status201Created, result);
        }

        /// <summary>
        /// Cập nhật điều khoản, giá trị và thời hạn hợp đồng
        /// </summary>
        /// <param name="id">Mã hợp đồng cần cập nhật.</param>
        /// <param name="request">Các trường hợp đồng cần thay đổi.</param>
        /// <remarks>Chỉ cho phép cập nhật hợp đồng Draft; nếu giảm giá trị hợp đồng thì tổng milestone hiện tại không được vượt giá trị mới.</remarks>
        [HttpPut("api/contracts/{id:int}")]
        [Authorize(Roles = "Admin,Procurement")]
        [ProducesResponseType(typeof(ApiResponse<ContractDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<ContractDto>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<ContractDto>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<ContractDto>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<ContractDto>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiResponse<ContractDto>>> UpdateContract(
            int id,
            [FromBody] UpdateContractRequest request)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized(ApiResponse<ContractDto>.Fail("Không xác định được danh tính người dùng."));
            }

            var result = await _contractService.UpdateContractAsync(id, request, userId.Value);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        /// <summary>
        /// Đổi trạng thái hợp đồng (Draft → Active → Completed / Terminated)
        /// </summary>
        /// <param name="id">Mã hợp đồng cần đổi trạng thái.</param>
        /// <param name="request">Trạng thái mới hợp lệ theo luồng hợp đồng.</param>
        [HttpPut("api/contracts/{id:int}/status")]
        [Authorize(Roles = "Admin,Procurement")]
        [ProducesResponseType(typeof(ApiResponse<ContractDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<ContractDto>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<ContractDto>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<ContractDto>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<ContractDto>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiResponse<ContractDto>>> ChangeStatus(
            int id,
            [FromBody] ChangeContractStatusRequest request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return BadRequest(ApiResponse<ContractDto>.Fail(errors));
            }

            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized(ApiResponse<ContractDto>.Fail("Không xác định được danh tính người dùng."));
            }

            var result = await _contractService.ChangeStatusAsync(id, request, userId.Value);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        /// <summary>
        /// Upload file PDF hợp đồng có chữ ký scan
        /// </summary>
        /// <param name="id">Mã hợp đồng cần gắn file scan.</param>
        /// <param name="file">File PDF hoặc bản scan hợp đồng đã ký.</param>
        [HttpPost("api/contracts/{id:int}/scanned-file")]
        [Authorize(Roles = "Admin,Procurement")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiResponse<string>>> UploadScannedFile(
            int id,
            IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(ApiResponse<string>.Fail("File tải lên không hợp lệ."));
            }

            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized(ApiResponse<string>.Fail("Không xác định được danh tính người dùng."));
            }

            var result = await _contractService.UploadScannedContractAsync(id, file, userId.Value);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        /// <summary>
        /// Thêm mốc thanh toán nghiệm thu vào hợp đồng
        /// </summary>
        /// <param name="id">Mã hợp đồng cần thêm mốc thanh toán.</param>
        /// <param name="request">Thông tin tiêu đề, hạn nghiệm thu và số tiền của mốc.</param>
        /// <remarks>Tổng tiền của tất cả mốc thanh toán không được vượt quá giá trị hợp đồng.</remarks>
        [HttpPost("api/contracts/{id:int}/milestones")]
        [Authorize(Roles = "Admin,Procurement")]
        [ProducesResponseType(typeof(ApiResponse<ContractMilestoneDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<ContractMilestoneDto>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<ContractMilestoneDto>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<ContractMilestoneDto>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<ContractMilestoneDto>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiResponse<ContractMilestoneDto>>> AddMilestone(
            int id,
            [FromBody] CreateMilestoneRequest request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return BadRequest(ApiResponse<ContractMilestoneDto>.Fail(errors));
            }

            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized(ApiResponse<ContractMilestoneDto>.Fail("Không xác định được danh tính người dùng."));
            }

            var result = await _contractService.AddMilestoneAsync(id, request, userId.Value);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return StatusCode(StatusCodes.Status201Created, result);
        }

        /// <summary>
        /// Cập nhật mốc thanh toán nghiệm thu của hợp đồng
        /// </summary>
        /// <param name="id">Mã hợp đồng chứa mốc thanh toán.</param>
        /// <param name="milestoneId">Mã mốc thanh toán cần cập nhật.</param>
        /// <param name="request">Thông tin mới của mốc thanh toán.</param>
        /// <remarks>Chỉ cập nhật milestone Pending; tổng tiền các milestone sau cập nhật không được vượt quá giá trị hợp đồng.</remarks>
        [HttpPut("api/contracts/{id:int}/milestones/{milestoneId:int}")]
        [Authorize(Roles = "Admin,Procurement")]
        [ProducesResponseType(typeof(ApiResponse<ContractMilestoneDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<ContractMilestoneDto>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<ContractMilestoneDto>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<ContractMilestoneDto>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<ContractMilestoneDto>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiResponse<ContractMilestoneDto>>> UpdateMilestone(
            int id,
            int milestoneId,
            [FromBody] CreateMilestoneRequest request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return BadRequest(ApiResponse<ContractMilestoneDto>.Fail(errors));
            }

            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized(ApiResponse<ContractMilestoneDto>.Fail("Không xác định được danh tính người dùng."));
            }

            var result = await _contractService.UpdateMilestoneAsync(id, milestoneId, request, userId.Value);

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

        /// <summary>
        /// Xóa mốc thanh toán khỏi hợp đồng
        /// </summary>
        /// <param name="id">Mã hợp đồng chứa mốc thanh toán.</param>
        /// <param name="milestoneId">Mã mốc thanh toán cần xóa.</param>
        [HttpDelete("api/contracts/{id:int}/milestones/{milestoneId:int}")]
        [Authorize(Roles = "Admin,Procurement")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiResponse<bool>>> DeleteMilestone(int id, int milestoneId)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized(ApiResponse<bool>.Fail("Không xác định được danh tính người dùng."));
            }

            var result = await _contractService.DeleteMilestoneAsync(id, milestoneId, userId.Value);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        /// <summary>
        /// Nhà thầu gửi báo cáo tiến độ tuần cho hợp đồng (kiểm soát IDOR)
        /// </summary>
        /// <param name="id">Mã hợp đồng cần gửi báo cáo tiến độ.</param>
        /// <param name="request">Thông tin tuần, phần trăm hoàn thành và ghi chú tiến độ.</param>
        [HttpPost("api/contracts/{id:int}/progress")]
        [Authorize(Roles = "Contractor")]
        [ProducesResponseType(typeof(ApiResponse<ProgressUpdateDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<ProgressUpdateDto>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<ProgressUpdateDto>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<ProgressUpdateDto>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiResponse<ProgressUpdateDto>>> AddProgressUpdate(
            int id,
            [FromBody] CreateProgressUpdateRequest request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return BadRequest(ApiResponse<ProgressUpdateDto>.Fail(errors));
            }

            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized(ApiResponse<ProgressUpdateDto>.Fail("Không xác định được danh tính người dùng."));
            }

            var result = await _contractService.AddProgressUpdateAsync(id, request, userId.Value);

            if (!result.Success)
            {
                if (result.Message.Contains("không có quyền"))
                {
                    return StatusCode(StatusCodes.Status403Forbidden, result);
                }
                if (result.Message.Contains("Không tìm thấy"))
                {
                    return NotFound(result);
                }
                return BadRequest(result);
            }

            return StatusCode(StatusCodes.Status201Created, result);
        }

        /// <summary>
        /// Nhà thầu gửi báo cáo tiến độ tuần theo mốc thanh toán (kiểm soát IDOR)
        /// </summary>
        /// <param name="id">Mã mốc thanh toán cần gửi báo cáo tiến độ.</param>
        /// <param name="request">Thông tin tuần, phần trăm hoàn thành và ghi chú tiến độ.</param>
        [HttpPost("api/contracts/milestones/{id:int}/progress")]
        [Authorize(Roles = "Contractor")]
        [ProducesResponseType(typeof(ApiResponse<ProgressUpdateDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<ProgressUpdateDto>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<ProgressUpdateDto>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<ProgressUpdateDto>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiResponse<ProgressUpdateDto>>> AddMilestoneProgressUpdate(
            int id,
            [FromBody] CreateProgressUpdateRequest request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return BadRequest(ApiResponse<ProgressUpdateDto>.Fail(errors));
            }

            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized(ApiResponse<ProgressUpdateDto>.Fail("Không xác định được danh tính người dùng."));
            }

            var result = await _contractService.AddMilestoneProgressUpdateAsync(id, request, userId.Value);

            if (!result.Success)
            {
                if (result.Message.Contains("không có quyền"))
                {
                    return StatusCode(StatusCodes.Status403Forbidden, result);
                }
                if (result.Message.Contains("Không tìm thấy"))
                {
                    return NotFound(result);
                }
                return BadRequest(result);
            }

            return StatusCode(StatusCodes.Status201Created, result);
        }

        /// <summary>
        /// Phê duyệt biên bản nghiệm thu mốc thanh toán
        /// </summary>
        /// <param name="id">Mã mốc thanh toán cần nghiệm thu.</param>
        /// <param name="request">Kết quả phê duyệt hoặc từ chối kèm ghi chú.</param>
        /// <remarks>Khi phê duyệt tất cả milestone của hợp đồng, hợp đồng Active sẽ tự động chuyển sang Completed.</remarks>
        [HttpPut("api/contracts/milestones/{id:int}/acceptance")]
        [HttpPut("api/contracts/milestones/{id:int}/accept")]
        [Authorize(Roles = "Admin,Procurement")]
        [ProducesResponseType(typeof(ApiResponse<MilestoneAcceptanceDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<MilestoneAcceptanceDto>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<MilestoneAcceptanceDto>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<MilestoneAcceptanceDto>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<MilestoneAcceptanceDto>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiResponse<MilestoneAcceptanceDto>>> ApproveMilestoneAcceptance(
            int id,
            [FromBody] ApproveMilestoneAcceptanceRequest request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return BadRequest(ApiResponse<MilestoneAcceptanceDto>.Fail(errors));
            }

            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized(ApiResponse<MilestoneAcceptanceDto>.Fail("Không xác định được danh tính người dùng."));
            }

            var result = await _contractService.ApproveMilestoneAcceptanceAsync(id, request, userId.Value);

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

        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out int userId) ? userId : null;
        }
    }
}
