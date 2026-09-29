using ProcurementSystem.Core.DTOs;
using ProcurementSystem.Core.DTOs.Report;

namespace ProcurementSystem.Core.Interfaces
{
    public interface IReportService
    {
        /// <summary>
        /// Lấy toàn bộ chỉ số thống kê tổng hợp cấp quản trị (Dashboard Metrics):
        /// Tiến độ gói thầu, hiệu quả tài chính đấu thầu, tổng giá trị hợp đồng và tiến độ giải ngân mốc thanh toán
        /// </summary>
        Task<ApiResponse<ProcurementDashboardDto>> GetDashboardMetricsAsync();
    }
}
