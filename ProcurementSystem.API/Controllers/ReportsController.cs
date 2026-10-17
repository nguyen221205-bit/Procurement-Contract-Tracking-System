using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProcurementSystem.Core.DTOs;
using ProcurementSystem.Core.DTOs.Report;
using ProcurementSystem.Core.Interfaces;
using ProcurementSystem.Core.Enums;

namespace ProcurementSystem.API.Controllers
{
    /// <summary>
    /// Phân hệ Báo cáo &amp; Thống kê Quản trị (Executive Management &amp; Procurement Reports)
    /// Cung cấp các chỉ số đo lường hiệu quả mua sắm, tài chính, tiết kiệm qua đấu thầu và tiến độ giải ngân hợp đồng.
    /// </summary>
    [ApiController]
    [Route("api/reports")]
    [Produces("application/json")]
    public class ReportsController : ControllerBase
    {
        private readonly IReportService _reportService;

        public ReportsController(IReportService reportService)
        {
            _reportService = reportService;
        }

        /// <summary>
        /// Báo cáo thống kê tổng hợp cấp Quản lý (Management Procurement &amp; Contract Dashboard)
        /// </summary>
        /// <remarks>
        /// Endpoint cung cấp bức tranh số liệu thời gian thực (Real-time Dashboard) cho Ban Giám đốc và Chuyên viên Mua sắm:
        /// - **Chỉ số Gói thầu:** Tổng số gói thầu, phân loại theo trạng thái (Open, Closed, Evaluating, Contracted).
        /// - **Chỉ số Hồ sơ &amp; Nhà thầu:** Tổng hồ sơ nộp, số lượng đã chấm điểm, số lượng trúng thầu và tổng số nhà thầu đăng ký.
        /// - **Chỉ số Tài chính &amp; Tiết kiệm:** Tổng ngân sách dự toán, tổng giá trị hợp đồng đã ký kết, số tiền tiết kiệm và tỷ lệ tiết kiệm chi phí.
        /// - **Chỉ số Giải ngân Mốc thanh toán:** Tổng số mốc nghiệm thu, số mốc hoàn thành, giá trị đã giải ngân và tỷ lệ giải ngân dòng tiền.
        /// - **Hợp đồng mới nhất:** Danh sách tóm tắt các hợp đồng phát sinh gần nhất.
        /// 
        /// **Yêu cầu quyền hạn:** Bắt buộc đăng nhập với vai trò **Admin** hoặc **Procurement**.
        /// </remarks>
        /// <response code="200">Truy xuất thành công dữ liệu Dashboard với cấu trúc ProcurementDashboardDto chuẩn.</response>
        /// <response code="401">Chưa xác thực danh tính (Thiếu hoặc Token JWT không hợp lệ/hết hạn).</response>
        /// <response code="403">Từ chối truy cập (Tài khoản không thuộc vai trò Admin hoặc Procurement).</response>
        [HttpGet("dashboard")]
        [Authorize(Roles = "Admin,Procurement")]
        [ProducesResponseType(typeof(ApiResponse<ProcurementDashboardDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<ApiResponse<ProcurementDashboardDto>>> GetDashboard()
        {
            var result = await _reportService.GetDashboardMetricsAsync();
            return Ok(result);
        }

        /// <summary>
        /// Xuất danh sách toàn bộ nhà thầu ra file CSV (Excel tương thích tiếng Việt có dấu với UTF-8 BOM)
        /// </summary>
        /// <remarks>Quyền hạn: Admin, Procurement.</remarks>
        [HttpGet("contractors/export")]
        [Authorize(Roles = "Admin,Procurement")]
        [Produces("text/csv")]
        public async Task<IActionResult> ExportContractorsCsv()
        {
            var csvBytes = await _reportService.ExportContractorsCsvAsync();
            var fileName = $"Danh_sach_nha_thau_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";
            return File(csvBytes, "text/csv; charset=utf-8", fileName);
        }

        /// <summary>
        /// Xuất danh sách toàn bộ hợp đồng ra file CSV (Excel tương thích tiếng Việt có dấu với UTF-8 BOM)
        /// </summary>
        /// <remarks>Quyền hạn: Admin, Procurement.</remarks>
        [HttpGet("contracts/export")]
        [Authorize(Roles = "Admin,Procurement")]
        [Produces("text/csv")]
        public async Task<IActionResult> ExportContractsCsv()
        {
            var csvBytes = await _reportService.ExportContractsCsvAsync();
            var fileName = $"Danh_sach_hop_dong_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";
            return File(csvBytes, "text/csv; charset=utf-8", fileName);
        }

        /// <summary>
        /// Xuất danh sách toàn bộ nhà thầu ra file Excel định dạng .xlsx
        /// </summary>
        /// <remarks>Quyền hạn: Admin, Procurement.</remarks>
        [HttpGet("contractors/export-excel")]
        [Authorize(Roles = "Admin,Procurement")]
        [Produces("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
        public async Task<IActionResult> ExportContractorsExcel()
        {
            var excelBytes = await _reportService.ExportContractorsExcelAsync();
            var fileName = $"Danh_sach_nha_thau_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xlsx";
            return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        /// <summary>
        /// Xuất danh sách toàn bộ hợp đồng ra file Excel định dạng .xlsx
        /// </summary>
        /// <remarks>Quyền hạn: Admin, Procurement.</remarks>
        [HttpGet("contracts/export-excel")]
        [Authorize(Roles = "Admin,Procurement")]
        [Produces("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
        public async Task<IActionResult> ExportContractsExcel()
        {
            var excelBytes = await _reportService.ExportContractsExcelAsync();
            var fileName = $"Danh_sach_hop_dong_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xlsx";
            return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        /// <summary>
        /// Báo cáo thống kê mua sắm theo năm và phân loại gói thầu (R3-10)
        /// </summary>
        /// <remarks>Quyền hạn: Admin, Procurement.</remarks>
        [HttpGet("procurement-summary")]
        [Authorize(Roles = "Admin,Procurement")]
        [ProducesResponseType(typeof(ApiResponse<ProcurementSummaryReportDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<ProcurementSummaryReportDto>>> GetProcurementSummary(
            [FromQuery] int? year,
            [FromQuery] BidPackageType? type)
        {
            var result = await _reportService.GetProcurementSummaryReportAsync(year, type);
            return Ok(result);
        }

        /// <summary>
        /// Báo cáo tỷ lệ trúng/trượt thầu và năng lực thực tế của các nhà thầu (R3-10)
        /// </summary>
        /// <remarks>Quyền hạn: Admin, Procurement.</remarks>
        [HttpGet("contractor-performance")]
        [Authorize(Roles = "Admin,Procurement")]
        [ProducesResponseType(typeof(ApiResponse<ContractorPerformanceReportDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<ContractorPerformanceReportDto>>> GetContractorPerformance()
        {
            var result = await _reportService.GetContractorPerformanceReportAsync();
            return Ok(result);
        }

        /// <summary>
        /// Báo cáo tỷ lệ mốc hoàn thành đúng tiến độ và danh sách hợp đồng sắp đến hạn (R3-10)
        /// </summary>
        /// <remarks>Quyền hạn: Admin, Procurement.</remarks>
        [HttpGet("contracts-progress")]
        [Authorize(Roles = "Admin,Procurement")]
        [ProducesResponseType(typeof(ApiResponse<ContractProgressReportDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<ContractProgressReportDto>>> GetContractsProgress(
            [FromQuery] int daysAhead = 30)
        {
            var result = await _reportService.GetContractProgressReportAsync(daysAhead);
            return Ok(result);
        }
    }
}
