using ProcurementSystem.Core.DTOs;
using ProcurementSystem.Core.DTOs.Audit;

namespace ProcurementSystem.Core.Interfaces
{
    public interface IAuditLogService
    {
        /// <summary>
        /// Tra cứu lịch sử kiểm toán phân trang và lọc theo entity, action, user, date
        /// </summary>
        Task<ApiResponse<PaginatedList<AuditLogDto>>> GetAuditLogsAsync(AuditLogFilterParams filter);

        /// <summary>
        /// Xem chi tiết một bản ghi nhật ký kiểm toán
        /// </summary>
        Task<ApiResponse<AuditLogDto>> GetAuditLogByIdAsync(int id);
    }
}
