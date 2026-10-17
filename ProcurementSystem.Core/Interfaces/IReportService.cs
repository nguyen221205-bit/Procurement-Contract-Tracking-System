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

        /// <summary>
        /// Xuất danh sách toàn bộ nhà thầu ra tệp Excel định dạng .xlsx
        /// </summary>
        Task<byte[]> ExportContractorsExcelAsync();

        /// <summary>
        /// Xuất danh sách toàn bộ hợp đồng ra tệp Excel định dạng .xlsx
        /// </summary>
        Task<byte[]> ExportContractsExcelAsync();

        /// <summary>
        /// Báo cáo thống kê mua sắm theo năm và phân loại gói thầu (R3-10)
        /// </summary>
        Task<ApiResponse<ProcurementSummaryReportDto>> GetProcurementSummaryReportAsync(int? year = null, ProcurementSystem.Core.Enums.BidPackageType? type = null);

        /// <summary>
        /// Báo cáo tỷ lệ trúng/trượt thầu và năng lực thực tế của các nhà thầu (R3-10)
        /// </summary>
        Task<ApiResponse<ContractorPerformanceReportDto>> GetContractorPerformanceReportAsync();

        /// <summary>
        /// Báo cáo tỷ lệ hoàn thành mốc đúng tiến độ và danh sách hợp đồng sắp đến hạn (R3-10)
        /// </summary>
        Task<ApiResponse<ContractProgressReportDto>> GetContractProgressReportAsync(int daysAhead = 30);
    }
}
