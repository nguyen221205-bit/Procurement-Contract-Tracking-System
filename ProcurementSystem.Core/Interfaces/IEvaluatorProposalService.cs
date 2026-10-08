using ProcurementSystem.Core.DTOs;
using ProcurementSystem.Core.DTOs.EvaluatorProposal;

namespace ProcurementSystem.Core.Interfaces
{
    /// <summary>
    /// Giao diện dịch vụ Quản lý Đề xuất và Phê duyệt cấp tài khoản Giám khảo
    /// </summary>
    public interface IEvaluatorProposalService
    {
        /// <summary>
        /// Lấy danh sách đề xuất Giám khảo (Admin xem tất cả, Procurement chỉ xem đề xuất của mình)
        /// </summary>
        Task<ApiResponse<PaginatedList<EvaluatorProposalDto>>> GetProposalsAsync(
            EvaluatorProposalFilterParams filter, int currentUserId, string currentUserRole);

        /// <summary>
        /// Xem chi tiết một đề xuất Giám khảo theo ID
        /// </summary>
        Task<ApiResponse<EvaluatorProposalDto>> GetProposalByIdAsync(
            int id, int currentUserId, string currentUserRole);

        /// <summary>
        /// Bên mời thầu (Procurement) gửi đề xuất Giám khảo mới kèm thông tin và chứng chỉ
        /// </summary>
        Task<ApiResponse<EvaluatorProposalDto>> CreateProposalAsync(
            CreateEvaluatorProposalRequest request, int proposerUserId);

        /// <summary>
        /// Quản trị viên (Admin) phê duyệt đề xuất, ấn định mật khẩu khởi tạo và tạo tài khoản User (Role Evaluator)
        /// </summary>
        Task<ApiResponse<EvaluatorProposalDto>> ApproveProposalAsync(
            int id, ApproveEvaluatorProposalRequest request, int adminUserId);

        /// <summary>
        /// Quản trị viên (Admin) từ chối đề xuất Giám khảo kèm lý do giải trình
        /// </summary>
        Task<ApiResponse<EvaluatorProposalDto>> RejectProposalAsync(
            int id, RejectEvaluatorProposalRequest request, int adminUserId);

        /// <summary>
        /// Tải về hoặc xem tệp Chứng chỉ nghiệp vụ / Bằng cấp chuyên môn của đề xuất
        /// </summary>
        Task<ApiResponse<(string PhysicalPath, string ContentType, string FileName)>> GetCertificateFileForDownloadAsync(
            int id, int currentUserId, string currentUserRole);
    }
}
