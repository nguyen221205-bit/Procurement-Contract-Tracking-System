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

        /// <summary>
        /// Xuất danh sách toàn bộ nhà thầu ra tệp CSV (UTF-8 BOM hỗ trợ mở trực tiếp bằng Excel tiếng Việt)
        /// </summary>
        Task<byte[]> ExportContractorsCsvAsync();

        /// <summary>
        /// Xuất danh sách toàn bộ hợp đồng ra tệp CSV (UTF-8 BOM hỗ trợ mở trực tiếp bằng Excel tiếng Việt)
        /// </summary>
        Task<byte[]> ExportContractsCsvAsync();
    }
}
