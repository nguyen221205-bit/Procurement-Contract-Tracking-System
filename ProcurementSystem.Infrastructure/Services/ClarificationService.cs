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

            var package = await _unitOfWork.Repository<BidPackage>().GetByIdAsync(packageId);
            if (package == null)
            {
                return ApiResponse<List<BidClarificationDto>>.Fail("Không tìm thấy gói thầu.");
            }

            // F5-3: Chống IDOR xem Làm rõ HSDT mật
            // - HSMT (PackageClarification): Công khai cho toàn bộ người dùng
            // - HSDT (SubmissionClarification): 
            //   + Admin: xem toàn bộ
            //   + Procurement: chỉ xem nếu chính là chủ sở hữu gói thầu (package.CreatedBy == userId)
            //   + Evaluator: chỉ xem nếu thuộc Tổ chuyên gia (BidPackageEvaluator) của gói thầu
            //   + Contractor: chỉ xem hồ sơ của chính doanh nghiệp mình
            //   + Các đối tượng khác: bị ẩn toàn bộ SubmissionClarification, chỉ xem PackageClarification
            bool canViewPrivateSubmissionClarifications = isAdmin;
            if (!canViewPrivateSubmissionClarifications)
            {
                if (isProcurement && package.CreatedBy == userId)
                {
                    canViewPrivateSubmissionClarifications = true;
                }
                else if (isEvaluator)
                {
                    var isPackageEvaluator = await _unitOfWork.Repository<BidPackageEvaluator>()
                        .ExistsAsync(pe => pe.BidPackageId == packageId && pe.EvaluatorId == userId);
                    if (isPackageEvaluator)
                    {
                        canViewPrivateSubmissionClarifications = true;
                    }
                }
            }

            if (isContractor)
            {
                var contractor = await _unitOfWork.Repository<Contractor>()
                    .Query()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.UserId == userId);

                int contractorId = contractor?.Id ?? 0;

                query = query.Where(c =>
                    c.Type == ClarificationType.PackageClarification ||
                    (c.Type == ClarificationType.SubmissionClarification && c.BidSubmission != null && c.BidSubmission.ContractorId == contractorId));
            }
            else if (!canViewPrivateSubmissionClarifications)
            {
                // Giám khảo ngoài tổ hoặc Bên mời thầu khác: Chặn IDOR xem giải trình HSDT
                query = query.Where(c => c.Type == ClarificationType.PackageClarification);
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

            // F5-3: Kiểm tra trạng thái gói thầu & hạn nộp thầu
            if (package.Status != BidPackageStatus.Open)
            {
                return ApiResponse<BidClarificationDto>.Fail("Chỉ được gửi yêu cầu làm rõ HSMT khi gói thầu đang trong thời gian mời thầu (Open).");
            }

            if (package.Deadline <= DateTime.UtcNow)
            {
                return ApiResponse<BidClarificationDto>.Fail("Gói thầu đã kết thúc thời hạn nộp thầu, không thể gửi yêu cầu làm rõ HSMT.");
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
            int evaluatorOrProcurementUserId,
            bool isAdmin)
        {
            var package = await _unitOfWork.Repository<BidPackage>().GetByIdAsync(packageId);
            if (package == null)
            {
                return ApiResponse<BidClarificationDto>.Fail("Không tìm thấy gói thầu.");
            }

            // F5-3: Kiểm tra trạng thái gói thầu phải là Evaluating hoặc Closed
            if (package.Status != BidPackageStatus.Evaluating && package.Status != BidPackageStatus.Closed)
            {
                return ApiResponse<BidClarificationDto>.Fail("Chỉ được yêu cầu giải trình HSDT khi gói thầu đang ở trạng thái Đã đóng thầu (Closed) hoặc Đang chấm điểm (Evaluating).");
            }

            // F5-3: Kiểm tra thẩm quyền người gửi
            if (!isAdmin)
            {
                bool isOwner = package.CreatedBy == evaluatorOrProcurementUserId;
                bool isPackageEvaluator = await _unitOfWork.Repository<BidPackageEvaluator>()
                    .ExistsAsync(pe => pe.BidPackageId == packageId && pe.EvaluatorId == evaluatorOrProcurementUserId);

                if (!isOwner && !isPackageEvaluator)
                {
                    return ApiResponse<BidClarificationDto>.Forbidden("Bạn không có thẩm quyền yêu cầu giải trình HSDT cho gói thầu này.");
                }
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

            // F5-3: Chặn yêu cầu làm rõ hồ sơ đã rút hoặc bị loại
            if (submission.Status == BidSubmissionStatus.Withdrawn || submission.Status == BidSubmissionStatus.Disqualified)
            {
                return ApiResponse<BidClarificationDto>.Fail("Không thể yêu cầu làm rõ đối với hồ sơ dự thầu đã rút (Withdrawn) hoặc đã bị loại (Disqualified).");
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

            // F5-3: Chặn ghi đè câu trả lời đã gửi
            if (clarification.Status == ClarificationStatus.Answered)
            {
                return ApiResponse<BidClarificationDto>.Fail("Yêu cầu làm rõ này đã được phản hồi trước đó, không thể nộp lại hoặc ghi đè.");
            }

            // Kiểm tra phân quyền và hạn chót:
            if (clarification.Type == ClarificationType.PackageClarification)
            {
                if (!isAdmin && !isProcurement)
                {
                    return ApiResponse<BidClarificationDto>.Fail("Chỉ Bên mời thầu hoặc Admin mới có thẩm quyền phản hồi làm rõ HSMT.");
                }

                // F5-3: Bên mời thầu phải là chủ sở hữu gói thầu
                if (!isAdmin && clarification.BidPackage.CreatedBy != currentUserId)
                {
                    return ApiResponse<BidClarificationDto>.Forbidden("Bạn chỉ có thẩm quyền phản hồi làm rõ HSMT cho gói thầu do chính mình phụ trách.");
                }
            }
            else if (clarification.Type == ClarificationType.SubmissionClarification)
            {
                var contractorUserId = clarification.BidSubmission?.Contractor?.UserId;
                if (!isAdmin && contractorUserId != currentUserId)
                {
                    return ApiResponse<BidClarificationDto>.Fail("Chỉ Nhà thầu sở hữu hồ sơ dự thầu này mới được quyền gửi văn bản giải trình.");
                }

                // F5-3: Kiểm tra hạn chót giải trình HSDT
                if (clarification.Deadline.HasValue && DateTime.UtcNow > clarification.Deadline.Value)
                {
                    return ApiResponse<BidClarificationDto>.Fail($"Đã quá thời hạn nộp văn bản giải trình HSDT ({clarification.Deadline.Value:dd/MM/yyyy HH:mm} UTC). Hệ thống đã ngừng tiếp nhận.");
                }
            }

            clarification.AnswerContent = request.AnswerContent.Trim();
            clarification.AnswerAttachmentUrl = request.AnswerAttachmentUrl;
            clarification.AnsweredByUserId = currentUserId;
            clarification.AnsweredAt = DateTime.UtcNow;
            clarification.Status = ClarificationStatus.Answered;

            _unitOfWork.Repository<BidClarification>().Update(clarification);
            await _unitOfWork.SaveChangesAsync();

            // Gửi thông báo & SignalR phản hồi
            var responder = await _unitOfWork.Repository<User>().GetByIdAsync(currentUserId);

            var notifTitle = clarification.Type == ClarificationType.PackageClarification
                ? "Phản hồi làm rõ HSMT công khai"
                : "Văn bản giải trình HSDT từ Nhà thầu";

            var notifMessage = clarification.Type == ClarificationType.PackageClarification
                ? $"Bên mời thầu đã phản hồi câu hỏi làm rõ HSMT gói thầu [{clarification.BidPackage.Code}]: '{clarification.Title}'"
                : $"Nhà thầu ({clarification.BidSubmission?.Contractor?.CompanyName}) đã nộp văn bản giải trình HSDT gói thầu [{clarification.BidPackage.Code}]: '{clarification.Title}'";

            // F5-3: Với làm rõ HSMT công khai, thông báo cho người hỏi và toàn bộ nhà thầu đã nộp HSDT
            if (clarification.Type == ClarificationType.PackageClarification)
            {
                var submittedContractorUserIds = await _unitOfWork.Repository<BidSubmission>()
                    .Query()
                    .Include(s => s.Contractor)
                    .Where(s => s.BidPackageId == clarification.BidPackageId && s.Contractor != null && s.Contractor.UserId > 0)
                    .Select(s => s.Contractor!.UserId)
                    .ToListAsync();

                var recipients = submittedContractorUserIds
                    .Append(clarification.AskedByUserId)
                    .Distinct()
                    .Where(uid => uid != currentUserId)
                    .ToList();

                if (recipients.Any())
                {
                    await _notificationService.CreateBulkNotificationsAsync(
                        recipients,
                        notifTitle,
                        notifMessage,
                        NotificationType.Info,
                        sendEmail: false
                    );
                }
            }
            else
            {
                await _notificationService.CreateNotificationAsync(
                    clarification.AskedByUserId,
                    notifTitle,
                    notifMessage,
                    NotificationType.Success,
                    sendEmail: false
                );
            }

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
