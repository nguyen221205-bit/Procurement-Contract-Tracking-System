using ClosedXML.Excel;
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
            var awardedPackages = await packagesQuery.CountAsync(bp => bp.Status == BidPackageStatus.Awarded);
            var contractedPackages = await packagesQuery.CountAsync(bp => bp.Status == BidPackageStatus.Contracted);
            var totalEstimatedBudget = await packagesQuery.SumAsync(bp => (decimal?)bp.Budget) ?? 0;

            // 2. Thống kê hồ sơ & nhà thầu
            var submissionsQuery = _unitOfWork.Repository<BidSubmission>()
                .Query()
                .AsNoTracking();

            var totalSubmissions = await submissionsQuery.CountAsync();
            var evaluatedSubmissions = await submissionsQuery.CountAsync(s => s.Status == BidSubmissionStatus.Evaluated || s.Status == BidSubmissionStatus.Selected || s.Status == BidSubmissionStatus.Rejected);
            var selectedSubmissions = await submissionsQuery.CountAsync(s => s.Status == BidSubmissionStatus.Selected);

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
                AwardedPackages = awardedPackages,
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

        public async Task<byte[]> ExportContractorsCsvAsync()
        {
            var contractors = await _unitOfWork.Repository<Contractor>()
                .Query()
                .Include(c => c.User)
                .OrderBy(c => c.Id)
                .AsNoTracking()
                .ToListAsync();

            using var memoryStream = new MemoryStream();
            using (var writer = new StreamWriter(memoryStream, new System.Text.UTF8Encoding(true)))
            {
                // Tiêu đề cột
                await writer.WriteLineAsync("ID,Tên doanh nghiệp,Mã số thuế,Địa chỉ,Người đại diện,Email,Số điện thoại,Điểm uy tín (sao),Trạng thái thẩm định,Ngày tạo");

                foreach (var c in contractors)
                {
                    var line = string.Join(",",
                        EscapeCsv(c.Id),
                        EscapeCsv(c.CompanyName),
                        EscapeCsv(c.TaxCode),
                        EscapeCsv(c.Address),
                        EscapeCsv(c.User?.FullName),
                        EscapeCsv(c.User?.Email),
                        EscapeCsv(c.User?.Phone),
                        EscapeCsv(c.Rating),
                        EscapeCsv(c.VerificationStatus),
                        EscapeCsv(c.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"))
                    );
                    await writer.WriteLineAsync(line);
                }
            }

            return memoryStream.ToArray();
        }

        public async Task<byte[]> ExportContractsCsvAsync()
        {
            var contracts = await _unitOfWork.Repository<Contract>()
                .Query()
                .Include(c => c.BidPackage)
                .Include(c => c.Contractor)
                .OrderByDescending(c => c.CreatedAt)
                .AsNoTracking()
                .ToListAsync();

            using var memoryStream = new MemoryStream();
            using (var writer = new StreamWriter(memoryStream, new System.Text.UTF8Encoding(true)))
            {
                // Tiêu đề cột
                await writer.WriteLineAsync("ID,Số hợp đồng,Mã gói thầu,Tên gói thầu,Nhà thầu thi công,Mã số thuế,Giá trị hợp đồng (VNĐ),Ngân sách dự toán (VNĐ),Tiết kiệm (VNĐ),Ngày bắt đầu,Ngày kết thúc,Trạng thái,Ngày ký");

                foreach (var c in contracts)
                {
                    var budget = c.BidPackage?.Budget ?? 0;
                    var val = c.Value;
                    var savings = budget > val ? budget - val : 0;

                    var line = string.Join(",",
                        EscapeCsv(c.Id),
                        EscapeCsv(c.ContractNumber),
                        EscapeCsv(c.BidPackage?.Code),
                        EscapeCsv(c.BidPackage?.Name),
                        EscapeCsv(c.Contractor?.CompanyName),
                        EscapeCsv(c.Contractor?.TaxCode),
                        EscapeCsv(val.ToString("0.##")),
                        EscapeCsv(budget.ToString("0.##")),
                        EscapeCsv(savings.ToString("0.##")),
                        EscapeCsv(c.StartDate.ToString("yyyy-MM-dd")),
                        EscapeCsv(c.EndDate.ToString("yyyy-MM-dd")),
                        EscapeCsv(c.Status.ToString()),
                        EscapeCsv(c.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"))
                    );
                    await writer.WriteLineAsync(line);
                }
            }

            return memoryStream.ToArray();
        }

        public async Task<byte[]> ExportContractorsExcelAsync()
        {
            var contractors = await _unitOfWork.Repository<Contractor>()
                .Query()
                .Include(c => c.User)
                .OrderBy(c => c.Id)
                .AsNoTracking()
                .ToListAsync();

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Danh sách nhà thầu");

            // Tiêu đề các cột
            var headers = new[]
            {
                "ID", "Tên doanh nghiệp", "Mã số thuế", "Địa chỉ",
                "Người đại diện", "Email", "Số điện thoại", "Điểm uy tín (sao)",
                "Trạng thái thẩm định", "Ngày đăng ký"
            };

            for (int col = 0; col < headers.Length; col++)
            {
                var cell = worksheet.Cell(1, col + 1);
                cell.Value = headers[col];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1E40AF");
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }

            // Ghi dữ liệu từng dòng
            int row = 2;
            foreach (var c in contractors)
            {
                worksheet.Cell(row, 1).Value = c.Id;
                worksheet.Cell(row, 2).Value = c.CompanyName;
                worksheet.Cell(row, 3).Value = c.TaxCode ?? string.Empty;
                worksheet.Cell(row, 4).Value = c.Address ?? string.Empty;
                worksheet.Cell(row, 5).Value = c.User?.FullName ?? string.Empty;
                worksheet.Cell(row, 6).Value = c.User?.Email ?? string.Empty;
                worksheet.Cell(row, 7).Value = c.User?.Phone ?? string.Empty;
                worksheet.Cell(row, 8).Value = (double)c.Rating;
                worksheet.Cell(row, 9).Value = c.VerificationStatus;
                worksheet.Cell(row, 10).Value = c.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss");
                row++;
            }

            worksheet.Columns().AdjustToContents();

            using var memoryStream = new MemoryStream();
            workbook.SaveAs(memoryStream);
            return memoryStream.ToArray();
        }

        public async Task<byte[]> ExportContractsExcelAsync()
        {
            var contracts = await _unitOfWork.Repository<Contract>()
                .Query()
                .Include(c => c.BidPackage)
                .Include(c => c.Contractor)
                .OrderByDescending(c => c.CreatedAt)
                .AsNoTracking()
                .ToListAsync();

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Danh sách hợp đồng");

            var headers = new[]
            {
                "ID", "Số hợp đồng", "Mã gói thầu", "Tên gói thầu",
                "Nhà thầu thi công", "Mã số thuế", "Giá trị hợp đồng (VNĐ)",
                "Ngân sách dự toán (VNĐ)", "Tiết kiệm (VNĐ)", "Ngày bắt đầu",
                "Ngày kết thúc", "Trạng thái", "Ngày ký"
            };

            for (int col = 0; col < headers.Length; col++)
            {
                var cell = worksheet.Cell(1, col + 1);
                cell.Value = headers[col];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#166534");
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }

            int row = 2;
            foreach (var c in contracts)
            {
                var budget = c.BidPackage?.Budget ?? 0;
                var val = c.Value;
                var savings = budget > val ? budget - val : 0;

                worksheet.Cell(row, 1).Value = c.Id;
                worksheet.Cell(row, 2).Value = c.ContractNumber;
                worksheet.Cell(row, 3).Value = c.BidPackage?.Code ?? string.Empty;
                worksheet.Cell(row, 4).Value = c.BidPackage?.Name ?? string.Empty;
                worksheet.Cell(row, 5).Value = c.Contractor?.CompanyName ?? string.Empty;
                worksheet.Cell(row, 6).Value = c.Contractor?.TaxCode ?? string.Empty;
                worksheet.Cell(row, 7).Value = (double)val;
                worksheet.Cell(row, 8).Value = (double)budget;
                worksheet.Cell(row, 9).Value = (double)savings;
                worksheet.Cell(row, 10).Value = c.StartDate.ToString("yyyy-MM-dd");
                worksheet.Cell(row, 11).Value = c.EndDate.ToString("yyyy-MM-dd");
                worksheet.Cell(row, 12).Value = c.Status.ToString();
                worksheet.Cell(row, 13).Value = c.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss");
                row++;
            }

            worksheet.Columns().AdjustToContents();

            using var memoryStream = new MemoryStream();
            workbook.SaveAs(memoryStream);
            return memoryStream.ToArray();
        }

        public async Task<ApiResponse<ProcurementSummaryReportDto>> GetProcurementSummaryReportAsync(int? year = null, BidPackageType? type = null)
        {
            var query = _unitOfWork.Repository<BidPackage>()
                .Query()
                .Include(bp => bp.Contracts)
                .AsNoTracking();

            if (year.HasValue)
            {
                query = query.Where(bp => bp.CreatedAt.Year == year.Value);
            }

            if (type.HasValue)
            {
                query = query.Where(bp => bp.Type == type.Value);
            }

            var packages = await query.ToListAsync();

            var totalPackages = packages.Count;
            var totalBudget = packages.Sum(p => p.Budget);

            // Hợp đồng hợp lệ liên kết gói thầu
            var validContracts = packages
                .SelectMany(p => p.Contracts)
                .Where(c => c.Status != ContractStatus.Terminated)
                .ToList();

            var totalContractValue = validContracts.Sum(c => c.Value);
            var contractedPackagesBudget = packages
                .Where(p => p.Contracts.Any(c => c.Status != ContractStatus.Terminated))
                .Sum(p => p.Budget);

            var totalSavings = contractedPackagesBudget > totalContractValue ? (contractedPackagesBudget - totalContractValue) : 0m;
            var savingsRate = contractedPackagesBudget > 0 ? Math.Round((totalSavings / contractedPackagesBudget) * 100, 2) : 0m;

            // Phân bổ theo loại gói thầu (Type Breakdown)
            var allTypes = Enum.GetValues<BidPackageType>();
            var typeBreakdowns = allTypes.Select(t =>
            {
                var pkgsOfType = packages.Where(p => p.Type == t).ToList();
                var cnt = pkgsOfType.Count;
                var bgt = pkgsOfType.Sum(p => p.Budget);
                var cVal = pkgsOfType.SelectMany(p => p.Contracts).Where(c => c.Status != ContractStatus.Terminated).Sum(c => c.Value);
                var cBgt = pkgsOfType.Where(p => p.Contracts.Any(c => c.Status != ContractStatus.Terminated)).Sum(p => p.Budget);
                var sv = cBgt > cVal ? (cBgt - cVal) : 0m;
                var rate = cBgt > 0 ? Math.Round((sv / cBgt) * 100, 2) : 0m;

                return new PackageTypeStatDto
                {
                    Type = t,
                    TypeName = t switch
                    {
                        BidPackageType.Goods => "Hàng hóa",
                        BidPackageType.Construction => "Xây dựng",
                        BidPackageType.Service => "Dịch vụ",
                        _ => t.ToString()
                    },
                    PackageCount = cnt,
                    TotalBudget = bgt,
                    TotalContractValue = cVal,
                    TotalSavings = sv,
                    SavingsRate = rate
                };
            }).ToList();

            // Phân bổ theo trạng thái (Status Breakdown)
            var allStatuses = Enum.GetValues<BidPackageStatus>();
            var statusBreakdowns = allStatuses.Select(s =>
            {
                var cnt = packages.Count(p => p.Status == s);
                var pct = totalPackages > 0 ? Math.Round(((decimal)cnt / totalPackages) * 100, 2) : 0m;
                return new PackageStatusStatDto
                {
                    Status = s,
                    StatusName = s.ToString(),
                    PackageCount = cnt,
                    Percentage = pct
                };
            }).ToList();

            var report = new ProcurementSummaryReportDto
            {
                FilteredYear = year,
                FilteredType = type,
                FilteredTypeName = type?.ToString(),
                TotalPackages = totalPackages,
                TotalBudget = totalBudget,
                TotalContractValue = totalContractValue,
                TotalSavings = totalSavings,
                SavingsRate = savingsRate,
                TypeBreakdowns = typeBreakdowns,
                StatusBreakdowns = statusBreakdowns
            };

            return ApiResponse<ProcurementSummaryReportDto>.Ok(report);
        }

        public async Task<ApiResponse<ContractorPerformanceReportDto>> GetContractorPerformanceReportAsync()
        {
            var contractors = await _unitOfWork.Repository<Contractor>()
                .Query()
                .Include(c => c.BidSubmissions)
                .Include(c => c.Contracts)
                .AsNoTracking()
                .ToListAsync();

            var items = contractors.Select(c =>
            {
                var submissions = c.BidSubmissions.ToList();
                var totalSubmissions = submissions.Count;
                var wonSubmissions = submissions.Count(s => s.Status == BidSubmissionStatus.Selected);
                var lostSubmissions = submissions.Count(s => s.Status == BidSubmissionStatus.Rejected);
                var disqualifiedSubmissions = submissions.Count(s => s.Status == BidSubmissionStatus.Disqualified);
                var winRate = totalSubmissions > 0 ? Math.Round(((decimal)wonSubmissions / totalSubmissions) * 100, 2) : 0m;
                var totalAwardedValue = c.Contracts
                    .Where(ct => ct.Status != ContractStatus.Terminated)
                    .Sum(ct => ct.Value);

                return new ContractorPerformanceItemDto
                {
                    ContractorId = c.Id,
                    CompanyName = c.CompanyName,
                    TaxCode = c.TaxCode ?? string.Empty,
                    Rating = c.Rating,
                    VerificationStatus = c.VerificationStatus,
                    TotalSubmissions = totalSubmissions,
                    WonSubmissions = wonSubmissions,
                    LostSubmissions = lostSubmissions,
                    DisqualifiedSubmissions = disqualifiedSubmissions,
                    WinRate = winRate,
                    TotalAwardedValue = totalAwardedValue
                };
            })
            .OrderByDescending(x => x.TotalAwardedValue)
            .ThenByDescending(x => x.WinRate)
            .ToList();

            var result = new ContractorPerformanceReportDto
            {
                TotalContractors = contractors.Count,
                ActiveBiddingContractors = items.Count(x => x.TotalSubmissions > 0),
                Contractors = items
            };

            return ApiResponse<ContractorPerformanceReportDto>.Ok(result);
        }

        public async Task<ApiResponse<ContractProgressReportDto>> GetContractProgressReportAsync(int daysAhead = 30)
        {
            var now = DateTime.UtcNow;
            var targetFutureDate = now.AddDays(daysAhead);

            var milestones = await _unitOfWork.Repository<ContractMilestone>()
                .Query()
                .Include(m => m.Acceptances)
                .AsNoTracking()
                .ToListAsync();

            var totalMilestones = milestones.Count;
            var completedMilestones = milestones.Count(m => m.Status == MilestoneStatus.Completed);
            var pendingMilestones = milestones.Count(m => m.Status == MilestoneStatus.Pending || m.Status == MilestoneStatus.InProgress);

            // Tiến độ: Mốc hoàn thành đúng hạn (ApprovedAt <= DueDate)
            int onTimeMilestones = 0;
            int delayedMilestones = 0;

            foreach (var m in milestones)
            {
                if (m.Status == MilestoneStatus.Completed)
                {
                    var approvedAcceptance = m.Acceptances
                        .Where(a => a.Status == AcceptanceStatus.Approved && a.ApprovedAt.HasValue)
                        .OrderBy(a => a.ApprovedAt)
                        .FirstOrDefault();

                    if (approvedAcceptance != null && approvedAcceptance.ApprovedAt <= m.DueDate)
                    {
                        onTimeMilestones++;
                    }
                    else
                    {
                        delayedMilestones++;
                    }
                }
                else if (m.DueDate < now)
                {
                    // Đang Pending/InProgress nhưng đã quá hạn
                    delayedMilestones++;
                }
            }

            var evaluatedMilestones = onTimeMilestones + delayedMilestones;
            var onTimeRate = evaluatedMilestones > 0
                ? Math.Round(((decimal)onTimeMilestones / evaluatedMilestones) * 100, 2)
                : 100m;

            // Hợp đồng sắp hết hạn và quá hạn
            var activeContracts = await _unitOfWork.Repository<Contract>()
                .Query()
                .Include(c => c.BidPackage)
                .Include(c => c.Contractor)
                .Include(c => c.Milestones)
                .Where(c => c.Status == ContractStatus.Active)
                .AsNoTracking()
                .ToListAsync();

            var expiringContractItems = new List<ExpiringContractItemDto>();

            foreach (var c in activeContracts)
            {
                var isOverdue = c.EndDate < now;
                var isExpiringSoon = c.EndDate >= now && c.EndDate <= targetFutureDate;

                if (isOverdue || isExpiringSoon)
                {
                    var disbursed = c.Milestones
                        .Where(m => m.Status == MilestoneStatus.Completed)
                        .Sum(m => m.Amount);
                    var rate = c.Value > 0 ? Math.Round((disbursed / c.Value) * 100, 2) : 0m;
                    var daysRemaining = (int)Math.Ceiling((c.EndDate - now).TotalDays);

                    expiringContractItems.Add(new ExpiringContractItemDto
                    {
                        ContractId = c.Id,
                        ContractNumber = c.ContractNumber,
                        BidPackageCode = c.BidPackage?.Code ?? string.Empty,
                        BidPackageName = c.BidPackage?.Name ?? string.Empty,
                        ContractorName = c.Contractor?.CompanyName ?? string.Empty,
                        ContractValue = c.Value,
                        StartDate = c.StartDate,
                        EndDate = c.EndDate,
                        DaysRemaining = daysRemaining,
                        IsOverdue = isOverdue,
                        Status = c.Status.ToString(),
                        TotalDisbursedAmount = disbursed,
                        DisbursementRate = rate
                    });
                }
            }

            expiringContractItems = expiringContractItems
                .OrderBy(x => x.DaysRemaining)
                .ToList();

            var overdueCount = expiringContractItems.Count(x => x.IsOverdue);
            var expiringCount = expiringContractItems.Count(x => !x.IsOverdue);

            var report = new ContractProgressReportDto
            {
                TotalMilestones = totalMilestones,
                CompletedMilestones = completedMilestones,
                PendingMilestones = pendingMilestones,
                OnTimeMilestones = onTimeMilestones,
                DelayedMilestones = delayedMilestones,
                OnTimeRate = onTimeRate,
                ExpiringContractsCount = expiringCount,
                OverdueContractsCount = overdueCount,
                ExpiringContracts = expiringContractItems
            };

            return ApiResponse<ContractProgressReportDto>.Ok(report);
        }

        private static string EscapeCsv(object? value)
        {
            if (value == null) return string.Empty;
            var str = value.ToString() ?? string.Empty;

            // Chống CSV / Formula Injection (CWE-1236):
            // Các ô bắt đầu bằng =, +, -, @ hoặc tab/CR phải được tiền tố bằng dấu nháy đơn '
            if (str.Length > 0 && (str[0] == '=' || str[0] == '+' || str[0] == '-' || str[0] == '@' || str[0] == '\t'))
            {
                str = "'" + str;
            }

            if (str.Contains(',') || str.Contains('"') || str.Contains('\n') || str.Contains('\r'))
            {
                return $"\"{str.Replace("\"", "\"\"")}\"";
            }
            return str;
        }
    }
}
