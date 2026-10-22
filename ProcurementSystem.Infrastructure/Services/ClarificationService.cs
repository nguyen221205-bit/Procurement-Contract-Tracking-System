using Microsoft.EntityFrameworkCore;
using ProcurementSystem.Core.DTOs;
using ProcurementSystem.Core.DTOs.Clarification;
using ProcurementSystem.Core.Enums;
using ProcurementSystem.Core.Interfaces;
using ProcurementSystem.Infrastructure.Entities;

namespace ProcurementSystem.Infrastructure.Services
{
    public class ClarificationService : IClarificationService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly INotificationService _notificationService;

        public ClarificationService(
            IUnitOfWork unitOfWork,
            INotificationService notificationService)
        {
            _unitOfWork = unitOfWork;
            _notificationService = notificationService;
        }

        public async Task<ApiResponse<List<BidClarificationDto>>> GetClarificationsAsync(
            int packageId,
            int userId,
            bool isAdmin,
            bool isProcurement,
            bool isEvaluator,
            bool isContractor,
            ClarificationType? type = null)
        {
            var query = _unitOfWork.Repository<BidClarification>()
                .Query()
                .Include(c => c.BidPackage)
                .Include(c => c.BidSubmission)
                    .ThenInclude(s => s!.Contractor)
                .Include(c => c.AskedByUser)
                .Include(c => c.AnsweredByUser)
                .Where(c => c.BidPackageId == packageId)
                .AsNoTracking();

            if (type.HasValue)
            {
                query = query.Where(c => c.Type == type.Value);
            }

            // Quy tắc bảo mật phân quyền:
            // - HSMT (PackageClarification): Công khai cho toàn bộ người dùng
            // - HSDT (SubmissionClarification): Nếu là Nhà thầu (Contractor), CHỈ xem được các yêu cầu làm rõ hồ sơ của chính mình
            if (isContractor && !isAdmin && !isProcurement && !isEvaluator)
            {
                // Tìm ContractorId của User hiện tại
                var contractor = await _unitOfWork.Repository<Contractor>()
                    .Query()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.UserId == userId);

                int contractorId = contractor?.Id ?? 0;

                query = query.Where(c =>
                    c.Type == ClarificationType.PackageClarification ||
                    (c.Type == ClarificationType.SubmissionClarification && c.BidSubmission != null && c.BidSubmission.ContractorId == contractorId));
            }

            var list = await query
                .OrderByDescending(c => c.AskedAt)
                .Select(c => new BidClarificationDto
                {
                    Id = c.Id,
                    BidPackageId = c.BidPackageId,
                    BidPackageCode = c.BidPackage.Code,
                    BidPackageName = c.BidPackage.Name,
                    BidSubmissionId = c.BidSubmissionId,
                    ContractorName = c.BidSubmission != null && c.BidSubmission.Contractor != null ? c.BidSubmission.Contractor.CompanyName : null,
                    Type = c.Type,
                    TypeName = c.Type == ClarificationType.PackageClarification ? "Làm rõ HSMT" : "Làm rõ HSDT",
                    Title = c.Title,
                    QuestionContent = c.QuestionContent,
                    QuestionAttachmentUrl = c.QuestionAttachmentUrl,
                    AskedByUserId = c.AskedByUserId,
                    AskedByUserName = c.AskedByUser.FullName,
                    AskedAt = c.AskedAt,
                    Deadline = c.Deadline,
                    AnswerContent = c.AnswerContent,
                    AnswerAttachmentUrl = c.AnswerAttachmentUrl,
                    AnsweredByUserId = c.AnsweredByUserId,
                    AnsweredByUserName = c.AnsweredByUser != null ? c.AnsweredByUser.FullName : null,
                    AnsweredAt = c.AnsweredAt,
                    Status = c.Status,
                    StatusName = c.Status == ClarificationStatus.Pending ? "Chờ phản hồi" : "Đã phản hồi"
                })
                .ToListAsync();

            return ApiResponse<List<BidClarificationDto>>.Ok(list);
        }

        public async Task<ApiResponse<BidClarificationDto>> CreatePackageClarificationAsync(
            int packageId,
            CreatePackageClarificationRequest request,
            int contractorUserId)
        {
            var package = await _unitOfWork.Repository<BidPackage>().GetByIdAsync(packageId);
            if (package == null)
            {
                return ApiResponse<BidClarificationDto>.Fail("Không tìm thấy gói thầu.");
            }

            var clarification = new BidClarification
            {
                BidPackageId = packageId,
                BidSubmissionId = null,
                Type = ClarificationType.PackageClarification,
                Title = request.Title.Trim(),
                QuestionContent = request.QuestionContent.Trim(),
                QuestionAttachmentUrl = request.QuestionAttachmentUrl,
                AskedByUserId = contractorUserId,
                AskedAt = DateTime.UtcNow,
                Status = ClarificationStatus.Pending
            };

            await _unitOfWork.Repository<BidClarification>().AddAsync(clarification);
            await _unitOfWork.SaveChangesAsync();

            // Gửi thông báo & SignalR đến Bên mời thầu phụ trách và Admin
            var recipients = new List<int> { package.CreatedBy };

            var adminUserIds = await _unitOfWork.Repository<UserRole>()
                .Query()
                .Include(ur => ur.Role)
                .Where(ur => ur.Role.Name == "Admin")
                .Select(ur => ur.UserId)
                .ToListAsync();

            recipients.AddRange(adminUserIds);

            await _notificationService.CreateBulkNotificationsAsync(
                recipients.Distinct(),
                "Yêu cầu làm rõ HSMT mới",
                $"Nhà thầu vừa gửi câu hỏi làm rõ HSMT cho gói thầu [{package.Code}] {package.Name}: '{clarification.Title}'",
                NotificationType.Info,
                sendEmail: false
            );

            var user = await _unitOfWork.Repository<User>().GetByIdAsync(contractorUserId);

            return ApiResponse<BidClarificationDto>.Ok(new BidClarificationDto
            {
                Id = clarification.Id,
                BidPackageId = packageId,
                BidPackageCode = package.Code,
                BidPackageName = package.Name,
                Type = clarification.Type,
                TypeName = "Làm rõ HSMT",
                Title = clarification.Title,
                QuestionContent = clarification.QuestionContent,
                QuestionAttachmentUrl = clarification.QuestionAttachmentUrl,
                AskedByUserId = contractorUserId,
                AskedByUserName = user?.FullName ?? "Nhà thầu",
                AskedAt = clarification.AskedAt,
                Status = clarification.Status,
                StatusName = "Chờ phản hồi"
            }, "Gửi câu hỏi làm rõ HSMT thành công.");
        }

        public async Task<ApiResponse<BidClarificationDto>> CreateSubmissionClarificationAsync(
            int packageId,
            CreateSubmissionClarificationRequest request,
            int evaluatorOrProcurementUserId)
        {
            var package = await _unitOfWork.Repository<BidPackage>().GetByIdAsync(packageId);
            if (package == null)
            {
                return ApiResponse<BidClarificationDto>.Fail("Không tìm thấy gói thầu.");
            }

            var submission = await _unitOfWork.Repository<BidSubmission>()
                .Query()
                .Include(s => s.Contractor)
                    .ThenInclude(c => c.User)
                .FirstOrDefaultAsync(s => s.Id == request.BidSubmissionId && s.BidPackageId == packageId);

            if (submission == null)
            {
                return ApiResponse<BidClarificationDto>.Fail("Không tìm thấy hồ sơ dự thầu cần làm rõ trong gói thầu này.");
            }

            if (request.Deadline <= DateTime.UtcNow)
            {
                return ApiResponse<BidClarificationDto>.Fail("Thời hạn giải trình bổ sung hồ sơ phải ở một mốc thời gian trong tương lai.");
            }

            var clarification = new BidClarification
            {
                BidPackageId = packageId,
                BidSubmissionId = request.BidSubmissionId,
                Type = ClarificationType.SubmissionClarification,
                Title = request.Title.Trim(),
                QuestionContent = request.QuestionContent.Trim(),
                QuestionAttachmentUrl = request.QuestionAttachmentUrl,
                AskedByUserId = evaluatorOrProcurementUserId,
                AskedAt = DateTime.UtcNow,
                Deadline = request.Deadline,
                Status = ClarificationStatus.Pending
            };

            await _unitOfWork.Repository<BidClarification>().AddAsync(clarification);
            await _unitOfWork.SaveChangesAsync();

            // Gửi thông báo & SignalR trực tiếp cho Nhà thầu sở hữu hồ sơ dự thầu
            if (submission.Contractor?.UserId != null)
            {
                await _notificationService.CreateNotificationAsync(
                    submission.Contractor.UserId,
                    "Yêu cầu giải trình bổ sung Hồ sơ dự thầu (HSDT)",
                    $"Bên mời thầu / Tổ chuyên gia yêu cầu giải trình HSDT cho gói thầu [{package.Code}]: '{clarification.Title}'. Hạn chót: {request.Deadline:dd/MM/yyyy HH:mm}.",
                    NotificationType.Warning,
                    sendEmail: true
                );
            }

            var sender = await _unitOfWork.Repository<User>().GetByIdAsync(evaluatorOrProcurementUserId);

            return ApiResponse<BidClarificationDto>.Ok(new BidClarificationDto
            {
                Id = clarification.Id,
                BidPackageId = packageId,
                BidPackageCode = package.Code,
                BidPackageName = package.Name,
                BidSubmissionId = submission.Id,
                ContractorName = submission.Contractor?.CompanyName,
                Type = clarification.Type,
                TypeName = "Làm rõ HSDT",
                Title = clarification.Title,
                QuestionContent = clarification.QuestionContent,
                QuestionAttachmentUrl = clarification.QuestionAttachmentUrl,
                AskedByUserId = evaluatorOrProcurementUserId,
                AskedByUserName = sender?.FullName ?? "Giám khảo / Bên mời thầu",
                AskedAt = clarification.AskedAt,
                Deadline = clarification.Deadline,
                Status = clarification.Status,
                StatusName = "Chờ phản hồi"
            }, "Đã gửi yêu cầu làm rõ hồ sơ dự thầu tới Nhà thầu.");
        }

        public async Task<ApiResponse<BidClarificationDto>> AnswerClarificationAsync(
            int clarificationId,
            AnswerClarificationRequest request,
            int currentUserId,
            bool isAdmin,
            bool isProcurement,
            bool isContractor)
        {
            var clarification = await _unitOfWork.Repository<BidClarification>()
                .Query()
                .Include(c => c.BidPackage)
                .Include(c => c.BidSubmission)
                    .ThenInclude(s => s!.Contractor)
                .Include(c => c.AskedByUser)
                .FirstOrDefaultAsync(c => c.Id == clarificationId);

            if (clarification == null)
            {
                return ApiResponse<BidClarificationDto>.Fail("Không tìm thấy bản ghi làm rõ.");
            }

            // Kiểm tra phân quyền trả lời:
            if (clarification.Type == ClarificationType.PackageClarification)
            {
                // Làm rõ HSMT -> Chỉ Admin hoặc Procurement mới được trả lời
                if (!isAdmin && !isProcurement)
                {
                    return ApiResponse<BidClarificationDto>.Fail("Chỉ Bên mời thầu hoặc Admin mới có thẩm quyền phản hồi làm rõ HSMT.");
                }
            }
            else if (clarification.Type == ClarificationType.SubmissionClarification)
            {
                // Làm rõ HSDT -> Chỉ chính Nhà thầu sở hữu HSDT mới được nộp văn bản giải trình
                var contractorUserId = clarification.BidSubmission?.Contractor?.UserId;
                if (!isAdmin && contractorUserId != currentUserId)
                {
                    return ApiResponse<BidClarificationDto>.Fail("Chỉ Nhà thầu sở hữu hồ sơ dự thầu này mới được quyền gửi văn bản giải trình.");
                }
            }

            clarification.AnswerContent = request.AnswerContent.Trim();
            clarification.AnswerAttachmentUrl = request.AnswerAttachmentUrl;
            clarification.AnsweredByUserId = currentUserId;
            clarification.AnsweredAt = DateTime.UtcNow;
            clarification.Status = ClarificationStatus.Answered;

            _unitOfWork.Repository<BidClarification>().Update(clarification);
            await _unitOfWork.SaveChangesAsync();

            // Gửi thông báo & SignalR phản hồi cho người hỏi ban đầu
            var responder = await _unitOfWork.Repository<User>().GetByIdAsync(currentUserId);

            var notifTitle = clarification.Type == ClarificationType.PackageClarification
                ? "Phản hồi làm rõ HSMT"
                : "Văn bản giải trình HSDT từ Nhà thầu";

            var notifMessage = clarification.Type == ClarificationType.PackageClarification
                ? $"Bên mời thầu đã phản hồi câu hỏi làm rõ HSMT gói thầu [{clarification.BidPackage.Code}]: '{clarification.Title}'"
                : $"Nhà thầu ({clarification.BidSubmission?.Contractor?.CompanyName}) đã nộp văn bản giải trình HSDT gói thầu [{clarification.BidPackage.Code}]: '{clarification.Title}'";

            await _notificationService.CreateNotificationAsync(
                clarification.AskedByUserId,
                notifTitle,
                notifMessage,
                NotificationType.Success,
                sendEmail: false
            );

            return ApiResponse<BidClarificationDto>.Ok(new BidClarificationDto
            {
                Id = clarification.Id,
                BidPackageId = clarification.BidPackageId,
                BidPackageCode = clarification.BidPackage.Code,
                BidPackageName = clarification.BidPackage.Name,
                BidSubmissionId = clarification.BidSubmissionId,
                ContractorName = clarification.BidSubmission?.Contractor?.CompanyName,
                Type = clarification.Type,
                TypeName = clarification.Type == ClarificationType.PackageClarification ? "Làm rõ HSMT" : "Làm rõ HSDT",
                Title = clarification.Title,
                QuestionContent = clarification.QuestionContent,
                QuestionAttachmentUrl = clarification.QuestionAttachmentUrl,
                AskedByUserId = clarification.AskedByUserId,
                AskedByUserName = clarification.AskedByUser.FullName,
                AskedAt = clarification.AskedAt,
                Deadline = clarification.Deadline,
                AnswerContent = clarification.AnswerContent,
                AnswerAttachmentUrl = clarification.AnswerAttachmentUrl,
                AnsweredByUserId = currentUserId,
                AnsweredByUserName = responder?.FullName,
                AnsweredAt = clarification.AnsweredAt,
                Status = clarification.Status,
                StatusName = "Đã phản hồi"
            }, "Gửi phản hồi / giải trình thành công.");
        }
    }
}
