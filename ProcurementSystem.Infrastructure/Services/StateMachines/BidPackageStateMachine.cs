using Microsoft.EntityFrameworkCore;
using ProcurementSystem.Core.DTOs.BidPackage;
using ProcurementSystem.Core.Enums;
using ProcurementSystem.Core.Interfaces;
using ProcurementSystem.Infrastructure.Entities;

namespace ProcurementSystem.Infrastructure.Services.StateMachines
{
    public sealed record StateTransitionValidationResult(
        bool IsValid,
        string? ErrorMessage = null,
        bool IsForbidden = false)
    {
        public static readonly StateTransitionValidationResult Valid = new(true);

        public static StateTransitionValidationResult Fail(string message) =>
            new(false, message, IsForbidden: false);

        public static StateTransitionValidationResult Forbidden(string message) =>
            new(false, message, IsForbidden: true);
    }

    public sealed record BidPackageTransitionContext(
        BidPackage Package,
        ChangeBidPackageStatusRequest Request,
        IUnitOfWork UnitOfWork,
        DateTime UtcNow);

    /// <summary>
    /// Máy trạng thái gói thầu theo hướng khai báo quy tắc (Declarative State Machine & Rule Pipeline - tuân thủ SRP & OCP).
    /// Cho phép mở rộng quy tắc chuyển trạng thái mới mà không cần sửa đổi hàm điều phối trong BidPackageService.
    /// </summary>
    public static class BidPackageStateMachine
    {
        private static readonly string PercentSymbol = char.ConvertFromUtf32(37);

        private static readonly IReadOnlySet<BidPackageStatus> LockedCurrentStatuses =
            new HashSet<BidPackageStatus>
            {
                BidPackageStatus.Awarded,
                BidPackageStatus.Contracted
            };

        private static readonly IReadOnlySet<BidPackageStatus> SystemManagedTargetStatuses =
            new HashSet<BidPackageStatus>
            {
                BidPackageStatus.Awarded,
                BidPackageStatus.Contracted
            };

        private static readonly IReadOnlyDictionary<BidPackageStatus, IReadOnlySet<BidPackageStatus>> AllowedTransitions =
            new Dictionary<BidPackageStatus, IReadOnlySet<BidPackageStatus>>
            {
                [BidPackageStatus.Open] = new HashSet<BidPackageStatus>
                {
                    BidPackageStatus.Closed,
                    BidPackageStatus.Cancelled
                },
                [BidPackageStatus.Closed] = new HashSet<BidPackageStatus>
                {
                    BidPackageStatus.Evaluating,
                    BidPackageStatus.Open,
                    BidPackageStatus.Cancelled
                },
                [BidPackageStatus.Evaluating] = new HashSet<BidPackageStatus>
                {
                    BidPackageStatus.Closed,
                    BidPackageStatus.Cancelled
                },
                [BidPackageStatus.Awarded] = new HashSet<BidPackageStatus>(),
                [BidPackageStatus.Contracted] = new HashSet<BidPackageStatus>(),
                [BidPackageStatus.Cancelled] = new HashSet<BidPackageStatus>()
            };

        private static readonly IReadOnlyList<Func<BidPackageTransitionContext, Task<StateTransitionValidationResult>>> TransitionRules =
            new Func<BidPackageTransitionContext, Task<StateTransitionValidationResult>>[]
            {
                ValidateNoScoringOrAwardRollbackAsync,
                ValidateReopenDeadlineRuleAsync,
                ValidateEarlyCloseReasonRuleAsync,
                ValidateCancellationRuleAsync,
                ValidateEvaluatingEntryRequirementsAsync
            };

        public static bool CanTransition(BidPackageStatus currentStatus, BidPackageStatus targetStatus)
        {
            return AllowedTransitions.TryGetValue(currentStatus, out var targets) && targets.Contains(targetStatus);
        }

        public static async Task<StateTransitionValidationResult> ValidateTransitionAsync(BidPackageTransitionContext context)
        {
            var current = context.Package.Status;
            var target = context.Request.NewStatus;

            if (LockedCurrentStatuses.Contains(current))
            {
                return StateTransitionValidationResult.Fail(
                    $"Gói thầu hiện đang ở trạng thái '{current}'. Không thể thay đổi trạng thái gói thầu một khi đã có kết quả trao thầu hoặc đã ký hợp đồng.");
            }

            if (SystemManagedTargetStatuses.Contains(target))
            {
                return StateTransitionValidationResult.Fail(
                    $"Không thể chuyển trạng thái thủ công sang '{target}'. " +
                    "Trạng thái 'Awarded' được hệ thống tự động thiết lập khi phê duyệt kết quả trao thầu; " +
                    "trạng thái 'Contracted' được tự động thiết lập khi hợp đồng kinh tế chính thức có hiệu lực.");
            }

            if (!CanTransition(current, target))
            {
                return StateTransitionValidationResult.Fail(
                    $"Không thể chuyển trạng thái từ '{current}' sang '{target}'. Quy trình bắt buộc: Đang mở -> Đã đóng -> Đang chấm -> Đã trao thầu -> Đã ký HĐ.");
            }

            foreach (var rule in TransitionRules)
            {
                var result = await rule(context);
                if (!result.IsValid)
                {
                    return result;
                }
            }

            return StateTransitionValidationResult.Valid;
        }

        public static bool RequiresApprovalWorkflow(BidPackage package, BidPackageStatus targetStatus, DateTime utcNow)
        {
            var isEarlyClose = package.Status == BidPackageStatus.Open
                && targetStatus == BidPackageStatus.Closed
                && package.Deadline > utcNow;
            var isReopenOrExtend = package.Status == BidPackageStatus.Closed
                && targetStatus == BidPackageStatus.Open;
            var isCancel = targetStatus == BidPackageStatus.Cancelled;

            return isEarlyClose || isReopenOrExtend || isCancel;
        }

        public static string GetApprovalActionLabel(BidPackageStatus targetStatus)
        {
            return targetStatus switch
            {
                BidPackageStatus.Closed => "Đóng thầu trước thời hạn",
                BidPackageStatus.Open => "Mở lại / Gia hạn thời gian nộp thầu",
                BidPackageStatus.Cancelled => "Hủy gói thầu",
                _ => $"Chuyển trạng thái sang {targetStatus}"
            };
        }

        private static async Task<StateTransitionValidationResult> ValidateNoScoringOrAwardRollbackAsync(
            BidPackageTransitionContext ctx)
        {
            if (ctx.Request.NewStatus != BidPackageStatus.Open && ctx.Request.NewStatus != BidPackageStatus.Closed)
            {
                return StateTransitionValidationResult.Valid;
            }

            var packageId = ctx.Package.Id;
            var hasScores = await ctx.UnitOfWork.Repository<EvaluationScore>()
                .Query()
                .AnyAsync(es => es.Criteria.BidPackageId == packageId || es.BidSubmission.BidPackageId == packageId);

            var hasWinner = await ctx.UnitOfWork.Repository<BidSubmission>()
                .Query()
                .AnyAsync(bs => bs.BidPackageId == packageId &&
                                (bs.Status == BidSubmissionStatus.Selected || bs.Status == BidSubmissionStatus.Rejected));

            if (hasScores || hasWinner)
            {
                return StateTransitionValidationResult.Fail(
                    "Căn cứ Điều 16 Luật Đấu thầu 2023: Tuyệt đối không được phép mở lại hoặc quay lui trạng thái gói thầu " +
                    "sau khi Tổ chuyên gia đã tiến hành đánh giá chấm điểm hoặc đã có kết quả trao thầu.");
            }

            return StateTransitionValidationResult.Valid;
        }

        private static Task<StateTransitionValidationResult> ValidateReopenDeadlineRuleAsync(
            BidPackageTransitionContext ctx)
        {
            if (ctx.Package.Status != BidPackageStatus.Closed || ctx.Request.NewStatus != BidPackageStatus.Open)
            {
                return Task.FromResult(StateTransitionValidationResult.Valid);
            }

            if (ctx.Request.NewDeadline.HasValue)
            {
                if (ctx.Request.NewDeadline.Value <= ctx.UtcNow)
                {
                    return Task.FromResult(StateTransitionValidationResult.Fail(
                        "Thời hạn nộp thầu gia hạn mới (NewDeadline) phải lớn hơn thời điểm hiện tại."));
                }
            }
            else if (ctx.Package.Deadline <= ctx.UtcNow)
            {
                return Task.FromResult(StateTransitionValidationResult.Fail(
                    "Gói thầu đã hết hạn nộp thầu. Để mở lại gói thầu (Closed -> Open), bắt buộc phải cung cấp thời hạn nộp thầu mới (NewDeadline) ở tương lai."));
            }

            return Task.FromResult(StateTransitionValidationResult.Valid);
        }

        private static Task<StateTransitionValidationResult> ValidateEarlyCloseReasonRuleAsync(
            BidPackageTransitionContext ctx)
        {
            if (ctx.Package.Status == BidPackageStatus.Open &&
                ctx.Request.NewStatus == BidPackageStatus.Closed &&
                ctx.Package.Deadline > ctx.UtcNow &&
                string.IsNullOrWhiteSpace(ctx.Request.Reason))
            {
                return Task.FromResult(StateTransitionValidationResult.Fail(
                    "Gói thầu chưa hết thời hạn nộp thầu. Nếu muốn đóng thầu trước thời hạn, bắt buộc phải cung cấp lý do/căn cứ pháp lý."));
            }

            return Task.FromResult(StateTransitionValidationResult.Valid);
        }

        private static async Task<StateTransitionValidationResult> ValidateCancellationRuleAsync(
            BidPackageTransitionContext ctx)
        {
            if (ctx.Request.NewStatus != BidPackageStatus.Cancelled)
            {
                return StateTransitionValidationResult.Valid;
            }

            if (string.IsNullOrWhiteSpace(ctx.Request.Reason))
            {
                return StateTransitionValidationResult.Fail(
                    "Hủy gói thầu bắt buộc phải có lý do / căn cứ pháp lý hủy thầu theo quy định của Luật Đấu thầu.");
            }

            var hasActiveContract = await ctx.UnitOfWork.Repository<Contract>()
                .Query()
                .AnyAsync(c => c.BidPackageId == ctx.Package.Id &&
                               (c.Status == ContractStatus.Active || c.Status == ContractStatus.Completed));

            if (hasActiveContract)
            {
                return StateTransitionValidationResult.Fail(
                    "Không thể hủy gói thầu vì đã có hợp đồng kinh tế đang thực hiện hoặc đã hoàn thành.");
            }

            return StateTransitionValidationResult.Valid;
        }

        private static async Task<StateTransitionValidationResult> ValidateEvaluatingEntryRequirementsAsync(
            BidPackageTransitionContext ctx)
        {
            if (ctx.Request.NewStatus != BidPackageStatus.Evaluating)
            {
                return StateTransitionValidationResult.Valid;
            }

            var packageId = ctx.Package.Id;
            var criteriaList = await ctx.UnitOfWork.Repository<EvaluationCriteria>()
                .Query()
                .Where(c => c.BidPackageId == packageId)
                .ToListAsync();

            if (!criteriaList.Any())
            {
                return StateTransitionValidationResult.Fail(
                    "Không thể bắt đầu giai đoạn Chấm điểm. Gói thầu chưa được thiết lập bộ tiêu chí đánh giá.");
            }

            var totalWeight = criteriaList.Sum(c => c.Weight);
            if (totalWeight != 100)
            {
                return StateTransitionValidationResult.Fail(
                    $"Không thể bắt đầu giai đoạn Chấm điểm. Tổng trọng số bộ tiêu chí phải đạt đúng 100{PercentSymbol} (Hiện tại: {totalWeight}{PercentSymbol}).");
            }

            var evaluatorsCount = await ctx.UnitOfWork.Repository<BidPackageEvaluator>()
                .CountAsync(pe => pe.BidPackageId == packageId);

            if (evaluatorsCount < 3 || (evaluatorsCount & 1) == 0)
            {
                return StateTransitionValidationResult.Fail(
                    $"Không thể bắt đầu giai đoạn Chấm điểm. Tổ chuyên gia phải có tối thiểu 3 thành viên và số lượng thành viên phải là số lẻ (3, 5, 7,...) theo quy định của Luật Đấu thầu (Hiện tại: {evaluatorsCount} thành viên). Vui lòng phân công đủ giám khảo trước khi chuyển trạng thái.");
            }

            var validSubmissionsCount = await ctx.UnitOfWork.Repository<BidSubmission>()
                .CountAsync(s => s.BidPackageId == packageId && s.Status != BidSubmissionStatus.Withdrawn);

            if (validSubmissionsCount == 0)
            {
                return StateTransitionValidationResult.Fail(
                    "Không thể bắt đầu giai đoạn Chấm điểm vì gói thầu không có hồ sơ dự thầu nào được nộp (hoặc tất cả đã rút thầu). Căn cứ Điều 17 Luật Đấu thầu 2023, Bên mời thầu vui lòng báo cáo Người có thẩm quyền để xem xét hủy thầu hoặc gia hạn thời điểm đóng thầu.");
            }

            return StateTransitionValidationResult.Valid;
        }
    }
}
