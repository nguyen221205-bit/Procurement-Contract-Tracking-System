using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProcurementSystem.Core.DTOs;
using ProcurementSystem.Core.DTOs.Report;
using ProcurementSystem.Core.Interfaces;

namespace ProcurementSystem.API.Controllers
{
    [ApiController]
    [Route("api/reports")]
    public class ReportsController : ControllerBase
    {
        private readonly IReportService _reportService;

        public ReportsController(IReportService reportService)
        {
            _reportService = reportService;
        }

        /// <summary>
        /// Báo cáo thống kê tổng hợp cấp Quản lý (Management Procurement & Contract Dashboard)
        /// Thống kê tình trạng gói thầu, hiệu quả tài chính đấu thầu, giá trị hợp đồng và tiến độ giải ngân mốc thanh toán
        /// Quyền hạn: Admin, Procurement
        /// </summary>
        [HttpGet("dashboard")]
        [Authorize(Roles = "Admin,Procurement")]
        [ProducesResponseType(typeof(ApiResponse<ProcurementDashboardDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<ProcurementDashboardDto>>> GetDashboard()
        {
            var result = await _reportService.GetDashboardMetricsAsync();
            return Ok(result);
        }
    }
}
