using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProcurementSystem.Core.DTOs;
using ProcurementSystem.Core.DTOs.BidPackage;
using ProcurementSystem.Core.Interfaces;

namespace ProcurementSystem.API.Controllers
{
    /// <summary>
    /// Phân hệ Quản lý Gói thầu Mời thầu (Bid Package Management)
    /// Quản lý vòng đời gói thầu: Đăng tải mời thầu, tìm kiếm phân trang, chuyển đổi trạng thái Cỗ máy trạng thái (State Machine) và quản lý tài liệu HSMT.
    /// </summary>
    [ApiController]
    [Route("api/bid-packages")]
    [Produces("application/json")]
    public class BidPackagesController : ControllerBase
    {
        private readonly IBidPackageService _bidPackageService;

        public BidPackagesController(IBidPackageService bidPackageService)
        {
            _bidPackageService = bidPackageService;
        }

        /// <summary>
        /// Lấy danh sách gói thầu có phân trang và bộ lọc nâng cao
        /// </summary>
        /// <remarks>
        /// Cho phép tìm kiếm theo mã gói thầu, tên gói thầu, loại gói thầu (Hàng hóa, Xây lắp, Dịch vụ), trạng thái (Open, Closed, Evaluating, Contracted) và khoảng ngân sách.
        /// API này mở công khai (Public) để nhà thầu và công chúng tra cứu thông tin mời thầu.
        /// </remarks>
        /// <param name="filter">Bộ tham số lọc, sắp xếp và phân trang</param>
        /// <response code="200">Truy xuất danh sách gói thầu phân trang thành công.</response>
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
        /// </summary>
        /// <param name="id">Định danh gói thầu (BidPackageId)</param>
        /// <response code="200">Truy xuất chi tiết gói thầu thành công.</response>
        /// <response code="404">Không tìm thấy gói thầu với ID được cung cấp.</response>
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
        /// </summary>
        /// <remarks>
        /// Tự động sinh mã gói thầu theo định dạng PKG-YYYYMMDD-XXXX và khởi tạo trạng thái ban đầu là 'Open'.
        /// Quyền hạn: Bắt buộc **Admin** hoặc **Procurement**.
        /// </remarks>
        /// <param name="request">Thông tin gói thầu (tên, loại, dự toán ngân sách, thời hạn nộp hồ sơ, mô tả)</param>
        /// <response code="200">Khởi tạo gói thầu thành công.</response>
        /// <response code="400">Dữ liệu yêu cầu không hợp lệ hoặc thời hạn nộp thầu không nằm trong tương lai.</response>
        /// <response code="401">Chưa xác thực danh tính.</response>
        /// <response code="403">Từ chối truy cập (không có quyền Admin/Procurement).</response>
        [HttpPost]
        [Authorize(Roles = "Admin,Procurement")]
        [ProducesResponseType(typeof(ApiResponse<BidPackageDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<BidPackageDto>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status403Forbidden)]
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
        /// Cập nhật thông tin gói thầu
        /// </summary>
        /// <remarks>
        /// Chỉ cho phép sửa đổi khi gói thầu đang ở trạng thái 'Open' hoặc 'Closed' (chưa bắt đầu chấm điểm hay ký hợp đồng).
        /// Quyền hạn: Bắt buộc **Admin** hoặc người tạo gói thầu (**Procurement**).
        /// </remarks>
        /// <param name="id">Định danh gói thầu</param>
        /// <param name="request">Thông tin cần cập nhật</param>
        /// <response code="200">Cập nhật gói thầu thành công.</response>
        /// <response code="400">Dữ liệu không hợp lệ hoặc gói thầu đã bị khóa chỉnh sửa.</response>
        /// <response code="401">Chưa đăng nhập.</response>
        /// <response code="403">Từ chối truy cập (không phải chủ sở hữu hoặc Admin).</response>
        /// <response code="404">Không tìm thấy gói thầu.</response>
        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin,Procurement")]
        [ProducesResponseType(typeof(ApiResponse<BidPackageDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<BidPackageDto>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<BidPackageDto>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status403Forbidden)]
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
        /// Chuyển đổi trạng thái gói thầu theo Cỗ máy trạng thái (State Machine)
        /// </summary>
        /// <remarks>
        /// Luồng trạng thái hợp lệ:
        /// - **Open (0)**: Đang mở mời thầu
        /// - **Closed (1)**: Đóng thầu, ngừng nhận hồ sơ
        /// - **Evaluating (2)**: Đang chấm điểm và xếp hạng
        /// - **Contracted (3)**: Đã phê duyệt trúng thầu và chuyển sang phân hệ hợp đồng
        /// </remarks>
        /// <param name="id">Định danh gói thầu</param>
        /// <param name="request">Mã trạng thái mới cần chuyển đổi</param>
        /// <response code="200">Chuyển trạng thái thành công.</response>
        /// <response code="400">Bước chuyển trạng thái không hợp lệ theo quy tắc State Machine.</response>
        /// <response code="401">Chưa đăng nhập.</response>
        /// <response code="403">Từ chối truy cập.</response>
        [HttpPut("{id:int}/status")]
        [Authorize(Roles = "Admin,Procurement")]
        [ProducesResponseType(typeof(ApiResponse<BidPackageDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<BidPackageDto>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status403Forbidden)]
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
        /// Tải lên tài liệu mời thầu HSMT (Hỗ trợ tải lên đa tệp: PDF, DOCX, XLSX, ZIP)
        /// </summary>
        /// <param name="id">Định danh gói thầu</param>
        /// <param name="files">Danh sách tệp tin đính kèm</param>
        /// <response code="200">Tải lên tệp tài liệu thành công.</response>
        /// <response code="400">Danh sách tệp rỗng hoặc định dạng tệp không được hỗ trợ.</response>
        /// <response code="401">Chưa đăng nhập.</response>
        /// <response code="403">Từ chối truy cập.</response>
        [HttpPost("{id:int}/documents")]
        [Authorize(Roles = "Admin,Procurement")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(ApiResponse<List<BidDocumentDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<List<BidDocumentDto>>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status403Forbidden)]
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
        /// Xóa một tài liệu mời thầu HSMT đã đính kèm khỏi gói thầu
        /// </summary>
        /// <param name="id">Định danh gói thầu</param>
        /// <param name="documentId">Định danh tệp tài liệu cần xóa</param>
        /// <response code="200">Xóa tệp tài liệu thành công.</response>
        /// <response code="400">Không thể xóa khi gói thầu đã chuyển trạng thái.</response>
        /// <response code="401">Chưa đăng nhập.</response>
        /// <response code="403">Từ chối truy cập.</response>
        [HttpDelete("{id:int}/documents/{documentId:int}")]
        [Authorize(Roles = "Admin,Procurement")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status403Forbidden)]
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
