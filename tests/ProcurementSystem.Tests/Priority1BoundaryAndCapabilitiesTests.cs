using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ProcurementSystem.API.Extensions;
using ProcurementSystem.Core.DTOs;
using ProcurementSystem.Core.DTOs.BidPackage;
using ProcurementSystem.Core.DTOs.Common;
using ProcurementSystem.Core.DTOs.Contract;
using ProcurementSystem.Core.Enums;
using ProcurementSystem.Core.Interfaces;
using ProcurementSystem.Infrastructure.Data;
using ProcurementSystem.Infrastructure.Entities;
using ProcurementSystem.Infrastructure.Repositories;
using ProcurementSystem.Infrastructure.Services;
using ProcurementSystem.Infrastructure.Services.StateMachines;
using ProcurementSystem.Infrastructure.Utilities;
using Xunit;

namespace ProcurementSystem.Tests
{
    public class Priority1BoundaryAndCapabilitiesTests
    {
        private static async Task<(SqliteConnection conn, AppDbContext db)> CreateSqliteInMemoryDbAsync()
        {
            var conn = new SqliteConnection("DataSource=:memory:");
            await conn.OpenAsync();
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(conn)
                .Options;
            var db = new AppDbContext(options);
            await db.Database.EnsureCreatedAsync();
            return (conn, db);
        }

        private static async Task<(
            User managerA,
            User officerCreatorA,
            User officerOtherA,
            User signerA,
            User revokedOfficerA,
            User outsiderB,
            User contractorUser1,
            User contractorUser2,
            Contractor contractor1,
            Contractor contractor2,
            ProcuringEntity peA,
            ProcuringEntity peB,
            BidPackage packageA,
            BidSubmission submissionA,
            Contract contractA
        )> SeedScenarioAsync(AppDbContext db)
        {
            var managerA = new User { FullName = "Manager A", Email = "mgr.a@test.vn", PasswordHash = "h", IsActive = true };
            var officerCreatorA = new User { FullName = "Officer Creator A", Email = "off1.a@test.vn", PasswordHash = "h", IsActive = true };
            var officerOtherA = new User { FullName = "Officer Other A", Email = "off2.a@test.vn", PasswordHash = "h", IsActive = true };
            var signerA = new User { FullName = "Signer A", Email = "sig.a@test.vn", PasswordHash = "h", IsActive = true };
            var revokedOfficerA = new User { FullName = "Revoked Officer A", Email = "rev.a@test.vn", PasswordHash = "h", IsActive = true };
            var outsiderB = new User { FullName = "Outsider B", Email = "out.b@test.vn", PasswordHash = "h", IsActive = true };
            var contractorUser1 = new User { FullName = "Contractor 1", Email = "c1@test.vn", PasswordHash = "h", IsActive = true };
            var contractorUser2 = new User { FullName = "Contractor 2", Email = "c2@test.vn", PasswordHash = "h", IsActive = true };

            db.Users.AddRange(managerA, officerCreatorA, officerOtherA, signerA, revokedOfficerA, outsiderB, contractorUser1, contractorUser2);
            await db.SaveChangesAsync();

            var peA = new ProcuringEntity
            {
                UserId = managerA.Id,
                OrganizationName = "Ban QLDA A",
                TaxCode = "0100111222",
                OrganizationType = "StateEnterprise",
                Address = "HN",
                RepresentativeName = "Manager A",
                RepresentativeTitle = "Giam doc",
                EstablishmentDecisionFile = "uploads/dec_a.pdf",
                VerificationStatus = "Approved"
            };
            var peB = new ProcuringEntity
            {
                UserId = outsiderB.Id,
                OrganizationName = "Ban QLDA B",
                TaxCode = "0100333444",
                OrganizationType = "StateEnterprise",
                Address = "HCM",
                RepresentativeName = "Outsider B",
                RepresentativeTitle = "Giam doc",
                EstablishmentDecisionFile = "uploads/dec_b.pdf",
                VerificationStatus = "Approved"
            };
            db.ProcuringEntities.AddRange(peA, peB);
            await db.SaveChangesAsync();

            db.ProcuringEntityMembers.AddRange(
                new ProcuringEntityMember { ProcuringEntityId = peA.Id, UserId = managerA.Id, RoleInEntity = ProcuringEntityMemberRole.Manager, Status = MemberStatus.Active },
                new ProcuringEntityMember { ProcuringEntityId = peA.Id, UserId = officerCreatorA.Id, RoleInEntity = ProcuringEntityMemberRole.Officer, Status = MemberStatus.Active },
                new ProcuringEntityMember { ProcuringEntityId = peA.Id, UserId = officerOtherA.Id, RoleInEntity = ProcuringEntityMemberRole.Officer, Status = MemberStatus.Active },
                new ProcuringEntityMember { ProcuringEntityId = peA.Id, UserId = signerA.Id, RoleInEntity = ProcuringEntityMemberRole.Signer, Status = MemberStatus.Active },
                new ProcuringEntityMember { ProcuringEntityId = peA.Id, UserId = revokedOfficerA.Id, RoleInEntity = ProcuringEntityMemberRole.Officer, Status = MemberStatus.Revoked },
                new ProcuringEntityMember { ProcuringEntityId = peB.Id, UserId = outsiderB.Id, RoleInEntity = ProcuringEntityMemberRole.Manager, Status = MemberStatus.Active }
            );

            var contractor1 = new Contractor
            {
                UserId = contractorUser1.Id,
                CompanyName = "Nha Thau 1",
                TaxCode = "0300111222",
                Address = "HN",
                VerificationStatus = "Approved",
                BusinessLicenseFile = "contractors/license1.pdf"
            };
            var contractor2 = new Contractor
            {
                UserId = contractorUser2.Id,
                CompanyName = "Nha Thau 2",
                TaxCode = "0300333444",
                Address = "HCM",
                VerificationStatus = "Approved"
            };
            db.Contractors.AddRange(contractor1, contractor2);
            await db.SaveChangesAsync();

            var packageA = new BidPackage
            {
                Code = "PKG-PRIO1-001",
                Name = "Goi thau kiem thu Priority 1",
                Type = BidPackageType.Construction,
                Budget = 500_000_000m,
                Deadline = DateTime.UtcNow.AddDays(-1),
                Status = BidPackageStatus.Evaluating,
                CreatedBy = officerCreatorA.Id,
                ProcuringEntityId = peA.Id
            };
            db.BidPackages.Add(packageA);
            await db.SaveChangesAsync();

            var submissionA = new BidSubmission
            {
                BidPackageId = packageA.Id,
                ContractorId = contractor1.Id,
                BidPrice = 450_000_000m,
                Status = BidSubmissionStatus.Submitted,
                TotalScore = 92.5m,
                Rank = 1
            };
            db.BidSubmissions.Add(submissionA);

            var contractA = new Contract
            {
                BidPackageId = packageA.Id,
                ContractorId = contractor1.Id,
                ContractNumber = "HD-PRIO1-001",
                Value = 450_000_000m,
                Status = ContractStatus.Active,
                StartDate = DateTime.UtcNow.AddDays(-5),
                EndDate = DateTime.UtcNow.AddDays(60),
                ScannedFilePath = "contracts/nonexistent_scan.pdf"
            };
            db.Contracts.Add(contractA);
            await db.SaveChangesAsync();

            db.SigningAuthorities.Add(new SigningAuthority
            {
                ProcuringEntityId = peA.Id,
                UserId = signerA.Id,
                RequestedByUserId = managerA.Id,
                AuthorityType = SigningAuthorityType.DisbursementApproval,
                Scope = "EntityWide",
                ValidFrom = DateTime.UtcNow.AddDays(-10),
                ValidTo = DateTime.UtcNow.AddDays(30),
                DecisionNumber = "QD-SIGN-001",
                LegalBasis = "Quyet dinh uy quyen",
                Status = SigningAuthorityStatus.Active
            });

            db.ContractorEvaluations.Add(new ContractorEvaluation
            {
                ContractId = contractA.Id,
                ContractorId = contractor1.Id,
                EvaluatorUserId = officerCreatorA.Id,
                QualityScore = 36,
                ScheduleScore = 36,
                ComplianceScore = 18,
                TotalScore = 90,
                RatingGrade = "A"
            });

            await db.SaveChangesAsync();

            return (managerA, officerCreatorA, officerOtherA, signerA, revokedOfficerA, outsiderB, contractorUser1, contractorUser2, contractor1, contractor2, peA, peB, packageA, submissionA, contractA);
        }

        [Fact]
        public async Task GetBidPackageByIdAsync_ComputesCanManagePackage_ByEntityRoleAndOwnership()
        {
            var (conn, db) = await CreateSqliteInMemoryDbAsync();
            await using var _c = conn;
            await using var _d = db;
            var seed = await SeedScenarioAsync(db);
            var service = new BidPackageService(
                new UnitOfWork(db),
                new FileStorageService(),
                null!,
                new FakeNotificationService());

            // 1. Manager of Entity A -> CanManagePackage = true (even though CreatedBy is officerCreatorA)
            var resMgr = await service.GetBidPackageByIdAsync(seed.packageA.Id, seed.managerA.Id, isAdmin: false);
            Assert.True(resMgr.Success);
            Assert.True(resMgr.Data!.CanManagePackage);

            // 2. Officer Creator of Entity A -> CanManagePackage = true
            var resCreator = await service.GetBidPackageByIdAsync(seed.packageA.Id, seed.officerCreatorA.Id, isAdmin: false);
            Assert.True(resCreator.Data!.CanManagePackage);

            // 3. Another Officer of Entity A (not creator) -> CanManagePackage = false
            var resOtherOfficer = await service.GetBidPackageByIdAsync(seed.packageA.Id, seed.officerOtherA.Id, isAdmin: false);
            Assert.False(resOtherOfficer.Data!.CanManagePackage);

            // 4. Outsider Manager of Entity B -> CanManagePackage = false
            var resOutsider = await service.GetBidPackageByIdAsync(seed.packageA.Id, seed.outsiderB.Id, isAdmin: false);
            Assert.False(resOutsider.Data!.CanManagePackage);

            // 5. System Admin -> CanManagePackage = true
            var resAdmin = await service.GetBidPackageByIdAsync(seed.packageA.Id, 9999, isAdmin: true);
            Assert.True(resAdmin.Data!.CanManagePackage);
        }

        [Fact]
        public async Task BidSubmissionService_EnforcesEntityScopedAuthorization_ForManagerAndRevokedCreator()
        {
            var (conn, db) = await CreateSqliteInMemoryDbAsync();
            await using var _c = conn;
            await using var _d = db;
            var seed = await SeedScenarioAsync(db);
            var submissionService = new BidSubmissionService(
                new UnitOfWork(db),
                new FileStorageService(),
                new FakeNotificationService());
            var procurementRoles = new List<string> { "Procurement" };

            // 1. Manager of Entity A can view submissions and scores of package created by Officer A
            var mgrList = await submissionService.GetSubmissionsByPackageAsync(
                seed.packageA.Id, seed.managerA.Id, procurementRoles);
            Assert.True(mgrList.Success);
            Assert.Single(mgrList.Data!);
            Assert.Equal(92.5m, mgrList.Data![0].TotalScore);

            var mgrDetail = await submissionService.GetSubmissionByIdAsync(
                seed.submissionA.Id, seed.managerA.Id, procurementRoles);
            Assert.True(mgrDetail.Success);
            Assert.Equal(92.5m, mgrDetail.Data!.TotalScore);

            // 2. Outsider Procurement (Entity B) is blocked with 403 Forbidden
            var outsiderList = await submissionService.GetSubmissionsByPackageAsync(
                seed.packageA.Id, seed.outsiderB.Id, procurementRoles);
            Assert.False(outsiderList.Success);
            Assert.Equal(403, outsiderList.StatusCode);

            // 3. If Officer Creator A is revoked from Entity A, access to submissions is immediately blocked
            var creatorMembership = await db.ProcuringEntityMembers
                .FirstAsync(m => m.ProcuringEntityId == seed.peA.Id && m.UserId == seed.officerCreatorA.Id);
            creatorMembership.Status = MemberStatus.Revoked;
            await db.SaveChangesAsync();

            var revokedCreatorList = await submissionService.GetSubmissionsByPackageAsync(
                seed.packageA.Id, seed.officerCreatorA.Id, procurementRoles);
            Assert.False(revokedCreatorList.Success);
            Assert.Equal(403, revokedCreatorList.StatusCode);
        }

        [Fact]
        public async Task ContractService_ComputesMakerCheckerCapabilities_AndBlocksCrossEntityReads()
        {
            var (conn, db) = await CreateSqliteInMemoryDbAsync();
            await using var _c = conn;
            await using var _d = db;
            var seed = await SeedScenarioAsync(db);
            var contractService = new ContractService(
                new UnitOfWork(db),
                new FileStorageService(),
                new FakeContractorService(),
                new FakeNotificationService());

            // 1. Manager A: CanManageContract = true, CanTechnicalReview = false, CanApproveDisbursement = false
            var mgrContract = await contractService.GetContractByIdAsync(seed.contractA.Id, seed.managerA.Id, isInternalStaff: true, isAdmin: false);
            Assert.True(mgrContract.Success);
            Assert.True(mgrContract.Data!.CanManageContract);
            Assert.False(mgrContract.Data.CanTechnicalReview);
            Assert.False(mgrContract.Data.CanApproveDisbursement);

            // 2. Officer Creator A: CanManageContract = true, CanTechnicalReview = true, CanApproveDisbursement = false
            var officerContract = await contractService.GetContractByIdAsync(seed.contractA.Id, seed.officerCreatorA.Id, isInternalStaff: true, isAdmin: false);
            Assert.True(officerContract.Data!.CanManageContract);
            Assert.True(officerContract.Data.CanTechnicalReview);
            Assert.False(officerContract.Data.CanApproveDisbursement);

            // 3. Signer A (with active DisbursementApproval authority): CanManageContract = false, CanTechnicalReview = false, CanApproveDisbursement = true
            var signerContract = await contractService.GetContractByIdAsync(seed.contractA.Id, seed.signerA.Id, isInternalStaff: true, isAdmin: false);
            Assert.False(signerContract.Data!.CanManageContract);
            Assert.False(signerContract.Data.CanTechnicalReview);
            Assert.True(signerContract.Data.CanApproveDisbursement);

            // 4. Outsider B: Blocked from downloading scanned contract (403), viewing evaluation (403), and viewing contract details (403)
            var outsiderContract = await contractService.GetContractByIdAsync(seed.contractA.Id, seed.outsiderB.Id, isInternalStaff: true, isAdmin: false);
            Assert.False(outsiderContract.Success);
            Assert.Equal(403, outsiderContract.StatusCode);
            Assert.Null(outsiderContract.Data);

            var outsiderScan = await contractService.GetScannedContractForDownloadAsync(seed.contractA.Id, seed.outsiderB.Id, isInternalStaff: true, isAdmin: false);
            Assert.False(outsiderScan.Success);
            Assert.Equal(403, outsiderScan.StatusCode);

            var outsiderEval = await contractService.GetContractEvaluationAsync(seed.contractA.Id, seed.outsiderB.Id, isInternalStaff: true, isAdmin: false);
            Assert.False(outsiderEval.Success);
            Assert.Equal(403, outsiderEval.StatusCode);

            // 5. Revoked Officer A: Membership is revoked -> Blocked from viewing contract details (403)
            var revokedContract = await contractService.GetContractByIdAsync(seed.contractA.Id, seed.revokedOfficerA.Id, isInternalStaff: true, isAdmin: false);
            Assert.False(revokedContract.Success);
            Assert.Equal(403, revokedContract.StatusCode);
            Assert.Null(revokedContract.Data);

            // 6. Admin & Other Active Officer of Entity A: Can view contract details (200)
            var adminContract = await contractService.GetContractByIdAsync(seed.contractA.Id, 9999, isInternalStaff: true, isAdmin: true);
            Assert.True(adminContract.Success);
            Assert.NotNull(adminContract.Data);

            var otherOfficerContract = await contractService.GetContractByIdAsync(seed.contractA.Id, seed.officerOtherA.Id, isInternalStaff: true, isAdmin: false);
            Assert.True(otherOfficerContract.Success);
            Assert.NotNull(otherOfficerContract.Data);

            // 7. Contractor 1 (Owner of contractA) vs Contractor 2 (Different contractor)
            var c1Contract = await contractService.GetContractByIdAsync(seed.contractA.Id, seed.contractorUser1.Id, isInternalStaff: false, isAdmin: false);
            Assert.True(c1Contract.Success);
            Assert.NotNull(c1Contract.Data);

            var c2Contract = await contractService.GetContractByIdAsync(seed.contractA.Id, seed.contractorUser2.Id, isInternalStaff: false, isAdmin: false);
            Assert.False(c2Contract.Success);
            Assert.Equal(403, c2Contract.StatusCode);
            Assert.Null(c2Contract.Data);

            var entityStaffEval = await contractService.GetContractEvaluationAsync(seed.contractA.Id, seed.signerA.Id, isInternalStaff: true, isAdmin: false);
            Assert.True(entityStaffEval.Success);
            Assert.NotNull(entityStaffEval.Data);
        }

        [Fact]
        public async Task ContractorService_GetBusinessLicenseForDownloadAsync_BlocksOtherContractor()
        {
            var (conn, db) = await CreateSqliteInMemoryDbAsync();
            await using var _c = conn;
            await using var _d = db;
            var seed = await SeedScenarioAsync(db);
            var contractorService = new ContractorService(
                new UnitOfWork(db),
                new FileStorageService(),
                new FakeEmailService(),
                new FakeNotificationService());

            // Contractor 2 attempting to download Contractor 1's license -> 403 Forbidden
            var forbiddenRes = await contractorService.GetBusinessLicenseForDownloadAsync(
                seed.contractor1.Id, seed.contractorUser2.Id, isInternalStaff: false);
            Assert.False(forbiddenRes.Success);
            Assert.Equal(403, forbiddenRes.StatusCode);

            // Contractor 2 downloading own missing license -> 404 NotFound
            var missingRes = await contractorService.GetBusinessLicenseForDownloadAsync(
                seed.contractor2.Id, seed.contractorUser2.Id, isInternalStaff: false);
            Assert.False(missingRes.Success);
            Assert.Equal(404, missingRes.StatusCode);
        }

        private class DummyTestController : ControllerBase
        {
        }

        [Fact]
        public void ToFileActionResult_PreservesHttpErrorCodes_BadRequest_Forbidden_NotFound()
        {
            var controller = new DummyTestController();

            // 1. Lỗi 400 BadRequest (ví dụ: đường dẫn hỏng hoặc tham số không hợp lệ)
            var badRequestResponse = ApiResponse<FileDownloadResult>.Fail("Đường dẫn tệp không hợp lệ: invalid path", statusCode: StatusCodes.Status400BadRequest);
            var badResult = badRequestResponse.ToFileActionResult(controller);
            var badObjectResult = Assert.IsType<BadRequestObjectResult>(badResult);
            Assert.Equal(StatusCodes.Status400BadRequest, badObjectResult.StatusCode);

            // 2. Lỗi 403 Forbidden (ví dụ: không có quyền xem tệp của đơn vị khác)
            var forbiddenResponse = ApiResponse<FileDownloadResult>.Forbidden("Bạn không có quyền truy cập tệp tài liệu này.");
            var forbiddenResult = forbiddenResponse.ToFileActionResult(controller);
            var forbidObjectResult = Assert.IsType<ObjectResult>(forbiddenResult);
            Assert.Equal(StatusCodes.Status403Forbidden, forbidObjectResult.StatusCode);

            // 3. Lỗi 404 NotFound (ví dụ: tệp chưa tải lên hoặc không tồn tại)
            var notFoundResponse = ApiResponse<FileDownloadResult>.NotFound("Không tìm thấy tệp tài liệu.");
            var notFoundResult = notFoundResponse.ToFileActionResult(controller);
            var notFoundObjectResult = Assert.IsType<NotFoundObjectResult>(notFoundResult);
            Assert.Equal(StatusCodes.Status404NotFound, notFoundObjectResult.StatusCode);

            // 4. Luồng tải tệp thành công (200 OK FileStreamResult)
            var memoryStream = new MemoryStream(new byte[] { 1, 2, 3, 4 });
            var okResponse = ApiResponse<FileDownloadResult>.Ok(
                FileDownloadResult.FromStream(memoryStream, "hop_dong.pdf", "application/pdf"));
            var okResult = okResponse.ToFileActionResult(controller);
            var fileStreamResult = Assert.IsType<FileStreamResult>(okResult);
            Assert.Equal("application/pdf", fileStreamResult.ContentType);
            Assert.Equal("hop_dong.pdf", fileStreamResult.FileDownloadName);
        }

        [Theory]
        [InlineData("ho_so_moi_thau.pdf", "application/pdf")]
        [InlineData("phuong_an.DOCX", "application/vnd.openxmlformats-officedocument.wordprocessingml.document")]
        [InlineData("bang_gia.xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
        [InlineData("anh_scan.JPG", "image/jpeg")]
        [InlineData("file_khong_ro.unknown", "application/octet-stream")]
        public void ContentTypeResolver_ResolvesExpectedMimeType(string fileName, string expectedMime)
        {
            var actual = ContentTypeResolver.ResolveByFileName(fileName);
            Assert.Equal(expectedMime, actual);
        }

        [Fact]
        public async Task BidPackageStateMachine_And_ContractStateMachine_EnforceTransitionRules()
        {
            var (conn, db) = await CreateSqliteInMemoryDbAsync();
            await using var _c = conn;
            await using var _d = db;
            var seed = await SeedScenarioAsync(db);
            var uow = new UnitOfWork(db);
            var now = DateTime.UtcNow;

            // 1. BidPackage: Cannot jump directly from Open to Evaluating
            Assert.False(BidPackageStateMachine.CanTransition(BidPackageStatus.Open, BidPackageStatus.Evaluating));
            Assert.True(BidPackageStateMachine.CanTransition(BidPackageStatus.Open, BidPackageStatus.Closed));

            // 2. BidPackage: Early close from Open -> Closed (before Deadline) without Reason fails rule
            seed.packageA.Status = BidPackageStatus.Open;
            seed.packageA.Deadline = now.AddDays(5);
            var earlyCloseCtx = new BidPackageTransitionContext(
                seed.packageA,
                new ChangeBidPackageStatusRequest { NewStatus = BidPackageStatus.Closed, Reason = "" },
                uow,
                now);
            var earlyCloseRes = await BidPackageStateMachine.ValidateTransitionAsync(earlyCloseCtx);
            Assert.False(earlyCloseRes.IsValid);

            // 3. Contract: Completed is terminal (cannot transition back to Active)
            Assert.False(ContractStateMachine.CanTransition(ContractStatus.Completed, ContractStatus.Active));

            // 4. Contract: Terminated -> Active requires Admin privilege
            seed.contractA.Status = ContractStatus.Terminated;
            var nonAdminReactivateCtx = new ContractTransitionContext(
                seed.contractA,
                new ChangeContractStatusRequest
                {
                    NewStatus = ContractStatus.Active,
                    Note = "Khôi phục hợp đồng sau khi hoàn tất giải trình pháp lý đầy đủ"
                },
                seed.officerCreatorA.Id,
                IsAdmin: false,
                now);
            var nonAdminRes = ContractStateMachine.ValidateAndApplyTransitionMetadata(nonAdminReactivateCtx);
            Assert.False(nonAdminRes.IsValid);
            Assert.True(nonAdminRes.IsForbidden);

            var adminReactivateCtx = nonAdminReactivateCtx with { IsAdmin = true };
            var adminRes = ContractStateMachine.ValidateAndApplyTransitionMetadata(adminReactivateCtx);
            Assert.True(adminRes.IsValid);
            Assert.Contains("KHÔI PHỤC HĐ", seed.contractA.Terms);
        }

        [Fact]
        public async Task ContractService_ImplementsSegregatedSubInterfaces_Independently()
        {
            var (conn, db) = await CreateSqliteInMemoryDbAsync();
            await using var _c = conn;
            await using var _d = db;
            var seed = await SeedScenarioAsync(db);

            var service = new ContractService(
                new UnitOfWork(db),
                new FileStorageService(),
                new FakeContractorService(),
                new FakeNotificationService());

            IContractMilestoneAcceptanceService milestoneService = service;
            IContractGuaranteeService guaranteeService = service;
            IContractAddendumService addendumService = service;
            IContractorEvaluationService evaluationService = service;

            Assert.NotNull(milestoneService);
            var guaranteesRes = await guaranteeService.GetContractGuaranteesAsync(seed.contractA.Id, seed.managerA.Id, isInternalStaff: true, isAdmin: false);
            Assert.True(guaranteesRes.Success);

            var addendaRes = await addendumService.GetContractAddendaAsync(seed.contractA.Id, seed.managerA.Id, isInternalStaff: true, isAdmin: false);
            Assert.True(addendaRes.Success);

            var evalRes = await evaluationService.GetContractEvaluationAsync(seed.contractA.Id, seed.managerA.Id, isInternalStaff: true, isAdmin: false);
            Assert.True(evalRes.Success);
        }

        [Fact]
        public void DotEnvLoader_ParsesValuesWithEqualsAndBridgesConfigAliases()
        {
            var keys = new[]
            {
                "DB_CONNECTION_STRING",
                "ConnectionStrings__DefaultConnection",
                "PROCUREMENT_DB_CONN",
                "JWT_SECRET_KEY",
                "JwtSettings__SecretKey",
                "SMTP_HOST",
                "Smtp__Host",
                "SMTP_USERNAME",
                "Smtp__Username"
            };
            var snapshot = keys.ToDictionary(k => k, k => Environment.GetEnvironmentVariable(k));
            var tempEnv = Path.Combine(Path.GetTempPath(), $"procurement-env-test-{Guid.NewGuid():N}.env");

            try
            {
                foreach (var key in keys)
                {
                    Environment.SetEnvironmentVariable(key, null);
                }

                Environment.SetEnvironmentVariable("SMTP_HOST", "existing.smtp.override");
                File.WriteAllText(tempEnv, string.Join('\n', new[]
                {
                    "# Comment line",
                    "DB_CONNECTION_STRING=\"Server=localhost;Database=procurement_db;User Id=sa;Password=123;\"",
                    "JWT_SECRET_KEY='TestSecretKeyWithEquals=AtEnd_1234567890'",
                    "SMTP_HOST=smtp.ignored-override.test",
                    "SMTP_USERNAME=team@test.vn"
                }));

                ProcurementSystem.API.Extensions.DotEnvLoader.LoadFileAndSync(tempEnv);

                Assert.Equal("Server=localhost;Database=procurement_db;User Id=sa;Password=123;", Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection"));
                Assert.Equal("TestSecretKeyWithEquals=AtEnd_1234567890", Environment.GetEnvironmentVariable("JwtSettings__SecretKey"));
                Assert.Equal("existing.smtp.override", Environment.GetEnvironmentVariable("Smtp__Host"));
                Assert.Equal("team@test.vn", Environment.GetEnvironmentVariable("Smtp__Username"));
            }
            finally
            {
                if (File.Exists(tempEnv))
                {
                    File.Delete(tempEnv);
                }

                foreach (var (key, value) in snapshot)
                {
                    Environment.SetEnvironmentVariable(key, value);
                }
            }
        }
    }
}
