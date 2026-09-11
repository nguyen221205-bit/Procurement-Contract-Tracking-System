using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProcurementSystem.Core.DTOs;
using ProcurementSystem.Core.DTOs.BidPackage;
using ProcurementSystem.Core.Interfaces;

namespace ProcurementSystem.API.Controllers
{
    [ApiController]
    [Route("api/bid-packages")]
    public class BidPackagesController : ControllerBase
    {
        private readonly IBidPackageService _bidPackageService;

        public BidPackagesController(IBidPackageService bidPackageService)
        {
            _bidPackageService = bidPackageService;
        }

        /// <summary>
        /// Lấy danh sách gói thầu (Phân trang, tìm kiếm mã/tên, lọc theo loại, trạng thái, ngân sách, deadline)
        /// Public: Mọi người đều có thể tra cứu gói thầu đang mở
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<PaginatedList<BidPackageSummaryDto>>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<PaginatedList<BidPackageSummaryDto>>>> GetBidPackages([FromQuery] BidPackageFilterParams filter)
        {
            var result = await _bidPackageService.GetBidPackagesAsync(filter);
            return Ok(result);
        }

        /// <summary>
        /// Xem chi tiết thông tin gói thầu kèm danh sách tài liệu mời thầu (HSMT)
        /// Public: Mọi người đều có thể xem chi tiết gói thầu
        /// </summary>
        [HttpGet("{id:int}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<BidPackageDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<BidPackageDto>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiResponse<BidPackageDto>>> GetBidPackageById(int id)
        {
            var result = await _bidPackageService.GetBidPackageByIdAsync(id);
            if (!result.Success) return NotFound(result);
            return Ok(result);
        }

        /// <summary>
        /// Tạo mới gói thầu mời thầu
        /// Quyền hạn: Admin, Procurement
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin,Procurement")]
        [ProducesResponseType(typeof(ApiResponse<BidPackageDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<BidPackageDto>), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ApiResponse<BidPackageDto>>> CreateBidPackage([FromBody] CreateBidPackageRequest request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<BidPackageDto>.Fail(errors));
            }

            var userId = GetCurrentUserId();
            var result = await _bidPackageService.CreateBidPackageAsync(request, userId);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        /// <summary>
        /// Cập nhật thông tin gói thầu (chỉ sửa được khi gói thầu chưa vào giai đoạn chấm điểm/hợp đồng)
        /// Quyền hạn: Admin hoặc người tạo gói thầu (Procurement)
        /// </summary>
        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin,Procurement")]
        [ProducesResponseType(typeof(ApiResponse<BidPackageDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<BidPackageDto>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<BidPackageDto>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiResponse<BidPackageDto>>> UpdateBidPackage(int id, [FromBody] UpdateBidPackageRequest request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<BidPackageDto>.Fail(errors));
            }

            var userId = GetCurrentUserId();
            var isAdmin = User.IsInRole("Admin");

            var result = await _bidPackageService.UpdateBidPackageAsync(id, request, userId, isAdmin);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        /// <summary>
        /// Chuyển đổi trạng thái gói thầu theo State Machine (Open -> Closed -> Evaluating -> Contracted)
        /// Quyền hạn: Admin hoặc người tạo gói thầu (Procurement)
        /// </summary>
        [HttpPut("{id:int}/status")]
        [Authorize(Roles = "Admin,Procurement")]
        [ProducesResponseType(typeof(ApiResponse<BidPackageDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<BidPackageDto>), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ApiResponse<BidPackageDto>>> ChangeStatus(int id, [FromBody] ChangeBidPackageStatusRequest request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<BidPackageDto>.Fail(errors));
            }

            var userId = GetCurrentUserId();
            var isAdmin = User.IsInRole("Admin");

            var result = await _bidPackageService.ChangeStatusAsync(id, request, userId, isAdmin);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        /// <summary>
        /// Upload tài liệu mời thầu HSMT cho gói thầu (Hỗ trợ upload multi-file: PDF, DOCX, XLSX, ZIP)
        /// Quyền hạn: Admin hoặc người tạo gói thầu (Procurement)
        /// </summary>
        [HttpPost("{id:int}/documents")]
        [Authorize(Roles = "Admin,Procurement")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(ApiResponse<List<BidDocumentDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<List<BidDocumentDto>>), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ApiResponse<List<BidDocumentDto>>>> UploadDocuments(int id, [FromForm] List<IFormFile> files)
        {
            if (files == null || files.Count == 0)
            {
                return BadRequest(ApiResponse<List<BidDocumentDto>>.Fail("Vui lòng chọn ít nhất một file để upload."));
            }

            var userId = GetCurrentUserId();
            var isAdmin = User.IsInRole("Admin");

            var result = await _bidPackageService.UploadDocumentsAsync(id, files, userId, isAdmin);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        /// <summary>
        /// Xóa một tài liệu mời thầu HSMT đã đính kèm
        /// Quyền hạn: Admin hoặc người tạo gói thầu (Procurement)
        /// </summary>
        [HttpDelete("{id:int}/documents/{documentId:int}")]
        [Authorize(Roles = "Admin,Procurement")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ApiResponse<bool>>> DeleteDocument(int id, int documentId)
        {
            var userId = GetCurrentUserId();
            var isAdmin = User.IsInRole("Admin");

            var result = await _bidPackageService.DeleteDocumentAsync(id, documentId, userId, isAdmin);
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
