using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProcurementSystem.Core.DTOs;
using ProcurementSystem.Core.DTOs.Report;
using ProcurementSystem.Core.Interfaces;

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
    }
}
