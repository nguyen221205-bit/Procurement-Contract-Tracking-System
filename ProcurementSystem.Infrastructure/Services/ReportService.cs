using Microsoft.EntityFrameworkCore;
using ProcurementSystem.Core.DTOs;
using ProcurementSystem.Core.DTOs.Report;
using ProcurementSystem.Core.Enums;
using ProcurementSystem.Core.Interfaces;
using ProcurementSystem.Infrastructure.Entities;

namespace ProcurementSystem.Infrastructure.Services
{
    public class ReportService : IReportService
    {
        private readonly IUnitOfWork _unitOfWork;

        public ReportService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ApiResponse<ProcurementDashboardDto>> GetDashboardMetricsAsync()
        {
            // 1. Thống kê gói thầu
            var packagesQuery = _unitOfWork.Repository<BidPackage>()
                .Query()
                .AsNoTracking();

            var totalPackages = await packagesQuery.CountAsync();
            var openPackages = await packagesQuery.CountAsync(bp => bp.Status == BidPackageStatus.Open);
            var closedPackages = await packagesQuery.CountAsync(bp => bp.Status == BidPackageStatus.Closed);
            var evaluatingPackages = await packagesQuery.CountAsync(bp => bp.Status == BidPackageStatus.Evaluating);
            var contractedPackages = await packagesQuery.CountAsync(bp => bp.Status == BidPackageStatus.Contracted);
            var totalEstimatedBudget = await packagesQuery.SumAsync(bp => (decimal?)bp.Budget) ?? 0;

            // 2. Thống kê hồ sơ & nhà thầu
            var submissionsQuery = _unitOfWork.Repository<BidSubmission>()
                .Query()
                .AsNoTracking();

            var totalSubmissions = await submissionsQuery.CountAsync();
            var evaluatedSubmissions = await submissionsQuery.CountAsync(s => s.Status == "Evaluated" || s.Status == "Selected" || s.Status == "Rejected");
            var selectedSubmissions = await submissionsQuery.CountAsync(s => s.Status == "Selected");

            var totalContractors = await _unitOfWork.Repository<Contractor>()
                .Query()
                .AsNoTracking()
                .CountAsync();

            // 3. Thống kê hợp đồng
            var contractsQuery = _unitOfWork.Repository<Contract>()
                .Query()
                .Include(c => c.BidPackage)
                .AsNoTracking();

            var totalContracts = await contractsQuery.CountAsync();
            var draftContracts = await contractsQuery.CountAsync(c => c.Status == ContractStatus.Draft);
            var activeContracts = await contractsQuery.CountAsync(c => c.Status == ContractStatus.Active);
            var completedContracts = await contractsQuery.CountAsync(c => c.Status == ContractStatus.Completed);
            var terminatedContracts = await contractsQuery.CountAsync(c => c.Status == ContractStatus.Terminated);

            var validContracts = contractsQuery.Where(c => c.Status != ContractStatus.Terminated);
            var totalContractValue = await validContracts.SumAsync(c => (decimal?)c.Value) ?? 0;

            // Tính toán số tiền tiết kiệm qua đấu thầu (chỉ tính các gói đã có hợp đồng)
            var contractedPackagesBudget = await validContracts.SumAsync(c => (decimal?)c.BidPackage.Budget) ?? 0;
            var totalSavings = contractedPackagesBudget > totalContractValue ? (contractedPackagesBudget - totalContractValue) : 0;
            var savingsRate = contractedPackagesBudget > 0 ? Math.Round((totalSavings / contractedPackagesBudget) * 100, 2) : 0;

            // 4. Thống kê mốc thanh toán nghiệm thu (Milestones)
            var milestonesQuery = _unitOfWork.Repository<ContractMilestone>()
                .Query()
                .AsNoTracking();

            var totalMilestones = await milestonesQuery.CountAsync();
            var completedMilestones = await milestonesQuery.CountAsync(m => m.Status == MilestoneStatus.Completed);
            var pendingMilestones = await milestonesQuery.CountAsync(m => m.Status == MilestoneStatus.Pending || m.Status == MilestoneStatus.InProgress);

            var totalDisbursedAmount = await milestonesQuery
                .Where(m => m.Status == MilestoneStatus.Completed)
                .SumAsync(m => (decimal?)m.Amount) ?? 0;

            var totalRemainingAmount = totalContractValue > totalDisbursedAmount ? (totalContractValue - totalDisbursedAmount) : 0;
            var disbursementRate = totalContractValue > 0 ? Math.Round((totalDisbursedAmount / totalContractValue) * 100, 2) : 0;

            // 5. Danh sách hợp đồng gần đây (Top 5)
            var recentContracts = await _unitOfWork.Repository<Contract>()
                .Query()
                .Include(c => c.BidPackage)
                .Include(c => c.Contractor)
                .OrderByDescending(c => c.CreatedAt)
                .Take(5)
                .AsNoTracking()
                .Select(c => new RecentContractItemDto
                {
                    ContractId = c.Id,
                    ContractNumber = c.ContractNumber,
                    BidPackageCode = c.BidPackage.Code,
                    BidPackageName = c.BidPackage.Name,
                    CompanyName = c.Contractor.CompanyName,
                    PackageBudget = c.BidPackage.Budget,
                    ContractValue = c.Value,
                    Savings = c.BidPackage.Budget > c.Value ? c.BidPackage.Budget - c.Value : 0,
                    Status = c.Status.ToString(),
                    StartDate = c.StartDate,
                    EndDate = c.EndDate
                })
                .ToListAsync();

            var dashboardDto = new ProcurementDashboardDto
            {
                TotalPackages = totalPackages,
                OpenPackages = openPackages,
                ClosedPackages = closedPackages,
                EvaluatingPackages = evaluatingPackages,
                ContractedPackages = contractedPackages,

                TotalSubmissions = totalSubmissions,
                EvaluatedSubmissions = evaluatedSubmissions,
                SelectedSubmissions = selectedSubmissions,
                TotalContractors = totalContractors,

                TotalEstimatedBudget = totalEstimatedBudget,
                TotalContractValue = totalContractValue,
                TotalSavings = totalSavings,
                SavingsRate = savingsRate,

                TotalContracts = totalContracts,
                DraftContracts = draftContracts,
                ActiveContracts = activeContracts,
                CompletedContracts = completedContracts,
                TerminatedContracts = terminatedContracts,

                TotalMilestones = totalMilestones,
                CompletedMilestones = completedMilestones,
                PendingMilestones = pendingMilestones,
                TotalDisbursedAmount = totalDisbursedAmount,
                TotalRemainingAmount = totalRemainingAmount,
                DisbursementRate = disbursementRate,

                RecentContracts = recentContracts
            };

            return ApiResponse<ProcurementDashboardDto>.Ok(dashboardDto, "Lấy dữ liệu thống kê tổng hợp (Dashboard) thành công.");
        }
    }
}
