using ProcurementSystem.Core.DTOs;
using ProcurementSystem.Core.DTOs.Clarification;
using ProcurementSystem.Core.Enums;

namespace ProcurementSystem.Core.Interfaces
{
    /// <summary>
    /// Giao diện dịch vụ Xử lý Làm rõ Hồ sơ mời thầu (HSMT) và Hồ sơ dự thầu (HSDT)
    /// </summary>
    public interface IClarificationService
    {
        /// <summary>
        /// Lấy danh sách yêu cầu làm rõ của gói thầu theo quyền hạn của người xem
        /// (HSMT: công khai; HSDT: chỉ Giám khảo, Bên mời thầu, Admin và chính Nhà thầu sở hữu hồ sơ)
        /// </summary>
        Task<ApiResponse<List<BidClarificationDto>>> GetClarificationsAsync(
            int packageId,
            int userId,
            bool isAdmin,
            bool isProcurement,
            bool isEvaluator,
            bool isContractor,
            ClarificationType? type = null);

        /// <summary>
        /// Nhà thầu gửi câu hỏi làm rõ HSMT
        /// </summary>
        Task<ApiResponse<BidClarificationDto>> CreatePackageClarificationAsync(
            int packageId,
            CreatePackageClarificationRequest request,
            int contractorUserId);

        /// <summary>
        /// Tổ chuyên gia hoặc Bên mời thầu yêu cầu giải trình HSDT
        /// </summary>
        Task<ApiResponse<BidClarificationDto>> CreateSubmissionClarificationAsync(
            int packageId,
            CreateSubmissionClarificationRequest request,
            int evaluatorOrProcurementUserId,
            bool isAdmin);

        /// <summary>
        /// Phản hồi câu hỏi HSMT (Procurement/Admin) hoặc nộp văn bản giải trình HSDT (Nhà thầu)
        /// </summary>
        Task<ApiResponse<BidClarificationDto>> AnswerClarificationAsync(
            int clarificationId,
            AnswerClarificationRequest request,
            int currentUserId,
            bool isAdmin,
            bool isProcurement,
            bool isContractor);
    }
}
