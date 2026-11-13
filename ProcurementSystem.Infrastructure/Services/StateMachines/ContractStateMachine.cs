using ProcurementSystem.Core.DTOs.Contract;
using ProcurementSystem.Core.Enums;
using ProcurementSystem.Infrastructure.Entities;

namespace ProcurementSystem.Infrastructure.Services.StateMachines
{
    public sealed record ContractTransitionContext(
        Contract Contract,
        ChangeContractStatusRequest Request,
        int UserId,
        bool IsAdmin,
        DateTime UtcNow);

    /// <summary>
    /// Máy trạng thái hợp đồng kinh tế theo hướng khai báo quy tắc (Declarative State Machine & Rule Pipeline - tuân thủ SRP & OCP).
    /// Tách biệt hoàn toàn bảng quy tắc chuyển trạng thái và điều kiện nghiệp vụ khỏi ContractService.
    /// </summary>
    public static class ContractStateMachine
    {
        private static readonly string PercentSymbol = char.ConvertFromUtf32(37);

        private static readonly IReadOnlyDictionary<ContractStatus, IReadOnlySet<ContractStatus>> AllowedTransitions =
            new Dictionary<ContractStatus, IReadOnlySet<ContractStatus>>
            {
                [ContractStatus.Draft] = new HashSet<ContractStatus>
                {
                    ContractStatus.Active
                },
                [ContractStatus.Active] = new HashSet<ContractStatus>
                {
                    ContractStatus.Completed,
                    ContractStatus.Terminated
                },
                [ContractStatus.Completed] = new HashSet<ContractStatus>(),
                [ContractStatus.Terminated] = new HashSet<ContractStatus>
                {
                    ContractStatus.Active
                }
            };

        private static readonly IReadOnlyList<Func<ContractTransitionContext, StateTransitionValidationResult>> TransitionRules =
            new Func<ContractTransitionContext, StateTransitionValidationResult>[]
            {
                ValidateDraftToActiveRule,
                ValidateTerminationRule,
                ValidateReactivationRule,
                ValidateCompletionRule
            };

        public static bool CanTransition(ContractStatus currentStatus, ContractStatus targetStatus)
        {
            return AllowedTransitions.TryGetValue(currentStatus, out var targets) && targets.Contains(targetStatus);
        }

        public static StateTransitionValidationResult ValidateAndApplyTransitionMetadata(ContractTransitionContext context)
        {
            var current = context.Contract.Status;
            var target = context.Request.NewStatus;

            if (!CanTransition(current, target))
            {
                return StateTransitionValidationResult.Fail(
                    $"Không thể chuyển hợp đồng từ trạng thái '{current}' sang '{target}'.");
            }

            foreach (var rule in TransitionRules)
            {
                var result = rule(context);
                if (!result.IsValid)
                {
                    return result;
                }
            }

            return StateTransitionValidationResult.Valid;
        }

        private static StateTransitionValidationResult ValidateDraftToActiveRule(ContractTransitionContext ctx)
        {
            if (ctx.Contract.Status != ContractStatus.Draft || ctx.Request.NewStatus != ContractStatus.Active)
            {
                return StateTransitionValidationResult.Valid;
            }

            if (string.IsNullOrWhiteSpace(ctx.Contract.ScannedFilePath))
            {
                return StateTransitionValidationResult.Fail(
                    "Căn cứ Điều 65 Luật Đấu thầu: Hợp đồng phải được tải lên bản scan có chữ ký, con dấu pháp lý trước khi kích hoạt có hiệu lực.");
            }

            var milestones = ctx.Contract.Milestones?.ToList() ?? new List<ContractMilestone>();
            if (!milestones.Any())
            {
                return StateTransitionValidationResult.Fail(
                    "Hợp đồng chưa thiết lập các mốc thanh toán. Vui lòng tạo kế hoạch mốc thanh toán trước khi kích hoạt.");
            }

            var totalMilestoneAmount = milestones.Sum(m => m.Amount);
            if (totalMilestoneAmount != ctx.Contract.Value)
            {
                return StateTransitionValidationResult.Fail(
                    $"Tổng giá trị các mốc thanh toán ({totalMilestoneAmount:N0} VNĐ) phải đúng bằng 100{PercentSymbol} giá trị hợp đồng ({ctx.Contract.Value:N0} VNĐ).");
            }

            return StateTransitionValidationResult.Valid;
        }

        private static StateTransitionValidationResult ValidateTerminationRule(ContractTransitionContext ctx)
        {
            if (ctx.Request.NewStatus != ContractStatus.Terminated)
            {
                return StateTransitionValidationResult.Valid;
            }

            var pendingAcceptanceMilestone = ctx.Contract.Milestones?
                .FirstOrDefault(m => m.Acceptances != null && m.Acceptances.Any(a => a.Status == AcceptanceStatus.Pending));
            if (pendingAcceptanceMilestone != null)
            {
                return StateTransitionValidationResult.Fail(
                    $"Không thể chấm dứt hợp đồng khi mốc nghiệm thu '{pendingAcceptanceMilestone.Title}' đang có biên bản nghiệm thu chờ thẩm định duyệt. " +
                    "Vui lòng xử lý dứt điểm duyệt hoặc từ chối biên bản nghiệm thu này trước khi thực hiện chấm dứt hợp đồng.");
            }

            if (string.IsNullOrWhiteSpace(ctx.Request.TerminationReasonCategory))
            {
                return StateTransitionValidationResult.Fail(
                    "Căn cứ Luật Đấu thầu: Vui lòng chọn phân loại căn cứ pháp lý chấm dứt hợp đồng (Vi phạm nghĩa vụ hợp đồng, Bất khả kháng, Thỏa thuận hai bên hoặc Quyết định của cấp có thẩm quyền).");
            }

            if (string.IsNullOrWhiteSpace(ctx.Request.DecisionNumber))
            {
                return StateTransitionValidationResult.Fail(
                    "Vui lòng cung cấp Số hiệu Quyết định / Văn bản / Biên bản chấm dứt hợp đồng làm căn cứ pháp lý lưu trữ hồ sơ.");
            }

            if (string.IsNullOrWhiteSpace(ctx.Request.Note) || ctx.Request.Note.Trim().Length < 20)
            {
                return StateTransitionValidationResult.Fail(
                    "Lý do và nội dung giải trình chấm dứt hợp đồng phải có tối thiểu 20 ký tự, nêu rõ nguyên nhân và căn cứ pháp lý theo quy định.");
            }

            var terminationLog = $"[CHẤM DỨT HĐ {ctx.UtcNow:dd/MM/yyyy HH:mm} UTC] Căn cứ: {ctx.Request.TerminationReasonCategory} | Số hiệu QĐ: {ctx.Request.DecisionNumber.Trim()} | Lý do: {ctx.Request.Note.Trim()}";
            ctx.Contract.Terms = string.IsNullOrWhiteSpace(ctx.Contract.Terms)
                ? terminationLog
                : $"{ctx.Contract.Terms}\n\n{terminationLog}";

            return StateTransitionValidationResult.Valid;
        }

        private static StateTransitionValidationResult ValidateReactivationRule(ContractTransitionContext ctx)
        {
            if (ctx.Contract.Status != ContractStatus.Terminated || ctx.Request.NewStatus != ContractStatus.Active)
            {
                return StateTransitionValidationResult.Valid;
            }

            if (!ctx.IsAdmin)
            {
                return StateTransitionValidationResult.Forbidden(
                    "Chỉ Quản trị viên hệ thống (Admin) mới có thẩm quyền khôi phục hợp đồng đã chấm dứt.");
            }

            if (string.IsNullOrWhiteSpace(ctx.Request.Note) || ctx.Request.Note.Trim().Length < 20)
            {
                return StateTransitionValidationResult.Fail(
                    "Vui lòng nêu rõ lý do và căn cứ pháp lý khôi phục hiệu lực hợp đồng (tối thiểu 20 ký tự giải trình).");
            }

            var reactivateLog = $"[KHÔI PHỤC HĐ {ctx.UtcNow:dd/MM/yyyy HH:mm} UTC] Bởi Admin (ID: {ctx.UserId}) | Lý do: {ctx.Request.Note.Trim()}";
            ctx.Contract.Terms = string.IsNullOrWhiteSpace(ctx.Contract.Terms)
                ? reactivateLog
                : $"{ctx.Contract.Terms}\n\n{reactivateLog}";

            return StateTransitionValidationResult.Valid;
        }

        private static StateTransitionValidationResult ValidateCompletionRule(ContractTransitionContext ctx)
        {
            if (ctx.Request.NewStatus != ContractStatus.Completed)
            {
                return StateTransitionValidationResult.Valid;
            }

            var totalDisbursed = ctx.Contract.Milestones?
                .Where(m => m.Status == MilestoneStatus.Completed)
                .Sum(m => m.Amount) ?? 0;
            var allMilestonesCompleted = ctx.Contract.Milestones != null &&
                ctx.Contract.Milestones.Count > 0 &&
                ctx.Contract.Milestones.All(m => m.Status == MilestoneStatus.Completed);

            if (!allMilestonesCompleted || totalDisbursed < ctx.Contract.Value)
            {
                return StateTransitionValidationResult.Fail(
                    $"Không thể hoàn thành hợp đồng khi chưa nghiệm thu và giải ngân đủ 100{PercentSymbol} giá trị hợp đồng. " +
                    $"Hiện tại đã giải ngân: {totalDisbursed:N0} / {ctx.Contract.Value:N0} VNĐ.");
            }

            return StateTransitionValidationResult.Valid;
        }
    }
}
