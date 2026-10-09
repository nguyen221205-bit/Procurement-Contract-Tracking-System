using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProcurementSystem.Core.DTOs;
using ProcurementSystem.Core.DTOs.Audit;
using ProcurementSystem.Core.Interfaces;

namespace ProcurementSystem.API.Controllers
{
    /// <summary>
    /// Phân hệ Nhật ký kiểm toán hệ thống (Audit Trail)
    /// Cho phép ban quản trị truy vết mọi thao tác nhạy cảm trên hệ thống: chấm điểm, chọn thầu, duyệt hợp đồng, thẩm định nhà thầu.
    /// </summary>
    [ApiController]
    [Route("api/audit-logs")]
    [Produces("application/json")]
    [Authorize(Roles = "Admin,Procurement")]
    public class AuditLogsController : ControllerBase
    {
        private readonly IAuditLogService _auditLogService;

        public AuditLogsController(IAuditLogService auditLogService)
        {
            _auditLogService = auditLogService;
        }

        /// <summary>
        /// Tra cứu danh sách nhật ký kiểm toán (phân trang, lọc theo Action, Entity, User, khoảng thời gian)
        /// </summary>
        /// <remarks>Quyền hạn: Admin, Procurement.</remarks>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<PaginatedList<AuditLogDto>>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<PaginatedList<AuditLogDto>>>> GetAuditLogs(
            [FromQuery] AuditLogFilterParams filter)
        {
            var result = await _auditLogService.GetAuditLogsAsync(filter);
            return Ok(result);
        }

        /// <summary>
        /// Xem chi tiết một bản ghi nhật ký kiểm toán
        /// </summary>
        /// <param name="id">Mã định danh bản ghi kiểm toán.</param>
        /// <remarks>Quyền hạn: Admin, Procurement.</remarks>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(ApiResponse<AuditLogDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<AuditLogDto>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiResponse<AuditLogDto>>> GetAuditLogById(int id)
        {
            var result = await _auditLogService.GetAuditLogByIdAsync(id);
            if (!result.Success)
            {
                return NotFound(result);
            }
            return Ok(result);
        }
    }
}
