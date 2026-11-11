using System.Security.Claims;
using System.Text;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using ProcurementSystem.API.Controllers;
using ProcurementSystem.Core.DTOs;
using ProcurementSystem.Core.DTOs.Report;
using ProcurementSystem.Core.Enums;
using ProcurementSystem.Core.Interfaces;
using ProcurementSystem.Infrastructure.Data;
using ProcurementSystem.Infrastructure.Entities;
using ProcurementSystem.Infrastructure.Repositories;
using ProcurementSystem.Infrastructure.Services;
using Xunit;

namespace ProcurementSystem.Tests;

/// <summary>
/// Kiểm thử cô lập dữ liệu đa đơn vị (Multi-Entity Data Isolation) cho phân hệ Báo cáo &amp; Thống kê (Agent B).
/// </summary>
public class ReportDataIsolationTests : IAsyncLifetime
{
    private SqliteConnection _connection = null!;
    private DbContextOptions<AppDbContext> _dbOptions = null!;

    private User _adminUser = null!;
    private User _procUserA = null!;
    private User _procUserB = null!;
    private User _unassignedUser = null!;
    private User _revokedUserA = null!;

    private Contractor _contractorA = null!;
    private Contractor _contractorB = null!;

    public async Task InitializeAsync()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        await _connection.OpenAsync();

        _dbOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        await using (var initDb = new TestDbContext(_dbOptions))
        {
            await initDb.Database.EnsureCreatedAsync();
        }

        await SeedAsync();
    }

    public async Task DisposeAsync()
    {
        await _connection.DisposeAsync();
    }

    private TestDbContext CreateDb() => new(_dbOptions);

    private ReportService CreateService(TestDbContext db) => new(new UnitOfWork(db));

    private async Task SeedAsync()
    {
        await using var db = CreateDb();

        var adminRole = new Role { Name = "Admin", Description = "Quản trị hệ thống" };
        var procRole = new Role { Name = "Procurement", Description = "Bên mời thầu" };
        var contractorRole = new Role { Name = "Contractor", Description = "Nhà thầu" };
        db.Roles.AddRange(adminRole, procRole, contractorRole);
        await db.SaveChangesAsync();

        _adminUser = new User { FullName = "Quản trị viên", Email = "admin@iso.test", PasswordHash = "hash", IsActive = true };
        _procUserA = new User { FullName = "Cán bộ Đơn vị A", Email = "proc.a@iso.test", PasswordHash = "hash", IsActive = true };
        _procUserB = new User { FullName = "Cán bộ Đơn vị B", Email = "proc.b@iso.test", PasswordHash = "hash", IsActive = true };
        _unassignedUser = new User { FullName = "Tài khoản tự do", Email = "unassigned@iso.test", PasswordHash = "hash", IsActive = true };
        _revokedUserA = new User { FullName = "Cán bộ đã thu hồi quyền", Email = "revoked.a@iso.test", PasswordHash = "hash", IsActive = true };
        var contUserA = new User { FullName = "Đại diện Nhà thầu A", Email = "cont.a@iso.test", PasswordHash = "hash", IsActive = true };
        var contUserB = new User { FullName = "Đại diện Nhà thầu B", Email = "cont.b@iso.test", PasswordHash = "hash", IsActive = true };

        db.Users.AddRange(_adminUser, _procUserA, _procUserB, _unassignedUser, _revokedUserA, contUserA, contUserB);
        await db.SaveChangesAsync();

        db.UserRoles.AddRange(
            new UserRole { UserId = _adminUser.Id, RoleId = adminRole.Id },
            new UserRole { UserId = _procUserA.Id, RoleId = procRole.Id },
            new UserRole { UserId = _procUserB.Id, RoleId = procRole.Id },
            new UserRole { UserId = _unassignedUser.Id, RoleId = procRole.Id },
            new UserRole { UserId = _revokedUserA.Id, RoleId = procRole.Id },
            new UserRole { UserId = contUserA.Id, RoleId = contractorRole.Id },
            new UserRole { UserId = contUserB.Id, RoleId = contractorRole.Id }
        );
        await db.SaveChangesAsync();

        var entityA = new ProcuringEntity
        {
            UserId = _procUserA.Id,
            OrganizationName = "Ban Quản lý Dự án Đơn vị A",
            OrganizationType = "BanQLDA",
            TaxCode = "0100000001",
            RepresentativeName = "Nguyễn Văn A",
            RepresentativeTitle = "Giám đốc",
            EstablishmentDecisionFile = "pe_a.pdf",
            VerificationStatus = "Approved"
        };
        var entityB = new ProcuringEntity
        {
            UserId = _procUserB.Id,
            OrganizationName = "Bệnh viện Đa khoa Đơn vị B",
            OrganizationType = "DonViSuNghiep",
            TaxCode = "0200000002",
            RepresentativeName = "Trần Thị B",
            RepresentativeTitle = "Giám đốc",
            EstablishmentDecisionFile = "pe_b.pdf",
            VerificationStatus = "Approved"
        };
        db.ProcuringEntities.AddRange(entityA, entityB);
        await db.SaveChangesAsync();

        db.ProcuringEntityMembers.AddRange(
            new ProcuringEntityMember
            {
                ProcuringEntityId = entityA.Id,
                UserId = _procUserA.Id,
                RoleInEntity = ProcuringEntityMemberRole.Manager,
                Status = MemberStatus.Active
            },
            new ProcuringEntityMember
            {
                ProcuringEntityId = entityA.Id,
                UserId = _revokedUserA.Id,
                RoleInEntity = ProcuringEntityMemberRole.Officer,
                Status = MemberStatus.Revoked,
                RevokedAt = DateTime.UtcNow.AddDays(-1)
            },
            new ProcuringEntityMember
            {
                ProcuringEntityId = entityB.Id,
                UserId = _procUserB.Id,
                RoleInEntity = ProcuringEntityMemberRole.Manager,
                Status = MemberStatus.Active
            }
        );

        _contractorA = new Contractor
        {
            UserId = contUserA.Id,
            CompanyName = "Công ty Cổ phần Công nghệ Alpha",
            TaxCode = "1111111111",
            VerificationStatus = "Approved"
        };
        _contractorB = new Contractor
        {
            UserId = contUserB.Id,
            CompanyName = "Tổng Công ty Xây dựng Beta",
            TaxCode = "2222222222",
            VerificationStatus = "Approved"
        };
        db.Contractors.AddRange(_contractorA, _contractorB);
        await db.SaveChangesAsync();

        var pkgA1 = new BidPackage
        {
            Code = "PKG-A-001",
            Name = "Gói thầu Thiết bị Đơn vị A",
            Type = BidPackageType.Goods,
            Budget = 1_000_000_000m,
            Deadline = DateTime.UtcNow.AddDays(-10),
            Status = BidPackageStatus.Contracted,
            CreatedBy = _procUserA.Id,
            ProcuringEntityId = entityA.Id,
            CreatedAt = new DateTime(2026, 5, 10, 0, 0, 0, DateTimeKind.Utc)
        };
        var pkgA2 = new BidPackage
        {
            Code = "PKG-A-002",
            Name = "Gói thầu Tư vấn Đơn vị A",
            Type = BidPackageType.Service,
            Budget = 500_000_000m,
            Deadline = DateTime.UtcNow.AddDays(15),
            Status = BidPackageStatus.Open,
            CreatedBy = _procUserA.Id,
            ProcuringEntityId = entityA.Id,
            CreatedAt = new DateTime(2026, 6, 10, 0, 0, 0, DateTimeKind.Utc)
        };
        var pkgB1 = new BidPackage
        {
            Code = "PKG-B-999",
            Name = "Gói thầu Xây lắp Bệnh viện Đơn vị B",
            Type = BidPackageType.Construction,
            Budget = 3_000_000_000m,
            Deadline = DateTime.UtcNow.AddDays(-10),
            Status = BidPackageStatus.Contracted,
            CreatedBy = _procUserB.Id,
            ProcuringEntityId = entityB.Id,
            CreatedAt = new DateTime(2026, 5, 12, 0, 0, 0, DateTimeKind.Utc)
        };
        db.BidPackages.AddRange(pkgA1, pkgA2, pkgB1);
        await db.SaveChangesAsync();

        db.BidSubmissions.AddRange(
            new BidSubmission
            {
                BidPackageId = pkgA1.Id,
                ContractorId = _contractorA.Id,
                BidPrice = 800_000_000m,
                Status = BidSubmissionStatus.Selected
            },
            new BidSubmission
            {
                BidPackageId = pkgB1.Id,
                ContractorId = _contractorB.Id,
                BidPrice = 2_500_000_000m,
                Status = BidSubmissionStatus.Selected
            }
        );

        var contractA = new Contract
        {
            BidPackageId = pkgA1.Id,
            ContractorId = _contractorA.Id,
            ContractNumber = "HD-UNIT-A-001",
            Value = 800_000_000m,
            StartDate = DateTime.UtcNow.AddDays(-20),
            EndDate = DateTime.UtcNow.AddDays(10),
            Status = ContractStatus.Active
        };
        var contractB = new Contract
        {
            BidPackageId = pkgB1.Id,
            ContractorId = _contractorB.Id,
            ContractNumber = "HD-UNIT-B-999",
            Value = 2_500_000_000m,
            StartDate = DateTime.UtcNow.AddDays(-30),
            EndDate = DateTime.UtcNow.AddDays(5),
            Status = ContractStatus.Active
        };
        db.Contracts.AddRange(contractA, contractB);
        await db.SaveChangesAsync();

        var milestoneA = new ContractMilestone
        {
            ContractId = contractA.Id,
            Title = "Nghiệm thu Đợt 1 Đơn vị A",
            DueDate = DateTime.UtcNow.AddDays(5),
            Amount = 300_000_000m,
            Status = MilestoneStatus.Completed
        };
        var milestoneB = new ContractMilestone
        {
            ContractId = contractB.Id,
            Title = "Nghiệm thu Đợt 1 Đơn vị B",
            DueDate = DateTime.UtcNow.AddDays(-5),
            Amount = 1_200_000_000m,
            Status = MilestoneStatus.Completed
        };
        db.ContractMilestones.AddRange(milestoneA, milestoneB);
        await db.SaveChangesAsync();

        db.Acceptances.AddRange(
            new Acceptance
            {
                ContractId = contractA.Id,
                MilestoneId = milestoneA.Id,
                Status = AcceptanceStatus.Approved,
                ApprovedBy = _procUserA.Id,
                ApprovedAt = DateTime.UtcNow.AddDays(-1)
            },
            new Acceptance
            {
                ContractId = contractB.Id,
                MilestoneId = milestoneB.Id,
                Status = AcceptanceStatus.Approved,
                ApprovedBy = _procUserB.Id,
                ApprovedAt = DateTime.UtcNow.AddDays(-1)
            }
        );
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task Admin_GetDashboardAndReports_ReceivesAllEntitiesConsolidatedData()
    {
        await using var db = CreateDb();
        var service = CreateService(db);

        var dash = await service.GetDashboardMetricsAsync(_adminUser.Id, isAdmin: true);
        Assert.True(dash.Success);
        Assert.NotNull(dash.Data);
        Assert.Equal(3, dash.Data.TotalPackages);
        Assert.Equal(4_500_000_000m, dash.Data.TotalEstimatedBudget);
        Assert.Equal(2, dash.Data.TotalSubmissions);
        Assert.Equal(2, dash.Data.TotalContractors);
        Assert.Equal(2, dash.Data.TotalContracts);
        Assert.Equal(3_300_000_000m, dash.Data.TotalContractValue);
        Assert.Equal(1_500_000_000m, dash.Data.TotalDisbursedAmount);
        Assert.Contains(dash.Data.RecentContracts, c => c.ContractNumber == "HD-UNIT-A-001");
        Assert.Contains(dash.Data.RecentContracts, c => c.ContractNumber == "HD-UNIT-B-999");

        var summary = await service.GetProcurementSummaryReportAsync(2026, null, _adminUser.Id, isAdmin: true);
        Assert.True(summary.Success);
        Assert.Equal(3, summary.Data!.TotalPackages);
        Assert.Equal(4_500_000_000m, summary.Data.TotalBudget);

        var progress = await service.GetContractProgressReportAsync(30, _adminUser.Id, isAdmin: true);
        Assert.True(progress.Success);
        Assert.Equal(2, progress.Data!.TotalMilestones);
        Assert.Equal(2, progress.Data.ExpiringContractsCount);
    }

    [Fact]
    public async Task ProcurementUserA_GetDashboardAndReports_OnlyReceivesEntityAData()
    {
        await using var db = CreateDb();
        var service = CreateService(db);

        var dash = await service.GetDashboardMetricsAsync(_procUserA.Id, isAdmin: false);
        Assert.True(dash.Success);
        Assert.NotNull(dash.Data);
        Assert.Equal(2, dash.Data.TotalPackages);
        Assert.Equal(1_500_000_000m, dash.Data.TotalEstimatedBudget);
        Assert.Equal(1, dash.Data.TotalSubmissions);
        Assert.Equal(1, dash.Data.TotalContractors);
        Assert.Equal(1, dash.Data.TotalContracts);
        Assert.Equal(800_000_000m, dash.Data.TotalContractValue);
        Assert.Equal(200_000_000m, dash.Data.TotalSavings);
        Assert.Equal(1, dash.Data.TotalMilestones);
        Assert.Equal(300_000_000m, dash.Data.TotalDisbursedAmount);

        var recent = Assert.Single(dash.Data.RecentContracts);
        Assert.Equal("HD-UNIT-A-001", recent.ContractNumber);
        Assert.DoesNotContain(dash.Data.RecentContracts, c => c.ContractNumber == "HD-UNIT-B-999");

        var summary = await service.GetProcurementSummaryReportAsync(2026, null, _procUserA.Id, isAdmin: false);
        Assert.True(summary.Success);
        Assert.Equal(2, summary.Data!.TotalPackages);
        Assert.Equal(1_500_000_000m, summary.Data.TotalBudget);
        Assert.Equal(800_000_000m, summary.Data.TotalContractValue);

        var progress = await service.GetContractProgressReportAsync(30, _procUserA.Id, isAdmin: false);
        Assert.True(progress.Success);
        Assert.Equal(1, progress.Data!.TotalMilestones);
        Assert.Equal(1, progress.Data.OnTimeMilestones);
        Assert.Equal(0, progress.Data.DelayedMilestones);
        var expiring = Assert.Single(progress.Data.ExpiringContracts);
        Assert.Equal("HD-UNIT-A-001", expiring.ContractNumber);
        Assert.DoesNotContain(progress.Data.ExpiringContracts, c => c.ContractNumber == "HD-UNIT-B-999");
    }

    [Fact]
    public async Task ProcurementUserA_ExportContractsExcelAndCsv_ExcludesEntityBContracts()
    {
        await using var db = CreateDb();
        var service = CreateService(db);

        var excelBytes = await service.ExportContractsExcelAsync(_procUserA.Id, isAdmin: false);
        using var stream = new MemoryStream(excelBytes);
        using var workbook = new XLWorkbook(stream);
        var ws = workbook.Worksheet("Danh sách hợp đồng");

        Assert.Equal(2, ws.LastRowUsed()!.RowNumber());
        Assert.Equal("HD-UNIT-A-001", ws.Cell(2, 2).GetString());
        Assert.Equal("PKG-A-001", ws.Cell(2, 3).GetString());
        Assert.Equal(_contractorA.CompanyName, ws.Cell(2, 5).GetString());

        var allTextInExcel = string.Join(" | ", ws.CellsUsed().Select(c => c.GetString()));
        Assert.DoesNotContain("HD-UNIT-B-999", allTextInExcel);
        Assert.DoesNotContain("PKG-B-999", allTextInExcel);
        Assert.DoesNotContain(_contractorB.CompanyName, allTextInExcel);

        var csvBytes = await service.ExportContractsCsvAsync(_procUserA.Id, isAdmin: false);
        var csvText = Encoding.UTF8.GetString(csvBytes);
        Assert.Contains("HD-UNIT-A-001", csvText);
        Assert.DoesNotContain("HD-UNIT-B-999", csvText);
        Assert.DoesNotContain(_contractorB.CompanyName, csvText);
    }

    [Fact]
    public async Task UserWithoutActiveEntity_GetsEmptyReportDataAndZeroFinancials()
    {
        await using var db = CreateDb();
        var service = CreateService(db);

        foreach (var userId in new[] { _unassignedUser.Id, _revokedUserA.Id })
        {
            var dash = await service.GetDashboardMetricsAsync(userId, isAdmin: false);
            Assert.True(dash.Success);
            Assert.NotNull(dash.Data);
            Assert.Equal(0, dash.Data.TotalPackages);
            Assert.Equal(0m, dash.Data.TotalEstimatedBudget);
            Assert.Equal(0, dash.Data.TotalContracts);
            Assert.Equal(0m, dash.Data.TotalContractValue);
            Assert.Equal(0m, dash.Data.TotalDisbursedAmount);
            Assert.Equal(0, dash.Data.TotalContractors);
            Assert.Empty(dash.Data.RecentContracts);

            var summary = await service.GetProcurementSummaryReportAsync(2026, null, userId, isAdmin: false);
            Assert.Equal(0, summary.Data!.TotalPackages);
            Assert.Equal(0m, summary.Data.TotalBudget);
            Assert.Equal(0m, summary.Data.TotalContractValue);

            var progress = await service.GetContractProgressReportAsync(30, userId, isAdmin: false);
            Assert.Equal(0, progress.Data!.TotalMilestones);
            Assert.Empty(progress.Data.ExpiringContracts);

            var excelBytes = await service.ExportContractsExcelAsync(userId, isAdmin: false);
            using var ms = new MemoryStream(excelBytes);
            using var wb = new XLWorkbook(ms);
            var ws = wb.Worksheet("Danh sách hợp đồng");
            Assert.Equal(1, ws.LastRowUsed()!.RowNumber());

            var csvText = Encoding.UTF8.GetString(await service.ExportContractsCsvAsync(userId, isAdmin: false));
            Assert.DoesNotContain("HD-UNIT-A-001", csvText);
            Assert.DoesNotContain("HD-UNIT-B-999", csvText);
        }
    }

    [Fact]
    public async Task ReportsController_ForwardsUserIdAndAdminFlagFromClaims_AndRejectsMissingClaim()
    {
        var mockService = new Mock<IReportService>();
        mockService.Setup(s => s.GetDashboardMetricsAsync(42, false))
            .ReturnsAsync(ApiResponse<ProcurementDashboardDto>.Ok(new ProcurementDashboardDto()));
        mockService.Setup(s => s.ExportContractsExcelAsync(42, false))
            .ReturnsAsync(new byte[] { 1, 2, 3 });
        mockService.Setup(s => s.ExportContractsCsvAsync(42, false))
            .ReturnsAsync(new byte[] { 4, 5 });
        mockService.Setup(s => s.GetProcurementSummaryReportAsync(2026, null, 7, true))
            .ReturnsAsync(ApiResponse<ProcurementSummaryReportDto>.Ok(new ProcurementSummaryReportDto()));
        mockService.Setup(s => s.GetContractProgressReportAsync(30, 7, true))
            .ReturnsAsync(ApiResponse<ContractProgressReportDto>.Ok(new ContractProgressReportDto()));

        var controller = new ReportsController(mockService.Object);

        // 1. Claims rỗng -> 401 Unauthorized
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        var unauthRes = await controller.GetDashboard();
        Assert.IsType<UnauthorizedObjectResult>(unauthRes.Result);

        // 2. Tài khoản Procurement (#42) -> isAdmin = false
        controller.ControllerContext = BuildHttpContextWithUser(42, "Procurement");
        await controller.GetDashboard();
        await controller.ExportContractsExcel();
        await controller.ExportContractsCsv();
        mockService.Verify(s => s.GetDashboardMetricsAsync(42, false), Times.Once);
        mockService.Verify(s => s.ExportContractsExcelAsync(42, false), Times.Once);
        mockService.Verify(s => s.ExportContractsCsvAsync(42, false), Times.Once);

        // 3. Tài khoản Admin (#7) -> isAdmin = true
        controller.ControllerContext = BuildHttpContextWithUser(7, "Admin");
        await controller.GetProcurementSummary(2026, null);
        await controller.GetContractsProgress(30);
        mockService.Verify(s => s.GetProcurementSummaryReportAsync(2026, null, 7, true), Times.Once);
        mockService.Verify(s => s.GetContractProgressReportAsync(30, 7, true), Times.Once);
    }

    private static ControllerContext BuildHttpContextWithUser(int userId, string role)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, role)
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        return new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };
    }
}

