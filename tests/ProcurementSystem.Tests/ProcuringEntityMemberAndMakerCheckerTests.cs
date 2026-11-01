using ProcurementSystem.Core.DTOs.BidPackage;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;

using Microsoft.EntityFrameworkCore;

using Microsoft.Extensions.Configuration;

using Microsoft.Extensions.Logging.Abstractions;

using ProcurementSystem.Core.DTOs;

using ProcurementSystem.Core.DTOs.Contract;
using ProcurementSystem.Core.DTOs.Contractor;

using ProcurementSystem.Core.DTOs.Notification;

using ProcurementSystem.Core.DTOs.ProcuringEntity;

using ProcurementSystem.Core.DTOs.SigningAuthority;

using ProcurementSystem.Core.Enums;

using ProcurementSystem.Core.Interfaces;

using ProcurementSystem.Infrastructure.Data;

using ProcurementSystem.Infrastructure.Entities;

using ProcurementSystem.Infrastructure.Repositories;

using ProcurementSystem.Infrastructure.Services;

using Xunit;

namespace ProcurementSystem.Tests;

public class ProcuringEntityMemberAndMakerCheckerTests

{

    [Theory]
    [InlineData(MemberStatus.Revoked)]
    [InlineData(MemberStatus.Suspended)]
    public async Task R5_RevokedCreator_CannotEditOrUpload_AndKeepsTechnicalReview(MemberStatus status)
    {
        await using var s = await TestSetup.CreateAsync();
        var package = await s.Db.BidPackages.FindAsync(s.ActiveContract.BidPackageId);
        package!.CreatedBy = s.OfficerUser.Id;
        await s.Db.SaveChangesAsync();
        var review = await s.ContractService.TechnicalReviewMilestoneAsync(s.Milestone.Id,
            new() { IsPassed = true }, s.OfficerUser.Id, false);
        Assert.True(review.Success);
        var member = await s.Db.ProcuringEntityMembers.SingleAsync(m => m.UserId == s.OfficerUser.Id);
        member.Status = status;
        await s.Db.SaveChangesAsync();
        var originalTitle = s.Milestone.Title;
        var edit = await s.ContractService.UpdateMilestoneAsync(s.ActiveContract.Id, s.Milestone.Id,
            new() { Title = "Unauthorized edit", Amount = s.Milestone.Amount, DueDate = s.Milestone.DueDate }, s.OfficerUser.Id, false);
        Assert.Equal(403, edit.StatusCode);
        using var bytes = new MemoryStream(new byte[] { 1, 2, 3 });
        var upload = await s.ContractService.UploadScannedContractAsync(s.ActiveContract.Id,
            new FormFile(bytes, 0, bytes.Length, "file", "denied.pdf"), s.OfficerUser.Id, false);
        Assert.Equal(403, upload.StatusCode);
        Assert.Equal(originalTitle, (await s.Db.ContractMilestones.AsNoTracking().SingleAsync(m => m.Id == s.Milestone.Id)).Title);
        Assert.Equal(AcceptanceStatus.TechnicalApproved, (await s.Db.Acceptances.AsNoTracking().SingleAsync(a => a.Id == review.Data!.Id)).Status);
    }

    [Fact]
    public async Task R5_CreatePackage_RequiresMembership_AndOptionsAreScoped()
    {
        await using var s = await TestSetup.CreateAsync();
        s.Entity.VerificationStatus = "Approved";
        await s.Db.SaveChangesAsync();
        var service = new BidPackageService(new UnitOfWork(s.Db), new FileStorageService(), null!, s.NotificationService);
        var options = await service.GetCreatableEntitiesAsync(s.ManagerUser.Id);
        Assert.Equal(s.Entity.Id, Assert.Single(options.Data!).Id);
        Assert.Empty((await service.GetCreatableEntitiesAsync(s.SignerUser.Id)).Data!);
        var member = await s.Db.ProcuringEntityMembers.SingleAsync(m => m.UserId == s.ManagerUser.Id);
        s.Db.ProcuringEntityMembers.Remove(member);
        await s.Db.SaveChangesAsync();
        var request = new CreateBidPackageRequest { Name = "Owner without membership", Code = "R5-NO-MEMBER", Budget = 1000000, Deadline = DateTime.UtcNow.AddDays(20) };
        Assert.Equal(403, (await service.CreateBidPackageAsync(request, s.ManagerUser.Id)).StatusCode);
        Assert.Empty((await service.GetCreatableEntitiesAsync(s.ManagerUser.Id)).Data!);
        s.Db.ProcuringEntityMembers.Add(new() { UserId = s.ManagerUser.Id, ProcuringEntityId = s.Entity.Id,
            Status = MemberStatus.Active, RoleInEntity = ProcuringEntityMemberRole.Manager });
        await s.Db.SaveChangesAsync();
        Assert.True((await service.CreateBidPackageAsync(request, s.ManagerUser.Id)).Success);
    }

    [Fact]
    public async Task R5_AuthorizationTouch_DoesNotOverwriteRelatedUserCredentials()
    {
        await using var s = await TestSetup.CreateAsync();
        var package = await s.Db.BidPackages.FindAsync(s.ActiveContract.BidPackageId);
        package!.CreatedBy = s.OfficerUser.Id;
        await s.Db.SaveChangesAsync();
        // The request still tracks the previous password; another request rotated it.
        await s.Db.Database.ExecuteSqlInterpolatedAsync($"UPDATE Users SET PasswordHash = {"rotated-in-another-request"} WHERE Id = {s.OfficerUser.Id}");
        var result = await s.ContractService.UpdateMilestoneAsync(s.ActiveContract.Id, s.Milestone.Id,
            new() { Title = "Authorized update", Amount = s.Milestone.Amount, DueDate = s.Milestone.DueDate }, s.OfficerUser.Id, false);
        Assert.True(result.Success);
        Assert.Equal("rotated-in-another-request", await s.Db.Users.AsNoTracking().Where(u => u.Id == s.OfficerUser.Id).Select(u => u.PasswordHash).SingleAsync());
    }

    private sealed class TestSetup : IAsyncDisposable

    {

        public SqliteConnection Connection { get; }

        public AppDbContext Db { get; }

        public FakeEmailService EmailService { get; } = new();

        public FakeNotificationService NotificationService { get; } = new();

        public IConfiguration Configuration { get; }

        public ProcuringEntityMemberService MemberService { get; }

        public ContractService ContractService { get; }

        public SigningAuthorityService SigningAuthorityService { get; }

        public User ManagerUser { get; private set; } = null!;

        public User OfficerUser { get; private set; } = null!;

        public User SignerUser { get; private set; } = null!;

        public User ContractorUser { get; private set; } = null!;

        public Role ProcurementRole { get; private set; } = null!;

        public ProcuringEntity Entity { get; private set; } = null!;

        public Contract ActiveContract { get; private set; } = null!;

        public ContractMilestone Milestone { get; private set; } = null!;

        private TestSetup(SqliteConnection connection, AppDbContext db)

        {

            Connection = connection;

            Db = db;

            Configuration = new ConfigurationBuilder()

                .AddInMemoryCollection(new Dictionary<string, string?>

                {

                    ["FrontendUrl"] = "http://localhost:5173"

                })

                .Build();

            MemberService = new ProcuringEntityMemberService(

                Db,

                EmailService,

                Configuration,

                NullLogger<ProcuringEntityMemberService>.Instance);

            ContractService = new ContractService(

                new UnitOfWork(Db),

                new FileStorageService(),

                new FakeContractorService(),

                NotificationService);

            SigningAuthorityService = new SigningAuthorityService(

                Db,

                NullLogger<SigningAuthorityService>.Instance);

        }

        public static async Task<TestSetup> CreateAsync()

        {

            var connection = new SqliteConnection("Data Source=:memory:");

            await connection.OpenAsync();

            var db = new TestDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options);

            await db.Database.EnsureCreatedAsync();

            var setup = new TestSetup(connection, db);

            await setup.SeedDataAsync();

            return setup;

        }

        private async Task SeedDataAsync()

        {

            ProcurementRole = new Role { Name = "Procurement", Description = "Procurement Officer / Manager" };

            var contractorRole = new Role { Name = "Contractor", Description = "Contractor" };

            Db.Roles.AddRange(ProcurementRole, contractorRole);

            ManagerUser = new User

            {

                FullName = "Procurement Manager",

                Email = "manager@procurement.invalid",

                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!")

            };

            OfficerUser = new User

            {

                FullName = "Field Officer",

                Email = "officer@procurement.invalid",

                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!")

            };

            SignerUser = new User

            {

                FullName = "Director Signer",

                Email = "signer@procurement.invalid",

                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!")

            };

            ContractorUser = new User

            {

                FullName = "Contractor Lead",

                Email = "contractor@construction.invalid",

                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!")

            };

            Db.Users.AddRange(ManagerUser, OfficerUser, SignerUser, ContractorUser);

            await Db.SaveChangesAsync();

            Db.UserRoles.AddRange(

                new UserRole { UserId = ManagerUser.Id, RoleId = ProcurementRole.Id },

                new UserRole { UserId = OfficerUser.Id, RoleId = ProcurementRole.Id },

                new UserRole { UserId = SignerUser.Id, RoleId = ProcurementRole.Id },

                new UserRole { UserId = ContractorUser.Id, RoleId = contractorRole.Id }

            );

            await Db.SaveChangesAsync();

            Entity = new ProcuringEntity

            {

                OrganizationName = "Tổng Công ty Phát triển Hạ tầng",

                OrganizationType = "DoanhNghiep",

                TaxCode = "0101010101",

                Address = "Hà Nội",

                RepresentativeName = "Nguyễn Văn Quản Lý",

                RepresentativeTitle = "Giám đốc",

                EstablishmentDecisionFile = "decision.pdf",

                UserId = ManagerUser.Id

            };

            Db.ProcuringEntities.Add(Entity);

            await Db.SaveChangesAsync();

            // Setup Entity Members

            var managerMember = new ProcuringEntityMember

            {

                ProcuringEntityId = Entity.Id,

                UserId = ManagerUser.Id,

                RoleInEntity = ProcuringEntityMemberRole.Manager,

                Status = MemberStatus.Active,

                JoinedAt = DateTime.UtcNow

            };

            var officerMember = new ProcuringEntityMember

            {

                ProcuringEntityId = Entity.Id,

                UserId = OfficerUser.Id,

                RoleInEntity = ProcuringEntityMemberRole.Officer,

                Status = MemberStatus.Active,

                JoinedAt = DateTime.UtcNow

            };

            var signerMember = new ProcuringEntityMember

            {

                ProcuringEntityId = Entity.Id,

                UserId = SignerUser.Id,

                RoleInEntity = ProcuringEntityMemberRole.Signer,

                Status = MemberStatus.Active,

                JoinedAt = DateTime.UtcNow

            };

            Db.ProcuringEntityMembers.AddRange(managerMember, officerMember, signerMember);

            var contractor = new Contractor

            {

                CompanyName = "Công ty CP Xây dựng Sông Đà",

                User = ContractorUser,

                TaxCode = "0101010101"

            };

            Db.Contractors.Add(contractor);

            var package = new BidPackage

            {

                Code = "PKG-INFRA-2026-01",

                Name = "Gói thầu thi công hạ tầng kỹ thuật",

                Creator = ManagerUser,
                ProcuringEntity = Entity,

                Budget = 500_000_000m,

                Deadline = DateTime.UtcNow.AddDays(30),

                Status = BidPackageStatus.Awarded

            };

            Db.BidPackages.Add(package);

            ActiveContract = new Contract

            {

                BidPackage = package,

                Contractor = contractor,

                ContractNumber = "HD-2026-001",

                Value = 500_000_000m,

                Status = ContractStatus.Active,

                StartDate = DateTime.UtcNow.AddDays(-10),

                EndDate = DateTime.UtcNow.AddDays(90)

            };

            Milestone = new ContractMilestone

            {

                Contract = ActiveContract,

                Title = "Nghiệm thu phần móng và nền hạ tầng",

                Amount = 150_000_000m,

                DueDate = DateTime.UtcNow.AddDays(15),

                Status = MilestoneStatus.Pending

            };

            ActiveContract.Milestones.Add(Milestone);

            Db.Contracts.Add(ActiveContract);

            var signerAuthority = new SigningAuthority

            {

                ProcuringEntityId = Entity.Id,

                UserId = SignerUser.Id,

                AuthorityType = SigningAuthorityType.DisbursementApproval,

                Scope = "EntityWide",

                ValidFrom = DateTime.UtcNow.AddDays(-30),

                ValidTo = DateTime.UtcNow.AddDays(365),

                Status = SigningAuthorityStatus.Active,

                DecisionNumber = "QD-UQ-2026/01",

                LegalBasis = "Quyết định bổ nhiệm và ủy quyền ký duyệt tài chính",

                RequestedByUserId = ManagerUser.Id,

                RequestedAt = DateTime.UtcNow.AddDays(-30),

                VerifiedByUserId = ManagerUser.Id,

                VerifiedAt = DateTime.UtcNow.AddDays(-29)

            };

            Db.SigningAuthorities.Add(signerAuthority);

            await Db.SaveChangesAsync();

        }

        public async ValueTask DisposeAsync()

        {

            await Db.DisposeAsync();

            await Connection.DisposeAsync();

        }

    }

    [Fact]

    public async Task SendInvitation_ByManager_CreatesHashedRecord_AndSendsEmail()

    {

        await using var s = await TestSetup.CreateAsync();

        var request = new SendInvitationRequest

        {

            Email = "new_engineer@infra.invalid",

            RoleInEntity = ProcuringEntityMemberRole.Officer

        };

        var result = await s.MemberService.SendInvitationAsync(s.Entity.Id, request, s.ManagerUser.Id, false);

        Assert.True(result.Success);

        Assert.NotNull(result.Data);

        Assert.Equal("new_engineer@infra.invalid", result.Data.Email);

        Assert.Equal(ProcuringEntityMemberRole.Officer, result.Data.RoleInEntity);

        Assert.Equal(InvitationStatus.Pending, result.Data.Status);

        // Check email dispatched

        Assert.Single(s.EmailService.SentEmails);

        var sent = s.EmailService.SentEmails[0];

        Assert.Equal("new_engineer@infra.invalid", sent.To);

        Assert.Contains("Lời mời tham gia Bên mời thầu", sent.Subject);

        // Verify SHA256 hashed token is stored in DB

        var invInDb = await s.Db.ProcuringEntityInvitations.FirstOrDefaultAsync(i => i.Id == result.Data.Id);

        Assert.NotNull(invInDb);

        Assert.NotEmpty(invInDb.TokenHash);

        Assert.True(invInDb.TokenHash.Length == 64); // SHA-256 hex string is 64 chars

    }

    [Fact]

    public async Task SendInvitation_ByOfficer_ReturnsForbidden()

    {

        await using var s = await TestSetup.CreateAsync();

        var request = new SendInvitationRequest

        {

            Email = "another@infra.invalid",

            RoleInEntity = ProcuringEntityMemberRole.Officer

        };

        var result = await s.MemberService.SendInvitationAsync(s.Entity.Id, request, s.OfficerUser.Id, false);

        Assert.False(result.Success);

        Assert.Equal(403, result.StatusCode);

    }

    [Fact]

    public async Task AcceptInvitation_NewUser_CreatesProcurementAccountAndActiveMembership()

    {

        await using var s = await TestSetup.CreateAsync();

        var sendResult = await s.MemberService.SendInvitationAsync(s.Entity.Id, new SendInvitationRequest

        {

            Email = "inspector@infra.invalid",

            RoleInEntity = ProcuringEntityMemberRole.Officer

        }, s.ManagerUser.Id, false);

        Assert.True(sendResult.Success);

        Assert.Single(s.EmailService.SentEmails);

        var email = s.EmailService.SentEmails[0];

        var tokenIdx = email.Body.IndexOf("token=");

        Assert.True(tokenIdx >= 0);

        var rawToken = email.Body.Substring(tokenIdx + 6, 64);

        Assert.False(string.IsNullOrEmpty(rawToken));

        // Inspect token anonymously

        var inspectResult = await s.MemberService.GetInvitationByTokenAsync(rawToken);

        Assert.True(inspectResult.Success);

        Assert.False(inspectResult.Data!.HasExistingAccount);

        Assert.Equal(s.Entity.OrganizationName, inspectResult.Data.OrganizationName);

        // Accept invitation with registration info

        var acceptResult = await s.MemberService.AcceptInvitationAsync(new AcceptInvitationRequest

        {

            Token = rawToken,

            FullName = "Kỹ sư Giám sát",

            Password = "PasswordSecure123!"

        }, currentUserId: null);

        Assert.True(acceptResult.Success);

        // Check new user was created

        var newUser = await s.Db.Users.FirstOrDefaultAsync(u => u.Email == "inspector@infra.invalid");

        Assert.NotNull(newUser);

        Assert.Equal("Kỹ sư Giám sát", newUser.FullName);

        var userRoles = await s.Db.UserRoles

            .Include(ur => ur.Role)

            .Where(ur => ur.UserId == newUser.Id)

            .ToListAsync();

        Assert.Contains(userRoles, ur => ur.Role.Name == "Procurement");

        // Check membership created

        var membership = await s.Db.ProcuringEntityMembers

            .FirstOrDefaultAsync(m => m.ProcuringEntityId == s.Entity.Id && m.UserId == newUser.Id);

        Assert.NotNull(membership);

        Assert.Equal(ProcuringEntityMemberRole.Officer, membership.RoleInEntity);

        Assert.Equal(MemberStatus.Active, membership.Status);

        // Check invitation marked Accepted

        Assert.NotNull(sendResult.Data);

        var inv = await s.Db.ProcuringEntityInvitations.FindAsync(sendResult.Data.Id);

        Assert.NotNull(inv);

        Assert.Equal(InvitationStatus.Accepted, inv.Status);

    }

    [Fact]

    public async Task MakerChecker_TechnicalReview_OfficerMarksPassed()

    {

        await using var s = await TestSetup.CreateAsync();

        var reviewRequest = new TechnicalReviewMilestoneRequest

        {

            IsPassed = true,

            Note = "Đã kiểm tra cao độ, bê tông móng đạt R28 theo thiết kế."

        };

        var result = await s.ContractService.TechnicalReviewMilestoneAsync(

            s.Milestone.Id, reviewRequest, s.OfficerUser.Id, false);

        Assert.True(result.Success);

        Assert.NotNull(result.Data);

        Assert.True(result.Data.IsTechnicalPassed);

        Assert.Equal(s.OfficerUser.Id, result.Data.TechnicalReviewedBy);

        Assert.Equal(AcceptanceStatus.TechnicalApproved, result.Data.Status);

        Assert.Equal("Field Officer", result.Data.TechnicalReviewerName);

    }

    [Fact]

    public async Task MakerChecker_SelfApprovalBlocked_WhenMakerAttemptsCheckerRole()

    {

        await using var s = await TestSetup.CreateAsync();

        // Scenario 1: Officer has only Officer role -> Forbidden to call Checker endpoint directly

        var officerForbiddenResult = await s.ContractService.ApproveMilestoneAcceptanceAsync(

            s.Milestone.Id, new ApproveMilestoneAcceptanceRequest

            {

                AcceptanceId = 1,

                ExpectedRevision = 1,

                IsApproved = true,

                ExpectedVersion = 1,

                Note = "Cán bộ cố gắng duyệt"

            }, s.OfficerUser.Id, false);

        Assert.False(officerForbiddenResult.Success);

        Assert.Equal(403, officerForbiddenResult.StatusCode);

        // Give Officer an active SigningAuthority so they have financial signing authority

        s.Db.SigningAuthorities.Add(new SigningAuthority

        {

            ProcuringEntityId = s.Entity.Id,

            UserId = s.OfficerUser.Id,

            AuthorityType = SigningAuthorityType.DisbursementApproval,

            Scope = "EntityWide",

            ValidFrom = DateTime.UtcNow.AddDays(-30),

            ValidTo = DateTime.UtcNow.AddDays(365),

            Status = SigningAuthorityStatus.Active,

            DecisionNumber = "QD-UQ-OFFICER",

            LegalBasis = "Quyết định phê duyệt giải ngân kiêm nhiệm",

            RequestedByUserId = s.ManagerUser.Id,

            RequestedAt = DateTime.UtcNow.AddDays(-30),

            VerifiedByUserId = s.ManagerUser.Id,

            VerifiedAt = DateTime.UtcNow.AddDays(-29)

        });

        await s.Db.SaveChangesAsync();

        // Scenario 2: Officer performs technical review (Maker)

        var reviewResult = await s.ContractService.TechnicalReviewMilestoneAsync(

            s.Milestone.Id, new TechnicalReviewMilestoneRequest

            {

                IsPassed = true,

                Note = "Cán bộ kỹ thuật kiểm tra hiện trường đạt chuẩn"

            }, s.OfficerUser.Id, false);

        Assert.True(reviewResult.Success);

        // Now Officer (who also holds SigningAuthority) attempts to self-approve disbursement (Checker)

        var approveResult = await s.ContractService.ApproveMilestoneAcceptanceAsync(

            s.Milestone.Id, new ApproveMilestoneAcceptanceRequest

            {

                AcceptanceId = reviewResult.Data!.Id,

                ExpectedRevision = reviewResult.Data.Revision,

                IsApproved = true,

                ExpectedVersion = 1,

                Note = "Cán bộ tự duyệt giải ngân luôn"

            }, s.OfficerUser.Id, false);

        // Must be rejected by Maker-Checker invariant

        Assert.False(approveResult.Success);

        Assert.Contains("Maker - Checker", approveResult.Message);

        Assert.Contains("không được phép tự phê duyệt giải ngân", approveResult.Message);

        // Milestone must remain Pending (not Completed)

        var milestoneInDb = await s.Db.ContractMilestones.FindAsync(s.Milestone.Id);

        Assert.Equal(MilestoneStatus.Pending, milestoneInDb!.Status);

    }

    [Fact]

    public async Task MakerChecker_SignerCanApprove_AfterOfficerTechnicalReview()

    {

        await using var s = await TestSetup.CreateAsync();

        // Step 1: Officer reviews technical dossier (Maker)

        var reviewResult = await s.ContractService.TechnicalReviewMilestoneAsync(

            s.Milestone.Id, new TechnicalReviewMilestoneRequest

            {

                IsPassed = true,

                Note = "Biên bản nghiệm thu kỹ thuật hiện trường số 01/NTNB-2026 đạt"

            }, s.OfficerUser.Id, false);

        Assert.True(reviewResult.Success);

        // Step 2: Signer (different user) approves financial disbursement (Checker)

        var approveResult = await s.ContractService.ApproveMilestoneAcceptanceAsync(

            s.Milestone.Id, new ApproveMilestoneAcceptanceRequest

            {

                AcceptanceId = reviewResult.Data!.Id,

                ExpectedRevision = reviewResult.Data.Revision,

                IsApproved = true,

                ExpectedVersion = 1,

                Note = "Chấp thuận thanh toán đợt 1 theo tờ trình và biên bản hiện trường"

            }, s.SignerUser.Id, false);

        Assert.True(approveResult.Success);

        Assert.NotNull(approveResult.Data);

        Assert.Equal(AcceptanceStatus.Approved, approveResult.Data.Status);

        Assert.Equal(s.SignerUser.Id, approveResult.Data.ApprovedBy);

        Assert.Equal(MilestoneStatus.Completed, approveResult.Data.MilestoneStatus);

        // Check DB persisted correctly

        var milestoneInDb = await s.Db.ContractMilestones

            .Include(m => m.Acceptances)

            .FirstOrDefaultAsync(m => m.Id == s.Milestone.Id);

        Assert.Equal(MilestoneStatus.Completed, milestoneInDb!.Status);

        var acceptance = milestoneInDb.Acceptances.First();

        Assert.Equal(s.OfficerUser.Id, acceptance.TechnicalReviewedBy);

        Assert.Equal(s.SignerUser.Id, acceptance.ApprovedBy);

    }

    [Fact]

    public async Task MakerChecker_CannotApprove_WithoutPriorTechnicalReview()

    {

        await using var s = await TestSetup.CreateAsync();

        // Signer attempts to disburse immediately without technical review

        var approveResult = await s.ContractService.ApproveMilestoneAcceptanceAsync(

            s.Milestone.Id, new ApproveMilestoneAcceptanceRequest

            {

                AcceptanceId = 9999,

                ExpectedRevision = 1,

                ExpectedVersion = 1,

                IsApproved = true,

                Note = "Duyệt nóng không qua kỹ thuật"

            }, s.SignerUser.Id, false);

        Assert.False(approveResult.Success);

        Assert.Contains("chưa được Cán bộ kỹ thuật kiểm tra", approveResult.Message);

    }

    [Fact]

    public async Task DirectSignerRoleAssignment_IsBlocked_ViaInvitationAndRoleUpdate()

    {

        await using var s = await TestSetup.CreateAsync();

        // Test 1: Invitation with Signer role is blocked

        var inviteResult = await s.MemberService.SendInvitationAsync(s.Entity.Id, new SendInvitationRequest

        {

            Email = "direct_signer@infra.invalid",

            RoleInEntity = ProcuringEntityMemberRole.Signer

        }, s.ManagerUser.Id, false);

        Assert.False(inviteResult.Success);

        Assert.Contains("Không thể gán trực tiếp vai trò Người ký duyệt", inviteResult.Message);

        // Test 2: Role update to Signer is blocked

        var officerMember = await s.Db.ProcuringEntityMembers.FirstAsync(m => m.UserId == s.OfficerUser.Id);

        var updateResult = await s.MemberService.UpdateMemberRoleAsync(s.Entity.Id, officerMember.Id, new UpdateMemberRoleRequest

        {

            RoleInEntity = ProcuringEntityMemberRole.Signer

        }, s.ManagerUser.Id, false);

        Assert.False(updateResult.Success);

        Assert.Contains("Không thể gán trực tiếp vai trò Người ký duyệt", updateResult.Message);

    }

    [Fact]

    public async Task Admin_CannotBypassOrApproveDisbursement_WithoutSigningAuthority()

    {

        await using var s = await TestSetup.CreateAsync();

        var adminUser = new User

        {

            FullName = "Platform Super Admin",

            Email = "admin@platform.gov.vn",

            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!")

        };

        var adminRole = new Role { Name = "Admin", Description = "System Admin" };

        s.Db.Roles.Add(adminRole);

        s.Db.Users.Add(adminUser);

        await s.Db.SaveChangesAsync();

                s.Db.UserRoles.Add(new UserRole { UserId = adminUser.Id, RoleId = adminRole.Id });

        s.Db.ProcuringEntityMembers.Add(new ProcuringEntityMember

        {

            ProcuringEntityId = s.Entity.Id,

            UserId = adminUser.Id,

            RoleInEntity = ProcuringEntityMemberRole.Manager,

            Status = MemberStatus.Active,

            JoinedAt = DateTime.UtcNow

        });

        await s.Db.SaveChangesAsync();

        // Officer technical review (Maker)

        var reviewResult = await s.ContractService.TechnicalReviewMilestoneAsync(

            s.Milestone.Id, new TechnicalReviewMilestoneRequest

            {

                IsPassed = true,

                Note = "Kỹ thuật đạt chuẩn"

            }, s.OfficerUser.Id, false);

        Assert.True(reviewResult.Success);

        // Platform Admin attempts to approve disbursement directly without a verified SigningAuthority

        var approveResult = await s.ContractService.ApproveMilestoneAcceptanceAsync(

            s.Milestone.Id, new ApproveMilestoneAcceptanceRequest

            {

                AcceptanceId = reviewResult.Data!.Id,

                ExpectedRevision = reviewResult.Data.Revision,

                ExpectedVersion = 1,

                IsApproved = true,

                Note = "Admin duyệt thay doanh nghiệp"

            }, adminUser.Id, true);

        // Must be rejected with 403 Forbidden because Admin cannot bypass signing authority

        Assert.False(approveResult.Success);

        Assert.Equal(403, approveResult.StatusCode);

        Assert.Contains("Quản trị viên hệ thống (Admin) không được phép phê duyệt giải ngân", approveResult.Message);

    }

    [Fact]

    public async Task MilestoneUpdate_InvalidatesPreviousTechnicalReview_AndIncrementsVersion()

    {

        await using var s = await TestSetup.CreateAsync();

        // 1. Initial version is 1

        Assert.Equal(1, s.Milestone.Version);

        // 2. Officer technical review passed

        var reviewResult = await s.ContractService.TechnicalReviewMilestoneAsync(

            s.Milestone.Id, new TechnicalReviewMilestoneRequest

            {

                IsPassed = true,

                Note = "Maker review v1 passed"

            }, s.OfficerUser.Id, false);

        Assert.True(reviewResult.Success);

        Assert.Equal(1, reviewResult.Data!.MilestoneVersion);

        Assert.Equal(s.Milestone.Amount, reviewResult.Data.SnapshotAmount);

        // 3. Manager updates milestone amount

        var updateResult = await s.ContractService.UpdateMilestoneAsync(

            s.ActiveContract.Id, s.Milestone.Id, new CreateMilestoneRequest

            {

                Title = s.Milestone.Title,

                DueDate = s.Milestone.DueDate,

                Amount = 180_000_000m // changed from 150_000_000m

            }, s.ManagerUser.Id, false);

        Assert.True(updateResult.Success);

        Assert.Equal(2, updateResult.Data!.Version);

        // 4. Verification that acceptance review status was invalidated

        var acceptanceInDb = await s.Db.Acceptances.FirstAsync(a => a.MilestoneId == s.Milestone.Id);

        Assert.Equal(AcceptanceStatus.ReviewInvalidated, acceptanceInDb.Status);

        Assert.False(acceptanceInDb.IsTechnicalPassed);

        Assert.Contains("HỆ THỐNG VÔ HIỆU HÓA", acceptanceInDb.Note);

        // 5. Signer attempts to approve invalidated review -> Rejected

        var approveResult = await s.ContractService.ApproveMilestoneAcceptanceAsync(

            s.Milestone.Id, new ApproveMilestoneAcceptanceRequest

            {

                AcceptanceId = acceptanceInDb.Id,

                ExpectedRevision = acceptanceInDb.Revision,

                IsApproved = true,

                ExpectedVersion = 2,

                Note = "Cố tình phê duyệt mốc sau khi sửa"

            }, s.SignerUser.Id, false);

        Assert.False(approveResult.Success);

        Assert.Contains("vô hiệu hóa", approveResult.Message);

    }

    [Fact]

    public async Task ConcurrencyStaleVersion_IsRejectedWith409()

    {

        await using var s = await TestSetup.CreateAsync();

        // 1. Officer technical review passed

        var reviewResult = await s.ContractService.TechnicalReviewMilestoneAsync(

            s.Milestone.Id, new TechnicalReviewMilestoneRequest

            {

                IsPassed = true,

                Note = "Maker review passed"

            }, s.OfficerUser.Id, false);

        Assert.True(reviewResult.Success);

        // 2. Checker tries to approve with stale ExpectedVersion (e.g., v99 while current is v1)

        var approveResult = await s.ContractService.ApproveMilestoneAcceptanceAsync(

            s.Milestone.Id, new ApproveMilestoneAcceptanceRequest

            {

                AcceptanceId = reviewResult.Data!.Id,

                ExpectedRevision = reviewResult.Data.Revision,

                IsApproved = true,

                ExpectedVersion = 99,

                Note = "Checker xem tab cũ"

            }, s.SignerUser.Id, false);

        Assert.False(approveResult.Success);

        Assert.Equal(409, approveResult.StatusCode);

        Assert.Contains("Xung đột phiên bản", approveResult.Message);

    }

    [Fact]

    public async Task SigningAuthorityService_FullLifecycle_RequestVerifyRevoke()

    {

        await using var s = await TestSetup.CreateAsync();

        var adminUser = new User

        {

            FullName = "Platform Super Admin",

            Email = "admin@platform.gov.vn",

            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!")

        };

        var adminRole = new Role { Name = "Admin", Description = "System Admin" };

        s.Db.Roles.Add(adminRole);

        s.Db.Users.Add(adminUser);

        await s.Db.SaveChangesAsync();

        s.Db.UserRoles.Add(new UserRole { UserId = adminUser.Id, RoleId = adminRole.Id });

        s.Db.ProcuringEntityMembers.Add(new ProcuringEntityMember

        {

            ProcuringEntityId = s.Entity.Id,

            UserId = adminUser.Id,

            RoleInEntity = ProcuringEntityMemberRole.Manager,

            Status = MemberStatus.Active,

            JoinedAt = DateTime.UtcNow

        });

        await s.Db.SaveChangesAsync();

        // Step 1: Manager requests authority for Officer

        var requestResult = await s.SigningAuthorityService.RequestAuthorityAsync(

            s.Entity.Id, s.ManagerUser.Id, new RequestSigningAuthorityRequest

            {

                UserId = s.OfficerUser.Id,

                AuthorityType = SigningAuthorityType.DisbursementApproval,

                Scope = "EntityWide",

                ValidFrom = DateTime.UtcNow.AddDays(1),

                ValidTo = DateTime.UtcNow.AddYears(1),

                DecisionNumber = "QD-UQ-TEST-99",

                LegalBasis = "Quyết định phân công ký duyệt hợp đồng"

            });

        Assert.Equal(SigningAuthorityStatus.PendingVerification, requestResult.Status);

        Assert.Equal("QD-UQ-TEST-99", requestResult.DecisionNumber);

        // Step 2: Self-approval attempt by Admin for self -> rejected

        var adminSelfRequest = await s.SigningAuthorityService.RequestAuthorityAsync(

            s.Entity.Id, s.ManagerUser.Id, new RequestSigningAuthorityRequest

            {

                UserId = adminUser.Id,

                AuthorityType = SigningAuthorityType.FullLegalRepresentative,

                Scope = "EntityWide",

                ValidFrom = DateTime.UtcNow,

                ValidTo = DateTime.UtcNow.AddYears(1),

                DecisionNumber = "QD-ADMIN-SELF",

                LegalBasis = "Tự cấp quyền"

            });

        await Assert.ThrowsAsync<InvalidOperationException>(() =>

            s.SigningAuthorityService.VerifyAuthorityAsync(adminSelfRequest.Id, adminUser.Id, new VerifySigningAuthorityRequest

            {

                IsApproved = true

            }));

        // Step 3: Admin verifies Officer request -> Active

        var verifyResult = await s.SigningAuthorityService.VerifyAuthorityAsync(

            requestResult.Id, adminUser.Id, new VerifySigningAuthorityRequest

            {

                IsApproved = true,

                VerificationNotes = "Đã đối chiếu con dấu và hồ sơ pháp lý hợp lệ."

            });

        Assert.Equal(SigningAuthorityStatus.Active, verifyResult.Status);

        Assert.Equal(adminUser.Id, verifyResult.VerifiedByUserId);

        // Step 4: Revoke authority

        var revokeResult = await s.SigningAuthorityService.RevokeAuthorityAsync(

            requestResult.Id, s.ManagerUser.Id, new RevokeSigningAuthorityRequest

            {

                RevocationReason = "Nhân sự chuyển công tác sang đơn vị khác"

            });

        Assert.Equal(SigningAuthorityStatus.Revoked, revokeResult.Status);

        Assert.Equal(s.ManagerUser.Id, revokeResult.RevokedByUserId);

    }

        [Fact]
    public async Task R2_SequentialApproval_AfterFirstWinner_RejectsSecondAttempt_SmokeTest()
    {
        await using var s = await TestSetup.CreateAsync();

        // Officer reviews technical dossier (Maker)
        var reviewResult = await s.ContractService.TechnicalReviewMilestoneAsync(
            s.Milestone.Id, new TechnicalReviewMilestoneRequest
            {
                IsPassed = true,
                Note = "Maker review passed"
            }, s.OfficerUser.Id, false);

        Assert.True(reviewResult.Success);

        // Create a second DbContext + ContractService instance sharing s.Connection (SQLite smoke test)
        await using var secondDb = new TestDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(s.Connection).Options);
        var secondContractService = new ContractService(new UnitOfWork(secondDb), new FileStorageService(), new FakeContractorService(), s.NotificationService);

        // SQLite in-memory provider does not support concurrent write transactions on the same connection.
        // This test serves as a baseline smoke test verifying that once an acceptance is approved by a first signer,
        // any subsequent approval attempt on the same acceptance is rejected with 409 Conflict.
        // True multi-connection parallel race conditions are verified in SqlServerConcurrencyAndIntegrityIntegrationTests.
        var firstResult = await s.ContractService.ApproveMilestoneAcceptanceAsync(
            s.Milestone.Id, new ApproveMilestoneAcceptanceRequest
            {
                AcceptanceId = reviewResult.Data!.Id,
                ExpectedRevision = reviewResult.Data.Revision,
                ExpectedVersion = reviewResult.Data.MilestoneVersion,
                IsApproved = true,
                Note = "Signer 1 first attempt"
            }, s.SignerUser.Id, false);

        Assert.True(firstResult.Success);
        Assert.Equal(200, firstResult.StatusCode);

        var secondResult = await secondContractService.ApproveMilestoneAcceptanceAsync(
            s.Milestone.Id, new ApproveMilestoneAcceptanceRequest
            {
                AcceptanceId = reviewResult.Data!.Id,
                ExpectedRevision = reviewResult.Data.Revision,
                ExpectedVersion = reviewResult.Data.MilestoneVersion,
                IsApproved = true,
                Note = "Signer 2 second attempt"
            }, s.SignerUser.Id, false);

        Assert.False(secondResult.Success);
        Assert.Equal(409, secondResult.StatusCode);
        Assert.Contains("nghiệm thu hoàn thành trước đó", secondResult.Message);
    }

    [Fact]

    public async Task R3_RevokedMembership_BlocksChecker_EvenWithActiveSigningAuthority()

    {

        await using var s = await TestSetup.CreateAsync();

        // Officer reviews

        var reviewResult = await s.ContractService.TechnicalReviewMilestoneAsync(

            s.Milestone.Id, new TechnicalReviewMilestoneRequest

            {

                IsPassed = true,

                Note = "Kỹ thuật đạt"

            }, s.OfficerUser.Id, false);

        Assert.True(reviewResult.Success);

        // Revoke Signer's membership in ProcuringEntityMembers

        var signerMember = await s.Db.ProcuringEntityMembers

            .FirstOrDefaultAsync(m => m.UserId == s.SignerUser.Id && m.ProcuringEntityId == s.Entity.Id);

        Assert.NotNull(signerMember);

        signerMember.Status = MemberStatus.Revoked;

        await s.Db.SaveChangesAsync();

        // Signer attempts to approve with active SigningAuthority

        var approveResult = await s.ContractService.ApproveMilestoneAcceptanceAsync(

            s.Milestone.Id, new ApproveMilestoneAcceptanceRequest

            {

                AcceptanceId = reviewResult.Data!.Id,

                ExpectedRevision = reviewResult.Data.Revision,

                IsApproved = true,

                ExpectedVersion = 1,

                Note = "Signer bị thu hồi tư cách thành viên cố gắng duyệt"

            }, s.SignerUser.Id, false);

        Assert.False(approveResult.Success);

        Assert.Equal(403, approveResult.StatusCode);

        Assert.Contains("không còn là thành viên đang hoạt động", approveResult.Message);

    }

    [Fact]

    public async Task R4_MakerStrictlyRequiresOfficerRole_AndAdminCannotApproveDisbursementEvenWithAuthority()

    {

        await using var s = await TestSetup.CreateAsync();

        // 1. Manager attempts Maker review -> Forbidden

        var managerMakerResult = await s.ContractService.TechnicalReviewMilestoneAsync(

            s.Milestone.Id, new TechnicalReviewMilestoneRequest

            {

                IsPassed = true,

                Note = "Quản lý muốn tự làm Maker"

            }, s.ManagerUser.Id, false);

        Assert.False(managerMakerResult.Success);

        Assert.Equal(403, managerMakerResult.StatusCode);

        Assert.Contains("Officer", managerMakerResult.Message);

        // 2. Platform Admin attempts Maker review -> Forbidden

        var adminMakerResult = await s.ContractService.TechnicalReviewMilestoneAsync(

            s.Milestone.Id, new TechnicalReviewMilestoneRequest

            {

                IsPassed = true,

                Note = "Admin muốn làm Maker"

            }, s.ManagerUser.Id, true);

        Assert.False(adminMakerResult.Success);

        Assert.Equal(403, adminMakerResult.StatusCode);

        Assert.Contains("Admin", adminMakerResult.Message);

        // 3. Officer performs valid Maker review

        var officerMakerResult = await s.ContractService.TechnicalReviewMilestoneAsync(

            s.Milestone.Id, new TechnicalReviewMilestoneRequest

            {

                IsPassed = true,

                Note = "Officer kiểm tra hiện trường đạt"

            }, s.OfficerUser.Id, false);

        Assert.True(officerMakerResult.Success);

        // 4. Platform Admin attempts disbursement approval -> Strictly Forbidden

        var adminApproveResult = await s.ContractService.ApproveMilestoneAcceptanceAsync(

            s.Milestone.Id, new ApproveMilestoneAcceptanceRequest

            {

                AcceptanceId = officerMakerResult.Data!.Id,

                ExpectedRevision = officerMakerResult.Data.Revision,

                IsApproved = true,

                ExpectedVersion = 1,

                Note = "Admin muốn duyệt giải ngân"

            }, s.ManagerUser.Id, true);

        Assert.False(adminApproveResult.Success);

        Assert.Equal(403, adminApproveResult.StatusCode);

        Assert.Contains("Admin", adminApproveResult.Message);

    }

    [Fact]

    public async Task R5_TechnicalReview_CreatesNewRevision_PreservesHistory_AndLeavesApprovedByNull()

    {

        await using var s = await TestSetup.CreateAsync();

        // Revision 1: Officer reviews, IsPassed = false

        var review1 = await s.ContractService.TechnicalReviewMilestoneAsync(

            s.Milestone.Id, new TechnicalReviewMilestoneRequest

            {

                IsPassed = false,

                Note = "Đợt 1: Khối lượng bê tông chưa đạt tỷ lệ thiết kế"

            }, s.OfficerUser.Id, false);

        Assert.True(review1.Success);

        Assert.Equal(1, review1.Data!.Revision);

        Assert.Null(review1.Data.ApprovedBy);

        Assert.Equal(AcceptanceStatus.Rejected, review1.Data.Status);

        // Revision 2: Contractor fixes, Officer reviews again, IsPassed = true

        var review2 = await s.ContractService.TechnicalReviewMilestoneAsync(

            s.Milestone.Id, new TechnicalReviewMilestoneRequest

            {

                IsPassed = true,

                Note = "Đợt 2: Đã khắc phục, đạt tiêu chuẩn kỹ thuật"

            }, s.OfficerUser.Id, false);

        Assert.True(review2.Success);

        Assert.Equal(2, review2.Data!.Revision);

        Assert.Null(review2.Data.ApprovedBy);

        Assert.Equal(AcceptanceStatus.TechnicalApproved, review2.Data.Status);

        // Check DB has 2 distinct acceptance records preserved

        var acceptancesInDb = await s.Db.Acceptances

            .Where(a => a.MilestoneId == s.Milestone.Id)

            .OrderBy(a => a.Revision)

            .ToListAsync();

        Assert.Equal(2, acceptancesInDb.Count);

        Assert.Equal(1, acceptancesInDb[0].Revision);

        Assert.Equal(AcceptanceStatus.Rejected, acceptancesInDb[0].Status);

        Assert.Null(acceptancesInDb[0].ApprovedBy);

        Assert.Equal(2, acceptancesInDb[1].Revision);

        Assert.Equal(AcceptanceStatus.TechnicalApproved, acceptancesInDb[1].Status);

        Assert.Null(acceptancesInDb[1].ApprovedBy);

        // Now Signer approves Revision 2

        var approveResult = await s.ContractService.ApproveMilestoneAcceptanceAsync(

            s.Milestone.Id, new ApproveMilestoneAcceptanceRequest

            {

                AcceptanceId = review2.Data!.Id,

                ExpectedRevision = review2.Data.Revision,

                IsApproved = true,

                ExpectedVersion = 1,

                Note = "Phê duyệt giải ngân trên biên bản revision 2"

            }, s.SignerUser.Id, false);

        Assert.True(approveResult.Success);

        Assert.Equal(2, approveResult.Data!.Revision);

        Assert.Equal(s.SignerUser.Id, approveResult.Data.ApprovedBy);

        // Verify in DB that only Revision 2 has ApprovedBy set

        var finalAcceptances = await s.Db.Acceptances

            .Where(a => a.MilestoneId == s.Milestone.Id)

            .OrderBy(a => a.Revision)

            .ToListAsync();

        Assert.Null(finalAcceptances[0].ApprovedBy);

        Assert.Equal(s.SignerUser.Id, finalAcceptances[1].ApprovedBy);

    }

    [Fact]

    public async Task R6_ScannedDocumentHash_Snapshotted_AndHistoricalFilesPreserved()

    {

        await using var s = await TestSetup.CreateAsync();

        // Create a temporary scanned contract file

        var tempDir = Path.Combine(Path.GetTempPath(), "ProcurementTests_" + Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(tempDir);

        var scanFile1 = Path.Combine(tempDir, "contract_v1_signed.pdf");

        await File.WriteAllTextAsync(scanFile1, "SCANNED_CONTRACT_CONTENT_VERSION_1");

        try

        {

            s.ActiveContract.ScannedFilePath = scanFile1;

            await s.Db.SaveChangesAsync();

            // Officer performs technical review

            var reviewResult = await s.ContractService.TechnicalReviewMilestoneAsync(

                s.Milestone.Id, new TechnicalReviewMilestoneRequest

                {

                    IsPassed = true,

                    Note = "Kiểm tra kèm hợp đồng đã quét"

                }, s.OfficerUser.Id, false);

            Assert.True(reviewResult.Success);

            Assert.NotNull(reviewResult.Data!.SnapshotDocumentHash);

            Assert.NotEmpty(reviewResult.Data.SnapshotDocumentHash);

            Assert.Equal(scanFile1, reviewResult.Data.SnapshotDocumentPath);

            // Check DB persisted the hash

            var acceptanceInDb = await s.Db.Acceptances

                .FirstOrDefaultAsync(a => a.MilestoneId == s.Milestone.Id);

            Assert.NotNull(acceptanceInDb);

            Assert.Equal(scanFile1, acceptanceInDb.SnapshotDocumentPath);

            Assert.NotNull(acceptanceInDb.SnapshotDocumentHash);

        }

        finally

        {

            if (Directory.Exists(tempDir))

            {

                Directory.Delete(tempDir, true);

            }

        }

    }

    [Fact]

    public async Task R7_UpdateMilestoneAsync_InvalidatesReviewAcrossIndependentDbContext()

    {

        await using var s = await TestSetup.CreateAsync();

        // Officer performs technical review on v1

        var reviewResult = await s.ContractService.TechnicalReviewMilestoneAsync(

            s.Milestone.Id, new TechnicalReviewMilestoneRequest

            {

                IsPassed = true,

                Note = "Nghiệm thu kỹ thuật ban đầu"

            }, s.OfficerUser.Id, false);

        Assert.True(reviewResult.Success);

        // Verify DB has TechnicalApproved status

        var acceptanceBefore = await s.Db.Acceptances

            .FirstOrDefaultAsync(a => a.MilestoneId == s.Milestone.Id);

        Assert.NotNull(acceptanceBefore);

        Assert.Equal(AcceptanceStatus.TechnicalApproved, acceptanceBefore.Status);

        // Call UpdateMilestoneAsync via an INDEPENDENT DbContext to simulate another HTTP request

        await using var secondDb = new TestDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(s.Connection).Options);

        var secondContractService = new ContractService(new UnitOfWork(secondDb), null!, null!, s.NotificationService);

        var updateResult = await secondContractService.UpdateMilestoneAsync(

            s.ActiveContract.Id, s.Milestone.Id, new CreateMilestoneRequest

            {

                Title = "Nghiệm thu phần móng và nền hạ tầng (Điều chỉnh tiến độ)",

                Amount = 160_000_000m,

                DueDate = DateTime.UtcNow.AddDays(25)

            }, s.ManagerUser.Id, false);

        Assert.True(updateResult.Success);

        Assert.Equal(2, updateResult.Data!.Version);

        // Check DB via a fresh query on s.Db: Acceptance must be ReviewInvalidated

        var acceptanceAfter = await s.Db.Acceptances

            .AsNoTracking()

            .FirstOrDefaultAsync(a => a.MilestoneId == s.Milestone.Id);

        Assert.NotNull(acceptanceAfter);

        Assert.Equal(AcceptanceStatus.ReviewInvalidated, acceptanceAfter.Status);

        Assert.False(acceptanceAfter.IsTechnicalPassed);

        Assert.Contains("HỆ THỐNG VÔ HIỆU HÓA", acceptanceAfter.Note);

    }

    #region F01 - F05 Regression Tests (Handoff 2026-10-29)

    [Fact]

    public async Task F01_RevokedEntityOwner_CannotApproveDisbursement_ReturnsForbidden()

    {

        await using var s = await TestSetup.CreateAsync();

        // Template authority to copy verified fields

        var template = await s.Db.SigningAuthorities.FirstAsync();

        // Give Manager (Entity Owner) an active SigningAuthority

        s.Db.SigningAuthorities.Add(new SigningAuthority

        {

            ProcuringEntityId = s.Entity.Id,

            UserId = s.ManagerUser.Id,

            AuthorityType = SigningAuthorityType.DisbursementApproval,

            Scope = "EntityWide",

            ValidFrom = DateTime.UtcNow.AddDays(-1),

            ValidTo = DateTime.UtcNow.AddDays(1),

            Status = SigningAuthorityStatus.Active,

            DecisionNumber = "REGRESSION-F01",

            LegalBasis = "F01 Unit Test",

            RequestedByUserId = s.ManagerUser.Id,

            VerifiedByUserId = template.VerifiedByUserId,

            VerifiedAt = DateTime.UtcNow

        });

        // Set Manager's membership in ProcuringEntityMembers to Revoked

        var membership = await s.Db.ProcuringEntityMembers.FirstAsync(m => m.ProcuringEntityId == s.Entity.Id && m.UserId == s.ManagerUser.Id);

        membership.Status = MemberStatus.Revoked;

        await s.Db.SaveChangesAsync();

        // Verify Manager is indeed the entity owner

        Assert.Equal(s.Entity.UserId, s.ManagerUser.Id);

        // Officer conducts technical review

        var review = await s.ContractService.TechnicalReviewMilestoneAsync(s.Milestone.Id, new TechnicalReviewMilestoneRequest

        {

            IsPassed = true,

            Note = "Kỹ thuật đạt"

        }, s.OfficerUser.Id, false);

        Assert.True(review.Success);

        // Manager (Revoked Owner) attempts to approve disbursement -> Must be Forbidden 403

        var approve = await s.ContractService.ApproveMilestoneAcceptanceAsync(s.Milestone.Id, new ApproveMilestoneAcceptanceRequest

        {

            AcceptanceId = review.Data!.Id,

            ExpectedRevision = review.Data.Revision,

            ExpectedVersion = review.Data.MilestoneVersion,

            IsApproved = true,

            Note = "Chủ đơn vị bị thu hồi cố duyệt"

        }, s.ManagerUser.Id, false);

        Assert.False(approve.Success);

        Assert.Equal(403, approve.StatusCode);

        Assert.Contains("không còn là thành viên đang hoạt động", approve.Message);

    }

    [Fact]

    public async Task F02_StaleAcceptanceRevision_Returns409Conflict_AndBlocksApprovalOfSupersededReview()

    {

        await using var s = await TestSetup.CreateAsync();

        // First technical review (Revision 1)

        var review1 = await s.ContractService.TechnicalReviewMilestoneAsync(s.Milestone.Id, new TechnicalReviewMilestoneRequest

        {

            IsPassed = true,

            Note = "Biên bản nghiệm thu đợt 1 xem xét"

        }, s.OfficerUser.Id, false);

        Assert.True(review1.Success);

        Assert.Equal(1, review1.Data!.Revision);

        // Second technical review (Revision 2) without changing milestone

        var review2 = await s.ContractService.TechnicalReviewMilestoneAsync(s.Milestone.Id, new TechnicalReviewMilestoneRequest

        {

            IsPassed = true,

            Note = "Biên bản nghiệm thu đợt 2 mới hơn"

        }, s.OfficerUser.Id, false);

        Assert.True(review2.Success);

        Assert.Equal(2, review2.Data!.Revision);

        // Checker attempts to approve Revision 1 (which has been superseded by Revision 2)

        var approveOld = await s.ContractService.ApproveMilestoneAcceptanceAsync(s.Milestone.Id, new ApproveMilestoneAcceptanceRequest

        {

            AcceptanceId = review1.Data.Id,

            ExpectedRevision = 1,

            ExpectedVersion = review1.Data.MilestoneVersion,

            IsApproved = true,

            Note = "Duyệt theo bản đợt 1 cũ"

        }, s.SignerUser.Id, false);

        // Must fail with 409 Conflict

        Assert.False(approveOld.Success);

        Assert.Equal(409, approveOld.StatusCode);

        Assert.True(approveOld.Message.Contains("thay thế") || approveOld.Message.Contains("vô hiệu hóa"));

        // Now Checker reviews and approves Revision 2 with matching AcceptanceId and ExpectedRevision

        var approveNew = await s.ContractService.ApproveMilestoneAcceptanceAsync(s.Milestone.Id, new ApproveMilestoneAcceptanceRequest

        {

            AcceptanceId = review2.Data.Id,

            ExpectedRevision = 2,

            ExpectedVersion = review2.Data.MilestoneVersion,

            IsApproved = true,

            Note = "Duyệt theo bản đợt 2 mới nhất"

        }, s.SignerUser.Id, false);

        Assert.True(approveNew.Success);

        Assert.Equal(2, approveNew.Data!.Revision);

        Assert.Equal(AcceptanceStatus.Approved, approveNew.Data.Status);

    }

    [Fact]

    public async Task F03_DocumentHash_ComputesRealFileBytesSHA256_AndFailsOnMissingFile()

    {

        await using var s = await TestSetup.CreateAsync();

        // 1. Create a physical test file in uploads/

        Directory.CreateDirectory("uploads");

        var testBytes = System.Text.Encoding.UTF8.GetBytes("F03 Test bytes for contract SHA256 integrity check");

        var filePath = "uploads/contract-f03-test.pdf";

        await File.WriteAllBytesAsync(filePath, testBytes);

        s.ActiveContract.ScannedFilePath = "/" + filePath;

        await s.Db.SaveChangesAsync();

        var expectedHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(testBytes)).ToLowerInvariant();

        var pathHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(s.ActiveContract.ScannedFilePath))).ToLowerInvariant();

        // Technical review

        var review = await s.ContractService.TechnicalReviewMilestoneAsync(s.Milestone.Id, new TechnicalReviewMilestoneRequest

        {

            IsPassed = true,

            Note = "Kiểm tra file thật"

        }, s.OfficerUser.Id, false);

        Assert.True(review.Success);

        Assert.Equal(expectedHash, review.Data!.SnapshotDocumentHash);

        Assert.NotEqual(pathHash, review.Data.SnapshotDocumentHash);

        // 2. Now set path to a non-existent file -> review must fail immediately (no fake hash fallback)

        s.ActiveContract.ScannedFilePath = "/uploads/non-existent-ghost-file.pdf";

        await s.Db.SaveChangesAsync();

        var reviewMissing = await s.ContractService.TechnicalReviewMilestoneAsync(s.Milestone.Id, new TechnicalReviewMilestoneRequest

        {

            IsPassed = true,

            Note = "Thẩm định file không tồn tại"

        }, s.OfficerUser.Id, false);

        Assert.False(reviewMissing.Success);

        Assert.Contains("Không tìm thấy tệp hồ sơ scan hợp đồng vật lý", reviewMissing.Message);

        // Clean up test file

        if (File.Exists(filePath)) File.Delete(filePath);

    }

    [Fact]

    public async Task F04_UploadScannedContract_InvalidatesPendingReviews_AndBlocksStaleApproval()

    {

        await using var s = await TestSetup.CreateAsync();

        // 1. Prepare file A

        Directory.CreateDirectory("uploads");

        var bytesA = System.Text.Encoding.UTF8.GetBytes("Contract Scan File Version A");

        var pathA = "uploads/scan-contract-vA.pdf";

        await File.WriteAllBytesAsync(pathA, bytesA);

        s.ActiveContract.ScannedFilePath = "/" + pathA;

        await s.Db.SaveChangesAsync();

        // 2. Officer technical review on scan A

        var reviewA = await s.ContractService.TechnicalReviewMilestoneAsync(s.Milestone.Id, new TechnicalReviewMilestoneRequest

        {

            IsPassed = true,

            Note = "Nghiệm thu trên bản scan A"

        }, s.OfficerUser.Id, false);

        Assert.True(reviewA.Success);

        Assert.Equal(AcceptanceStatus.TechnicalApproved, reviewA.Data!.Status);

        // 3. New scan file B is uploaded

        var bytesB = System.Text.Encoding.UTF8.GetBytes("Contract Scan File Version B - modified content");

        using var streamB = new MemoryStream(bytesB);

        var formFileB = new Microsoft.AspNetCore.Http.FormFile(streamB, 0, bytesB.Length, "file", "scan-vB.pdf")

        {

            Headers = new Microsoft.AspNetCore.Http.HeaderDictionary(),

            ContentType = "application/pdf"

        };

        var uploadResult = await s.ContractService.UploadScannedContractAsync(

            s.ActiveContract.Id, formFileB, s.ActiveContract.BidPackage.CreatedBy, false);

        Assert.True(uploadResult.Success);

        // 4. Verify previous acceptance was invalidated and Milestone.Version incremented

        var acceptanceInDb = await s.Db.Acceptances.FindAsync(reviewA.Data.Id);

        Assert.NotNull(acceptanceInDb);

        Assert.Equal(AcceptanceStatus.ReviewInvalidated, acceptanceInDb.Status);

        var milestoneInDb = await s.Db.ContractMilestones.FindAsync(s.Milestone.Id);

        Assert.NotNull(milestoneInDb);

        Assert.Equal(2, milestoneInDb.Version);

        // 5. Checker attempts to approve old review A -> Blocked with 409

        var approveOld = await s.ContractService.ApproveMilestoneAcceptanceAsync(s.Milestone.Id, new ApproveMilestoneAcceptanceRequest

        {

            AcceptanceId = reviewA.Data.Id,

            ExpectedRevision = reviewA.Data.Revision,

            ExpectedVersion = reviewA.Data.MilestoneVersion,

            IsApproved = true,

            Note = "Cố duyệt bản đã bị vô hiệu do đổi scan"

        }, s.SignerUser.Id, false);

        Assert.False(approveOld.Success);

        Assert.Equal(409, approveOld.StatusCode);

        Assert.Contains("vô hiệu hóa", approveOld.Message);

        // Clean up

        if (File.Exists(pathA)) File.Delete(pathA);

        if (File.Exists(uploadResult.Data!.TrimStart('/'))) File.Delete(uploadResult.Data.TrimStart('/'));

    }

    #endregion

}
