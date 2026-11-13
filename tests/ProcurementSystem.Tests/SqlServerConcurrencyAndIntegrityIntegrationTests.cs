using System.Text.Json;
using System.Data.Common;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.IdentityModel.Tokens;
using ProcurementSystem.API.Controllers;
using ProcurementSystem.Core.DTOs;
using ProcurementSystem.Core.DTOs.BidPackage;
using ProcurementSystem.Core.DTOs.Contract;
using ProcurementSystem.Core.DTOs.SigningAuthority;
using ProcurementSystem.Core.Enums;
using ProcurementSystem.Core.Interfaces;
using ProcurementSystem.Infrastructure.Data;
using ProcurementSystem.Infrastructure.Entities;
using ProcurementSystem.Infrastructure.Repositories;
using ProcurementSystem.Infrastructure.Services;
using Xunit;

namespace ProcurementSystem.Tests
{
    public class SqlServerConcurrencyAndIntegrityIntegrationTests : IAsyncDisposable
    {
        private readonly string _dbName;
        private readonly string _masterConnStr;
        private readonly string _testConnStr;
        private bool _databaseCreated;

        public SqlServerConcurrencyAndIntegrityIntegrationTests()
        {
            _dbName = $"ProcurementTest_R4_{Guid.NewGuid():N}";
            _masterConnStr = "Server=(localdb)\\MSSQLLocalDB;Database=master;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";
            _testConnStr = $"Server=(localdb)\\MSSQLLocalDB;Database={_dbName};Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";
        }

        /// <summary>
        /// R4-05: Không bao giờ return sớm và báo PASS giả khi thiếu SQL Server.
        /// Nếu SQL Server LocalDB không khả dụng, test sẽ FAIL tường minh kèm thông báo rõ ràng.
        /// </summary>
        private async Task<string> EnsureSqlServerAvailableAsync()
        {
            await using var conn = new SqlConnection(_masterConnStr);
            await conn.OpenAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT @@VERSION;";
            var version = (string?)await cmd.ExecuteScalarAsync();
            Assert.False(string.IsNullOrWhiteSpace(version), "SQL Server LocalDB must return a valid @@VERSION.");
            _databaseCreated = true;
            return version!;
        }

        private AppDbContext CreateDbContext(string? connStr = null, params IInterceptor[] interceptors)
        {
            var builder = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlServer(connStr ?? _testConnStr);
            if (interceptors != null && interceptors.Length > 0)
            {
                builder.AddInterceptors(interceptors);
            }
            return new AppDbContext(builder.Options);
        }

        public async ValueTask DisposeAsync()
        {
            if (_databaseCreated)
            {
                try
                {
                    await using var conn = new SqlConnection(_masterConnStr);
                    await conn.OpenAsync();
                    await using var cmd = conn.CreateCommand();
                    cmd.CommandText = $@"
                        IF EXISTS (SELECT 1 FROM sys.databases WHERE name = '{_dbName}')
                        BEGIN
                            ALTER DATABASE [{_dbName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                            DROP DATABASE [{_dbName}];
                        END";
                    await cmd.ExecuteNonQueryAsync();
                }
                catch
                {
                    // Best-effort cleanup of isolated test DB
                }
            }
        }

        private async Task<(User manager, User officer, User signer, User outsider, User contractorUser, Contractor contractor, ProcuringEntity peA, ProcuringEntity peB, BidPackage packageA, Contract contractA, ContractMilestone milestoneA, Acceptance acceptanceA, SigningAuthority authorityA)>
            SeedBaseDataAsync(AppDbContext db)
        {
            var procRole = new Role { Name = "Procurement", Description = "Procurement" };
            var contrRole = new Role { Name = "Contractor", Description = "Contractor" };
            db.Roles.AddRange(procRole, contrRole);

            var manager = new User { FullName = "Manager A", Email = "mgrA@test.invalid", PasswordHash = "hash" };
            var officer = new User { FullName = "Officer A (Maker)", Email = "officerA@test.invalid", PasswordHash = "hash" };
            var signer = new User { FullName = "Signer A (Checker)", Email = "signerA@test.invalid", PasswordHash = "hash" };
            var outsider = new User { FullName = "Outsider Procurement", Email = "outsider@test.invalid", PasswordHash = "hash" };
            var contractorUser = new User { FullName = "Contractor User", Email = "contractor@test.invalid", PasswordHash = "hash" };
            var userOwnerB = new User { FullName = "Owner B", Email = "ownerB@test.invalid", PasswordHash = "hash" };

            db.Users.AddRange(manager, officer, signer, outsider, contractorUser, userOwnerB);
            await db.SaveChangesAsync();

            var contractor = new Contractor
            {
                UserId = contractorUser.Id,
                CompanyName = "Cong Ty Xay Dung A",
                TaxCode = "0109999888",
                Address = "Hanoi",
                VerificationStatus = "Approved"
            };
            db.Contractors.Add(contractor);

            var peA = new ProcuringEntity
            {
                UserId = manager.Id,
                OrganizationName = "Ben Moi Thau A",
                OrganizationType = "DoanhNghiep",
                TaxCode = "0101111222",
                RepresentativeName = "Manager A",
                RepresentativeTitle = "Giam Doc",
                EstablishmentDecisionFile = "uploads/dec_a.pdf",
                VerificationStatus = "Approved"
            };

            var peB = new ProcuringEntity
            {
                UserId = userOwnerB.Id,
                OrganizationName = "Ben Moi Thau B",
                OrganizationType = "DoanhNghiep",
                TaxCode = "0103333444",
                RepresentativeName = "Owner B",
                RepresentativeTitle = "Giam Doc",
                EstablishmentDecisionFile = "uploads/dec_b.pdf",
                VerificationStatus = "Approved"
            };
            db.ProcuringEntities.AddRange(peA, peB);
            await db.SaveChangesAsync();

            var memManager = new ProcuringEntityMember { ProcuringEntityId = peA.Id, UserId = manager.Id, RoleInEntity = ProcuringEntityMemberRole.Manager, Status = MemberStatus.Active };
            var memOfficer = new ProcuringEntityMember { ProcuringEntityId = peA.Id, UserId = officer.Id, RoleInEntity = ProcuringEntityMemberRole.Officer, Status = MemberStatus.Active };
            var memSigner = new ProcuringEntityMember { ProcuringEntityId = peA.Id, UserId = signer.Id, RoleInEntity = ProcuringEntityMemberRole.Signer, Status = MemberStatus.Active };
            var memOutsiderB = new ProcuringEntityMember { ProcuringEntityId = peB.Id, UserId = outsider.Id, RoleInEntity = ProcuringEntityMemberRole.Officer, Status = MemberStatus.Active };
            var memOwnerB = new ProcuringEntityMember { ProcuringEntityId = peB.Id, UserId = userOwnerB.Id, RoleInEntity = ProcuringEntityMemberRole.Manager, Status = MemberStatus.Active };
            db.ProcuringEntityMembers.AddRange(memManager, memOfficer, memSigner, memOutsiderB, memOwnerB);
            await db.SaveChangesAsync();

            var packageA = new BidPackage
            {
                Code = "PKG-A-001",
                Name = "Goi thau thi cong A",
                Budget = 500000000m,
                Deadline = DateTime.UtcNow.AddDays(30),
                Status = BidPackageStatus.Open,
                CreatedBy = officer.Id, // Officer-created package (R4-01 & R4-02)
                ProcuringEntityId = peA.Id
            };
            db.BidPackages.Add(packageA);
            await db.SaveChangesAsync();

            var contractScanPath = Path.Combine(Directory.GetCurrentDirectory(), $"test_contract_scan_{Guid.NewGuid():N}.pdf");
            await File.WriteAllTextAsync(contractScanPath, "DUMMY CONTRACT SCAN CONTENT A");
            string docHash;
            await using (var fs = File.OpenRead(contractScanPath))
            {
                docHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(fs)).ToLowerInvariant();
            }

            var contractA = new Contract
            {
                BidPackageId = packageA.Id,
                ContractorId = contractor.Id,
                ContractNumber = "CTR-A-001",
                Value = 200000000m,
                StartDate = DateTime.UtcNow.AddDays(-10),
                EndDate = DateTime.UtcNow.AddDays(60),
                Status = ContractStatus.Active,
                ScannedFilePath = contractScanPath
            };
            db.Contracts.Add(contractA);
            await db.SaveChangesAsync();

            var milestoneA = new ContractMilestone
            {
                ContractId = contractA.Id,
                Title = "Moc 1: Thi cong mong",
                Amount = 100000000m,
                DueDate = DateTime.UtcNow.AddDays(15),
                Status = MilestoneStatus.Pending,
                Version = 1
            };
            db.ContractMilestones.Add(milestoneA);
            await db.SaveChangesAsync();

            var authorityA = new SigningAuthority
            {
                ProcuringEntityId = peA.Id,
                UserId = signer.Id,
                AuthorityType = SigningAuthorityType.DisbursementApproval,
                Scope = "EntityWide",
                ValidFrom = DateTime.UtcNow.AddDays(-5),
                ValidTo = DateTime.UtcNow.AddDays(180),
                Status = SigningAuthorityStatus.Active,
                DecisionNumber = "QD-UQ-001/A",
                LegalBasis = "Luat Dau Thau 2023",
                RequestedByUserId = manager.Id,
                RequestedAt = DateTime.UtcNow.AddDays(-5),
                VerifiedByUserId = 1,
                VerifiedAt = DateTime.UtcNow.AddDays(-4)
            };
            var officerAuthority = new SigningAuthority
            {
                ProcuringEntityId = peA.Id,
                UserId = officer.Id,
                AuthorityType = SigningAuthorityType.DisbursementApproval,
                Scope = "EntityWide",
                ValidFrom = DateTime.UtcNow.AddDays(-5),
                ValidTo = DateTime.UtcNow.AddDays(180),
                Status = SigningAuthorityStatus.Active,
                DecisionNumber = "QD-UQ-OFFICER/A",
                LegalBasis = "Luat Dau Thau 2023",
                RequestedByUserId = manager.Id,
                RequestedAt = DateTime.UtcNow.AddDays(-5),
                VerifiedByUserId = 1,
                VerifiedAt = DateTime.UtcNow.AddDays(-4)
            };
            db.SigningAuthorities.AddRange(authorityA, officerAuthority);
            await db.SaveChangesAsync();

            var acceptanceA = new Acceptance
            {
                ContractId = contractA.Id,
                MilestoneId = milestoneA.Id,
                Revision = 1,
                MilestoneVersion = 1,
                TechnicalReviewedBy = officer.Id,
                TechnicalReviewedAt = DateTime.UtcNow,
                IsTechnicalPassed = true,
                Status = AcceptanceStatus.TechnicalApproved,
                SnapshotAmount = milestoneA.Amount,
                SnapshotTitle = milestoneA.Title,
                SnapshotDueDate = milestoneA.DueDate,
                SnapshotDocumentPath = contractScanPath,
                SnapshotDocumentHash = docHash,
                SigningAuthorityId = authorityA.Id
            };
            db.Acceptances.Add(acceptanceA);
            await db.SaveChangesAsync();

            return (manager, officer, signer, outsider, contractorUser, contractor, peA, peB, packageA, contractA, milestoneA, acceptanceA, authorityA);
        }

        [Fact]
        public async Task T01_Controller_DownloadAcceptanceSnapshot_AuthorizationMatrix_EnforcesBoundaries()
        {
            await EnsureSqlServerAvailableAsync();

            await using var db = CreateDbContext();
            await db.Database.EnsureDeletedAsync();
            await db.Database.MigrateAsync();

            var seed = await SeedBaseDataAsync(db);

            var fileStorage = new FileStorageService();
            var contractService = new ContractService(new UnitOfWork(db), fileStorage, new FakeContractorService(), new FakeNotificationService());
            var controller = new ContractsController(contractService);

            // 1. Legitimate Officer of Entity A -> Allowed 200 OK
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, seed.officer.Id.ToString()),
                        new Claim(ClaimTypes.Role, "Procurement")
                    }, "TestAuth"))
                }
            };
            var result1 = await controller.DownloadAcceptanceSnapshotDocument(seed.milestoneA.Id, seed.acceptanceA.Id);
            Assert.IsType<PhysicalFileResult>(result1);

            // 2. Outsider user with Procurement role (belongs to Entity B, not Entity A) -> 403 Forbidden
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, seed.outsider.Id.ToString()),
                        new Claim(ClaimTypes.Role, "Procurement")
                    }, "TestAuth"))
                }
            };
            var result2 = await controller.DownloadAcceptanceSnapshotDocument(seed.milestoneA.Id, seed.acceptanceA.Id);
            var objResult2 = Assert.IsType<ObjectResult>(result2);
            Assert.Equal(StatusCodes.Status403Forbidden, objResult2.StatusCode);

            // 3. User with Revoked membership in Entity A (even if moved to Entity B!) -> 403 Forbidden
            var mem = await db.ProcuringEntityMembers.FirstAsync(m => m.UserId == seed.officer.Id && m.ProcuringEntityId == seed.peA.Id);
            mem.Status = MemberStatus.Revoked;
            db.ProcuringEntityMembers.Add(new ProcuringEntityMember
            {
                ProcuringEntityId = seed.peB.Id,
                UserId = seed.officer.Id,
                RoleInEntity = ProcuringEntityMemberRole.Officer,
                Status = MemberStatus.Active
            });
            await db.SaveChangesAsync();

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, seed.officer.Id.ToString()),
                        new Claim(ClaimTypes.Role, "Procurement")
                    }, "TestAuth"))
                }
            };
            var result3 = await controller.DownloadAcceptanceSnapshotDocument(seed.milestoneA.Id, seed.acceptanceA.Id);
            var objResult3 = Assert.IsType<ObjectResult>(result3);
            Assert.Equal(StatusCodes.Status403Forbidden, objResult3.StatusCode);

            // 3b. Entity B member STILL gets 403 Forbidden even though package creator (officer) is now in Entity B!
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, seed.outsider.Id.ToString()),
                        new Claim(ClaimTypes.Role, "Procurement")
                    }, "TestAuth"))
                }
            };
            var result3b = await controller.DownloadAcceptanceSnapshotDocument(seed.milestoneA.Id, seed.acceptanceA.Id);
            var objResult3b = Assert.IsType<ObjectResult>(result3b);
            Assert.Equal(StatusCodes.Status403Forbidden, objResult3b.StatusCode);

            // 3c. Entity A active Signer STILL gets 200 OK!
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, seed.signer.Id.ToString()),
                        new Claim(ClaimTypes.Role, "Procurement")
                    }, "TestAuth"))
                }
            };
            var result3c = await controller.DownloadAcceptanceSnapshotDocument(seed.milestoneA.Id, seed.acceptanceA.Id);
            Assert.IsType<PhysicalFileResult>(result3c);

            // 4. Contractor owning contract -> Allowed 200 OK
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, seed.contractorUser.Id.ToString()),
                        new Claim(ClaimTypes.Role, "Contractor")
                    }, "TestAuth"))
                }
            };
            var result4 = await controller.DownloadAcceptanceSnapshotDocument(seed.milestoneA.Id, seed.acceptanceA.Id);
            Assert.IsType<PhysicalFileResult>(result4);

            // 5. Unauthenticated (no NameIdentifier) -> 401 Unauthorized
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) }
            };
            var result5 = await controller.DownloadAcceptanceSnapshotDocument(seed.milestoneA.Id, seed.acceptanceA.Id);
            var authResult = Assert.IsType<UnauthorizedObjectResult>(result5);
            Assert.Equal(StatusCodes.Status401Unauthorized, authResult.StatusCode);
        }

        /// <summary>
        /// R4-05: Full HTTP Pipeline Integration Test qua Kestrel + JWT Bearer Authentication + HttpClient thực tế.
        /// Kiểm chứng [Authorize], routing, status code 401/403/200/409 và byte stream tải về.
        /// </summary>
        [Fact]
        public async Task T01_HttpPipeline_DownloadAcceptanceSnapshot_RealJwtAndHttpClient_EnforcesBoundaries()
        {
            await EnsureSqlServerAvailableAsync();

            await using var db = CreateDbContext();
            await db.Database.EnsureDeletedAsync();
            await db.Database.MigrateAsync();

            var seed = await SeedBaseDataAsync(db);
            const string jwtKey = "TestSecretKeyForRound4HttpPipelineIntegrationTests_32Bytes!";
            const string jwtIssuer = "ProcurementTestIssuer";

            var builder = WebApplication.CreateBuilder();
            builder.WebHost.UseSetting("urls", "http://127.0.0.1:0");
            builder.Services.AddDbContext<AppDbContext>(opt => opt.UseSqlServer(_testConnStr));
            builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
            builder.Services.AddScoped<IFileStorageService, FileStorageService>();
            builder.Services.AddScoped<IContractorService, FakeContractorService>();
            builder.Services.AddScoped<INotificationService, FakeNotificationService>();
            builder.Services.AddScoped<IContractService, ContractService>();
            builder.Services.AddScoped<IBidPackageService>(sp => new BidPackageService(sp.GetRequiredService<IUnitOfWork>(), sp.GetRequiredService<IFileStorageService>(), null!, sp.GetRequiredService<INotificationService>()));
            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = jwtIssuer,
                        ValidAudience = jwtIssuer,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
                    };
                });
            builder.Services.AddAuthorization();
            builder.Services.AddControllers().AddApplicationPart(typeof(ContractsController).Assembly);

            await using var app = builder.Build();
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapControllers();
            await app.StartAsync();

            try
            {
                var address = app.Services.GetRequiredService<IServer>()
                    .Features.Get<IServerAddressesFeature>()!.Addresses.First();
                using var client = new HttpClient { BaseAddress = new Uri(address) };

                string CreateToken(int userId, string role)
                {
                    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
                    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
                    var token = new JwtSecurityToken(
                        issuer: jwtIssuer,
                        audience: jwtIssuer,
                        claims: new[]
                        {
                            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                            new Claim(ClaimTypes.Role, role)
                        },
                        expires: DateTime.UtcNow.AddHours(1),
                        signingCredentials: creds);
                    return new JwtSecurityTokenHandler().WriteToken(token);
                }

                // Round 5: real JWT/HTTP checks for scoped entity selection and forged IDs.
                db.ProcuringEntityMembers.Add(new ProcuringEntityMember { UserId = seed.officer.Id, ProcuringEntityId = seed.peB.Id,
                    Status = MemberStatus.Active, RoleInEntity = ProcuringEntityMemberRole.Officer });
                await db.SaveChangesAsync();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateToken(seed.officer.Id, "Procurement"));
                var entitiesResp = await client.GetAsync("/api/bid-packages/creatable-entities");
                Assert.Equal(HttpStatusCode.OK, entitiesResp.StatusCode);
                using (var json = JsonDocument.Parse(await entitiesResp.Content.ReadAsStringAsync()))
                    Assert.Equal(2, json.RootElement.GetProperty("data").GetArrayLength());
                Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/bid-packages/ownership-review")).StatusCode);
                async Task<HttpResponseMessage> CreatePackage(int? entityId, string code) => await client.PostAsync("/api/bid-packages",
                    new StringContent(JsonSerializer.Serialize(new { name = "R5 HTTP package", code, type = 0, budget = 1000000,
                        deadline = DateTime.UtcNow.AddDays(20), procuringEntityId = entityId }), Encoding.UTF8, "application/json"));
                Assert.Equal(HttpStatusCode.BadRequest, (await CreatePackage(null, "R5-HTTP-MISSING")).StatusCode);
                Assert.Equal(HttpStatusCode.Forbidden, (await CreatePackage(999999, "R5-HTTP-FORGED")).StatusCode);
                var created = await CreatePackage(seed.peB.Id, "R5-HTTP-B");
                Assert.Equal(HttpStatusCode.OK, created.StatusCode);
                using (var json = JsonDocument.Parse(await created.Content.ReadAsStringAsync()))
                    Assert.Equal(seed.peB.Id, json.RootElement.GetProperty("data").GetProperty("procuringEntityId").GetInt32());
                await using var revokeSelectionDb = CreateDbContext();
                var bMember = await revokeSelectionDb.ProcuringEntityMembers.SingleAsync(m => m.UserId == seed.officer.Id && m.ProcuringEntityId == seed.peB.Id);
                bMember.Status = MemberStatus.Revoked;
                await revokeSelectionDb.SaveChangesAsync();
                Assert.Equal(HttpStatusCode.Forbidden, (await CreatePackage(seed.peB.Id, "R5-HTTP-REVOKED")).StatusCode);
                client.DefaultRequestHeaders.Authorization = null;

                var url = $"/api/contracts/milestones/{seed.milestoneA.Id}/acceptances/{seed.acceptanceA.Id}/document/download";

                // 1. Unauthenticated HTTP GET -> 401 Unauthorized from ASP.NET Core JwtBearer middleware
                var unauthResp = await client.GetAsync(url);
                Assert.Equal(HttpStatusCode.Unauthorized, unauthResp.StatusCode);

                // 2. Outsider Procurement user (Entity B) -> 403 Forbidden
                using (var req = new HttpRequestMessage(HttpMethod.Get, url))
                {
                    req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", CreateToken(seed.outsider.Id, "Procurement"));
                    var resp = await client.SendAsync(req);
                    Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
                }

                // 3. Active Signer of Entity A -> 200 OK with exact PDF bytes
                using (var req = new HttpRequestMessage(HttpMethod.Get, url))
                {
                    req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", CreateToken(seed.signer.Id, "Procurement"));
                    var resp = await client.SendAsync(req);
                    Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
                    Assert.Equal("application/pdf", resp.Content.Headers.ContentType?.MediaType);
                    var body = await resp.Content.ReadAsStringAsync();
                    Assert.Equal("DUMMY CONTRACT SCAN CONTENT A", body);
                }

                // 4. Owning Contractor -> 200 OK
                using (var req = new HttpRequestMessage(HttpMethod.Get, url))
                {
                    req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", CreateToken(seed.contractorUser.Id, "Contractor"));
                    var resp = await client.SendAsync(req);
                    Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
                }

                // 5. Tamper with snapshot file on disk -> 409 Conflict
                await File.WriteAllTextAsync(seed.acceptanceA.SnapshotDocumentPath!, "TAMPERED BYTES ON DISK");
                using (var req = new HttpRequestMessage(HttpMethod.Get, url))
                {
                    req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", CreateToken(seed.signer.Id, "Procurement"));
                    var resp = await client.SendAsync(req);
                    Assert.Equal(HttpStatusCode.Conflict, resp.StatusCode);
                }
            }
            finally
            {
                await app.StopAsync();
            }
        }

        /// <summary>
        /// R4-01 & R4-02: Kiểm chứng gắn ProcuringEntityId ổn định khi tạo BidPackage (đơn vị đơn, đa đơn vị, Officer tạo gói)
        /// và xác nhận kỹ thuật không bị lỗi cycle Include trong no-tracking query.
        /// </summary>
        [Fact]
        public async Task R4_01_02_CreateBidPackage_And_OfficerMovesEntity_PreservesManagingEntityOwnership()
        {
            await EnsureSqlServerAvailableAsync();

            await using var db = CreateDbContext();
            await db.Database.EnsureDeletedAsync();
            await db.Database.MigrateAsync();

            var seed = await SeedBaseDataAsync(db);
            var bpService = new BidPackageService(new UnitOfWork(db), new FileStorageService(), null!, new FakeNotificationService());

            // 1. Officer belongs only to Entity A -> CreateBidPackageAsync automatically binds ProcuringEntityId = peA.Id
            var createSingleRes = await bpService.CreateBidPackageAsync(new CreateBidPackageRequest
            {
                Name = "Goi thau do Officer A tao",
                Type = BidPackageType.Construction,
                Budget = 300_000_000m,
                Deadline = DateTime.UtcNow.AddDays(20),
                Description = "Test R4-01 single entity"
            }, seed.officer.Id);
            Assert.True(createSingleRes.Success);
            Assert.Equal(seed.peA.Id, createSingleRes.Data!.ProcuringEntityId);

            // 2. Add Officer as active member of Entity B as well (now active in 2 entities: peA and peB)
            db.ProcuringEntityMembers.Add(new ProcuringEntityMember
            {
                ProcuringEntityId = seed.peB.Id,
                UserId = seed.officer.Id,
                RoleInEntity = ProcuringEntityMemberRole.Officer,
                Status = MemberStatus.Active
            });
            await db.SaveChangesAsync();

            // 2a. Creating package without explicit ProcuringEntityId when active in 2 entities -> 400 Bad Request
            var createAmbiguousRes = await bpService.CreateBidPackageAsync(new CreateBidPackageRequest
            {
                Name = "Goi thau mo ho don vi",
                Type = BidPackageType.Goods,
                Budget = 150_000_000m,
                Deadline = DateTime.UtcNow.AddDays(20)
            }, seed.officer.Id);
            Assert.False(createAmbiguousRes.Success);
            Assert.Equal(400, createAmbiguousRes.StatusCode);

            // 2b. Creating package with explicit ProcuringEntityId = peB.Id -> 200 OK and bound to peB.Id
            var createExplicitRes = await bpService.CreateBidPackageAsync(new CreateBidPackageRequest
            {
                Name = "Goi thau chon ro Don vi B",
                Type = BidPackageType.Goods,
                Budget = 150_000_000m,
                Deadline = DateTime.UtcNow.AddDays(20),
                ProcuringEntityId = seed.peB.Id
            }, seed.officer.Id);
            Assert.True(createExplicitRes.Success);
            Assert.Equal(seed.peB.Id, createExplicitRes.Data!.ProcuringEntityId);

            // 2c. Outsider (only in peB) tries to create package spoofing ProcuringEntityId = peA.Id -> 403 Forbidden
            var createSpoofRes = await bpService.CreateBidPackageAsync(new CreateBidPackageRequest
            {
                Name = "Goi thau gia mao Don vi A",
                Type = BidPackageType.Goods,
                Budget = 150_000_000m,
                Deadline = DateTime.UtcNow.AddDays(20),
                ProcuringEntityId = seed.peA.Id
            }, seed.outsider.Id);
            Assert.False(createSpoofRes.Success);
            Assert.Equal(403, createSpoofRes.StatusCode);

            // 3. Officer performs TechnicalReviewMilestoneAsync on packageA (created by officer, no cycle exception!)
            await using var dbReview = CreateDbContext();
            var contractService = new ContractService(new UnitOfWork(dbReview), new FileStorageService(), new FakeContractorService(), new FakeNotificationService());
            var reviewRes = await contractService.TechnicalReviewMilestoneAsync(
                seed.milestoneA.Id,
                new TechnicalReviewMilestoneRequest { IsPassed = true, Note = "Officer review r2 without cycle" },
                seed.officer.Id,
                isAdmin: false);
            Assert.True(reviewRes.Success);
            Assert.Equal(2, reviewRes.Data!.Revision);
        }

        [Fact]
        public async Task T05_RejectMilestoneAcceptance_RequiresMatchingExpectedVersion_AndEnforcesMakerChecker()
        {
            await EnsureSqlServerAvailableAsync();

            await using var db = CreateDbContext();
            await db.Database.EnsureDeletedAsync();
            await db.Database.MigrateAsync();

            var seed = await SeedBaseDataAsync(db);
            var fileStorage = new FileStorageService();
            var contractService = new ContractService(new UnitOfWork(db), fileStorage, new FakeContractorService(), new FakeNotificationService());

            // 1. Reject with mismatched ExpectedVersion -> 409 Conflict, DB state not modified!
            var rejectMismatched = await contractService.ApproveMilestoneAcceptanceAsync(
                seed.milestoneA.Id, new ApproveMilestoneAcceptanceRequest
                {
                    AcceptanceId = seed.acceptanceA.Id,
                    ExpectedRevision = seed.acceptanceA.Revision,
                    ExpectedVersion = seed.acceptanceA.MilestoneVersion + 999,
                    IsApproved = false,
                    Note = "Reject based on wrong version"
                }, seed.signer.Id, false);

            Assert.False(rejectMismatched.Success);
            Assert.Equal(409, rejectMismatched.StatusCode);

            var freshAcceptance = await db.Acceptances.FindAsync(seed.acceptanceA.Id);
            Assert.Equal(AcceptanceStatus.TechnicalApproved, freshAcceptance!.Status);

            // 2. Reject by Maker (who performed technical review) -> Blocked by Maker-Checker!
            var rejectByMaker = await contractService.ApproveMilestoneAcceptanceAsync(
                seed.milestoneA.Id, new ApproveMilestoneAcceptanceRequest
                {
                    AcceptanceId = seed.acceptanceA.Id,
                    ExpectedRevision = seed.acceptanceA.Revision,
                    ExpectedVersion = seed.acceptanceA.MilestoneVersion,
                    IsApproved = false,
                    Note = "Maker attempting to reject"
                }, seed.officer.Id, false);

            Assert.False(rejectByMaker.Success);
            Assert.Contains("Maker - Checker", rejectByMaker.Message);

            // 3. Reject with valid ExpectedVersion and valid Checker -> 200 OK, Status = Rejected
            var rejectValid = await contractService.ApproveMilestoneAcceptanceAsync(
                seed.milestoneA.Id, new ApproveMilestoneAcceptanceRequest
                {
                    AcceptanceId = seed.acceptanceA.Id,
                    ExpectedRevision = seed.acceptanceA.Revision,
                    ExpectedVersion = seed.acceptanceA.MilestoneVersion,
                    IsApproved = false,
                    Note = "Valid Checker rejecting"
                }, seed.signer.Id, false);

            Assert.True(rejectValid.Success);
            freshAcceptance = await db.Acceptances.FindAsync(seed.acceptanceA.Id);
            Assert.Equal(AcceptanceStatus.Rejected, freshAcceptance!.Status);

            var audit = await db.AuditLogs.FirstOrDefaultAsync(a => a.EntityId == seed.acceptanceA.Id && a.Action == "REJECT_MILESTONE_ACCEPTANCE");
            Assert.NotNull(audit);
            Assert.Equal(seed.signer.Id, audit.UserId);
        }

        /// <summary>
        /// R4-03 (1/7): Interleaved race approve/approve dừng ngay trước SaveChangesAsync (sau khi cả 2 giao dịch đã đọc xong quyền, version, revision).
        /// </summary>
        [Fact]
        public async Task R4_03_InterleavedRace_ApproveVsApprove_AtPreSaveContentionPoint_SingleWinner()
        {
            await EnsureSqlServerAvailableAsync();

            await using var dbSetup = CreateDbContext();
            await dbSetup.Database.EnsureDeletedAsync();
            await dbSetup.Database.MigrateAsync();

            var seed = await SeedBaseDataAsync(dbSetup);

            var gate1 = new PreSaveContentionInterceptor("UPDATE [ContractMilestones]");
            var gate2 = new PreSaveContentionInterceptor("UPDATE [ContractMilestones]");

            await using var db1 = CreateDbContext(_testConnStr, gate1);
            await using var db2 = CreateDbContext(_testConnStr, gate2);

            var service1 = new ContractService(new UnitOfWork(db1), new FileStorageService(), new FakeContractorService(), new FakeNotificationService());
            var service2 = new ContractService(new UnitOfWork(db2), new FileStorageService(), new FakeContractorService(), new FakeNotificationService());

            var task1 = Task.Run(() => service1.ApproveMilestoneAcceptanceAsync(
                seed.milestoneA.Id, new ApproveMilestoneAcceptanceRequest
                {
                    AcceptanceId = seed.acceptanceA.Id,
                    ExpectedRevision = seed.acceptanceA.Revision,
                    ExpectedVersion = seed.acceptanceA.MilestoneVersion,
                    IsApproved = true,
                    Note = "Signer 1 interleaved approve"
                }, seed.signer.Id, false));

            var task2 = Task.Run(() => service2.ApproveMilestoneAcceptanceAsync(
                seed.milestoneA.Id, new ApproveMilestoneAcceptanceRequest
                {
                    AcceptanceId = seed.acceptanceA.Id,
                    ExpectedRevision = seed.acceptanceA.Revision,
                    ExpectedVersion = seed.acceptanceA.MilestoneVersion,
                    IsApproved = true,
                    Note = "Signer 2 interleaved approve"
                }, seed.signer.Id, false));

            // Wait until BOTH transactions have read permissions/version/revision and reached the SQL UPDATE command
            await Task.WhenAll(
                gate1.ReachedPreSave.Task.WaitAsync(TimeSpan.FromSeconds(10)),
                gate2.ReachedPreSave.Task.WaitAsync(TimeSpan.FromSeconds(10)));

            // Release both to execute their SQL UPDATE batch concurrently
            gate1.ResumeSave.TrySetResult(true);
            gate2.ResumeSave.TrySetResult(true);

            var results = await Task.WhenAll(task1, task2);

            Assert.Equal(1, results.Count(r => r.Success));
            Assert.Equal(1, results.Count(r => !r.Success && r.StatusCode == 409));

            await using var verifyDb = CreateDbContext();
            var finalMilestone = await verifyDb.ContractMilestones.FindAsync(seed.milestoneA.Id);
            Assert.Equal(MilestoneStatus.Completed, finalMilestone!.Status);
            Assert.Equal(2, finalMilestone.Version);
        }

        /// <summary>
        /// R4-03 (2/7): Interleaved race approve vs revoke membership ngay sau khi transaction 1 đã đọc currentMember = Active nhưng trước SaveChangesAsync.
        /// </summary>
        [Fact]
        public async Task R4_03_InterleavedRace_ApproveVsRevokeMembership_AtPreSaveContentionPoint_BlocksApprove()
        {
            await EnsureSqlServerAvailableAsync();

            await using var dbSetup = CreateDbContext();
            await dbSetup.Database.EnsureDeletedAsync();
            await dbSetup.Database.MigrateAsync();

            var seed = await SeedBaseDataAsync(dbSetup);
            var gate = new PreSaveContentionInterceptor("UPDATE [ContractMilestones]");

            await using var dbApprove = CreateDbContext(_testConnStr, gate);
            await using var dbRevoke = CreateDbContext(_testConnStr);

            var contractService = new ContractService(new UnitOfWork(dbApprove), new FileStorageService(), new FakeContractorService(), new FakeNotificationService());
            var memberService = new ProcuringEntityMemberService(dbRevoke, new FakeEmailService(), new ConfigurationManager(), NullLogger<ProcuringEntityMemberService>.Instance);

            var approveTask = Task.Run(() => contractService.ApproveMilestoneAcceptanceAsync(
                seed.milestoneA.Id, new ApproveMilestoneAcceptanceRequest
                {
                    AcceptanceId = seed.acceptanceA.Id,
                    ExpectedRevision = seed.acceptanceA.Revision,
                    ExpectedVersion = seed.acceptanceA.MilestoneVersion,
                    IsApproved = true,
                    Note = "Approve paused after reading active membership"
                }, seed.signer.Id, false));

            // Wait until approveTask has verified currentMember.Status == Active and is paused at SaveChangesAsync
            await gate.ReachedPreSave.Task.WaitAsync(TimeSpan.FromSeconds(10));

            // Connection 2 revokes signer's membership and commits
            var memberRow = await dbRevoke.ProcuringEntityMembers.FirstAsync(m => m.UserId == seed.signer.Id && m.ProcuringEntityId == seed.peA.Id);
            var revokeRes = await memberService.RevokeMemberAsync(seed.peA.Id, memberRow.Id, seed.manager.Id, false);
            Assert.True(revokeRes.Success);

            // Resume approveTask -> RowVersion conflict on ProcuringEntityMembers triggers 409 Conflict & rollback!
            gate.ResumeSave.TrySetResult(true);
            var approveRes = await approveTask;

            Assert.False(approveRes.Success);
            Assert.Equal(409, approveRes.StatusCode);

            await using var verifyDb = CreateDbContext();
            var mRow = await verifyDb.ContractMilestones.FindAsync(seed.milestoneA.Id);
            Assert.Equal(MilestoneStatus.Pending, mRow!.Status);
            Assert.Equal(1, mRow.Version);
        }

        /// <summary>
        /// R4-03 (3/7): Interleaved race approve vs revoke signing authority ngay sau khi transaction 1 đã đọc activeAuthority = Active nhưng trước SaveChangesAsync.
        /// </summary>
        [Fact]
        public async Task R4_03_InterleavedRace_ApproveVsRevokeAuthority_AtPreSaveContentionPoint_BlocksApprove()
        {
            await EnsureSqlServerAvailableAsync();

            await using var dbSetup = CreateDbContext();
            await dbSetup.Database.EnsureDeletedAsync();
            await dbSetup.Database.MigrateAsync();

            var seed = await SeedBaseDataAsync(dbSetup);
            var gate = new PreSaveContentionInterceptor("UPDATE [ContractMilestones]");

            await using var dbApprove = CreateDbContext(_testConnStr, gate);
            await using var dbRevoke = CreateDbContext(_testConnStr);

            var contractService = new ContractService(new UnitOfWork(dbApprove), new FileStorageService(), new FakeContractorService(), new FakeNotificationService());
            var authorityService = new SigningAuthorityService(dbRevoke, NullLogger<SigningAuthorityService>.Instance);

            var approveTask = Task.Run(() => contractService.ApproveMilestoneAcceptanceAsync(
                seed.milestoneA.Id, new ApproveMilestoneAcceptanceRequest
                {
                    AcceptanceId = seed.acceptanceA.Id,
                    ExpectedRevision = seed.acceptanceA.Revision,
                    ExpectedVersion = seed.acceptanceA.MilestoneVersion,
                    IsApproved = true,
                    Note = "Approve paused after reading active authority"
                }, seed.signer.Id, false));

            await gate.ReachedPreSave.Task.WaitAsync(TimeSpan.FromSeconds(10));

            // Connection 2 revokes signing authority and commits
            var revokeAuthRes = await authorityService.RevokeAuthorityAsync(
                seed.authorityA.Id,
                seed.manager.Id,
                new RevokeSigningAuthorityRequest { RevocationReason = "Thu hoi khan cap uy quyen ky duyet giai ngan" });
            Assert.Equal(SigningAuthorityStatus.Revoked, revokeAuthRes.Status);

            gate.ResumeSave.TrySetResult(true);
            var approveRes = await approveTask;

            Assert.False(approveRes.Success);
            Assert.Equal(409, approveRes.StatusCode);

            await using var verifyDb = CreateDbContext();
            var mRow = await verifyDb.ContractMilestones.FindAsync(seed.milestoneA.Id);
            Assert.Equal(MilestoneStatus.Pending, mRow!.Status);
            Assert.Equal(1, mRow.Version);
        }

        /// <summary>
        /// R4-03 (4/7): Interleaved race approve vs edit milestone ngay sau khi transaction 1 đã đọc milestone v1 nhưng trước SaveChangesAsync.
        /// </summary>
        [Fact]
        public async Task R4_03_InterleavedRace_ApproveVsEditMilestone_AtPreSaveContentionPoint_BlocksApprove()
        {
            await EnsureSqlServerAvailableAsync();

            await using var dbSetup = CreateDbContext();
            await dbSetup.Database.EnsureDeletedAsync();
            await dbSetup.Database.MigrateAsync();

            var seed = await SeedBaseDataAsync(dbSetup);
            var gate = new PreSaveContentionInterceptor("UPDATE [ContractMilestones]");

            await using var dbApprove = CreateDbContext(_testConnStr, gate);
            await using var dbEdit = CreateDbContext(_testConnStr);

            var approveService = new ContractService(new UnitOfWork(dbApprove), new FileStorageService(), new FakeContractorService(), new FakeNotificationService());
            var editService = new ContractService(new UnitOfWork(dbEdit), new FileStorageService(), new FakeContractorService(), new FakeNotificationService());

            var approveTask = Task.Run(() => approveService.ApproveMilestoneAcceptanceAsync(
                seed.milestoneA.Id, new ApproveMilestoneAcceptanceRequest
                {
                    AcceptanceId = seed.acceptanceA.Id,
                    ExpectedRevision = seed.acceptanceA.Revision,
                    ExpectedVersion = seed.acceptanceA.MilestoneVersion,
                    IsApproved = true,
                    Note = "Approve paused before milestone edit"
                }, seed.signer.Id, false));

            await gate.ReachedPreSave.Task.WaitAsync(TimeSpan.FromSeconds(10));

            // Connection 2 edits milestone content and commits (incrementing Version to 2 and invalidating acceptanceA)
            var editRes = await editService.UpdateMilestoneAsync(
                seed.contractA.Id,
                seed.milestoneA.Id,
                new CreateMilestoneRequest
                {
                    Title = "Moc 1: Thay doi khoi luong khi dang duyet",
                    Amount = 120_000_000m,
                    DueDate = seed.milestoneA.DueDate
                },
                seed.officer.Id,
                isAdmin: false);
            Assert.True(editRes.Success);

            gate.ResumeSave.TrySetResult(true);
            var approveRes = await approveTask;

            Assert.False(approveRes.Success);
            Assert.Equal(409, approveRes.StatusCode);

            await using var verifyDb = CreateDbContext();
            var mRow = await verifyDb.ContractMilestones.FindAsync(seed.milestoneA.Id);
            Assert.Equal(MilestoneStatus.Pending, mRow!.Status);
            Assert.Equal(2, mRow.Version);
            Assert.Equal(120_000_000m, mRow.Amount);

            var aRow = await verifyDb.Acceptances.FindAsync(seed.acceptanceA.Id);
            Assert.Equal(AcceptanceStatus.ReviewInvalidated, aRow!.Status);
        }

        /// <summary>
        /// R4-03 (5/7): Interleaved race approve vs upload scanned contract ngay sau khi transaction 1 đã kiểm tra hash nhưng trước SaveChangesAsync.
        /// </summary>
        [Fact]
        public async Task R4_03_InterleavedRace_ApproveVsUploadScan_AtPreSaveContentionPoint_BlocksApprove()
        {
            await EnsureSqlServerAvailableAsync();

            await using var dbSetup = CreateDbContext();
            await dbSetup.Database.EnsureDeletedAsync();
            await dbSetup.Database.MigrateAsync();

            var seed = await SeedBaseDataAsync(dbSetup);
            var gate = new PreSaveContentionInterceptor("UPDATE [ContractMilestones]");

            await using var dbApprove = CreateDbContext(_testConnStr, gate);
            await using var dbUpload = CreateDbContext(_testConnStr);

            var approveService = new ContractService(new UnitOfWork(dbApprove), new FileStorageService(), new FakeContractorService(), new FakeNotificationService());
            var uploadService = new ContractService(new UnitOfWork(dbUpload), new FileStorageService(), new FakeContractorService(), new FakeNotificationService());

            var approveTask = Task.Run(() => approveService.ApproveMilestoneAcceptanceAsync(
                seed.milestoneA.Id, new ApproveMilestoneAcceptanceRequest
                {
                    AcceptanceId = seed.acceptanceA.Id,
                    ExpectedRevision = seed.acceptanceA.Revision,
                    ExpectedVersion = seed.acceptanceA.MilestoneVersion,
                    IsApproved = true,
                    Note = "Approve paused before scan replacement"
                }, seed.signer.Id, false));

            await gate.ReachedPreSave.Task.WaitAsync(TimeSpan.FromSeconds(10));

            // Connection 2 uploads a new scanned contract PDF
            var pdfBytes = Encoding.UTF8.GetBytes("%PDF-1.4 NEW CONTRACT SCAN CONTENT");
            await using var ms = new MemoryStream(pdfBytes);
            var formFile = new FormFile(ms, 0, pdfBytes.Length, "file", "new_scan.pdf")
            {
                Headers = new HeaderDictionary(),
                ContentType = "application/pdf"
            };
            var uploadRes = await uploadService.UploadScannedContractAsync(seed.contractA.Id, formFile, seed.officer.Id, isAdmin: false);
            Assert.True(uploadRes.Success);

            gate.ResumeSave.TrySetResult(true);
            var approveRes = await approveTask;

            Assert.False(approveRes.Success);
            Assert.Equal(409, approveRes.StatusCode);

            await using var verifyDb = CreateDbContext();
            var mRow = await verifyDb.ContractMilestones.FindAsync(seed.milestoneA.Id);
            Assert.Equal(MilestoneStatus.Pending, mRow!.Status);
            Assert.Equal(2, mRow.Version);
        }

        /// <summary>
        /// R4-03 (6/7): Interleaved race review/review dừng ngay sau khi cả 2 Cán bộ kỹ thuật đã đọc existingAcceptances (cùng tính nextRevision = 2) nhưng trước SaveChangesAsync.
        /// Đảm bảo chỉ 1 bên thắng và không bao giờ sinh trùng Revision cho cùng Milestone.
        /// </summary>
        [Fact]
        public async Task R4_03_InterleavedRace_ReviewVsReview_AtPreSaveContentionPoint_NoDuplicateRevision()
        {
            await EnsureSqlServerAvailableAsync();

            await using var dbSetup = CreateDbContext();
            await dbSetup.Database.EnsureDeletedAsync();
            await dbSetup.Database.MigrateAsync();

            var seed = await SeedBaseDataAsync(dbSetup);

            var gate1 = new PreSaveContentionInterceptor("INSERT INTO [Acceptances]");
            var gate2 = new PreSaveContentionInterceptor("INSERT INTO [Acceptances]");

            await using var db1 = CreateDbContext(_testConnStr, gate1);
            await using var db2 = CreateDbContext(_testConnStr, gate2);

            var service1 = new ContractService(new UnitOfWork(db1), new FileStorageService(), new FakeContractorService(), new FakeNotificationService());
            var service2 = new ContractService(new UnitOfWork(db2), new FileStorageService(), new FakeContractorService(), new FakeNotificationService());

            var task1 = Task.Run(() => service1.TechnicalReviewMilestoneAsync(
                seed.milestoneA.Id,
                new TechnicalReviewMilestoneRequest { IsPassed = true, Note = "Concurrent review 1" },
                seed.officer.Id,
                isAdmin: false));

            var task2 = Task.Run(() => service2.TechnicalReviewMilestoneAsync(
                seed.milestoneA.Id,
                new TechnicalReviewMilestoneRequest { IsPassed = true, Note = "Concurrent review 2" },
                seed.officer.Id,
                isAdmin: false));

            await Task.WhenAll(
                gate1.ReachedPreSave.Task.WaitAsync(TimeSpan.FromSeconds(10)),
                gate2.ReachedPreSave.Task.WaitAsync(TimeSpan.FromSeconds(10)));

            gate1.ResumeSave.TrySetResult(true);
            gate2.ResumeSave.TrySetResult(true);

            var results = await Task.WhenAll(task1, task2);

            Assert.Equal(1, results.Count(r => r.Success));
            Assert.Equal(1, results.Count(r => !r.Success && r.StatusCode == 409));

            await using var verifyDb = CreateDbContext();
            var acceptances = await verifyDb.Acceptances
                .Where(a => a.MilestoneId == seed.milestoneA.Id)
                .OrderBy(a => a.Revision)
                .ToListAsync();
            Assert.Equal(2, acceptances.Count);
            Assert.Equal(new[] { 1, 2 }, acceptances.Select(a => a.Revision).ToArray());
        }

        /// <summary>
        /// R4-03 (7/7): Kiểm chứng rollback nguyên tử ở mức DB Command khi lệnh UPDATE nghiệp vụ đã chạy thật trên SQL Server
        /// nhưng lệnh INSERT INTO [AuditLogs] tiếp theo trong cùng transaction bị lỗi.
        /// </summary>
        [Fact]
        public async Task R4_03_SqlServer_AuditCommandFailureAfterBusinessSqlWrite_RollsBackEntireTransaction()
        {
            await EnsureSqlServerAvailableAsync();

            await using var dbSetup = CreateDbContext();
            await dbSetup.Database.EnsureDeletedAsync();
            await dbSetup.Database.MigrateAsync();

            var seed = await SeedBaseDataAsync(dbSetup);

            var auditFaultInterceptor = new AuditCommandFailureInterceptor();
            await using var failingDb = CreateDbContext(_testConnStr, auditFaultInterceptor);
            var contractService = new ContractService(new UnitOfWork(failingDb), new FileStorageService(), new FakeContractorService(), new FakeNotificationService());

            await Assert.ThrowsAnyAsync<Exception>(async () =>
            {
                await contractService.ApproveMilestoneAcceptanceAsync(
                    seed.milestoneA.Id, new ApproveMilestoneAcceptanceRequest
                    {
                        AcceptanceId = seed.acceptanceA.Id,
                        ExpectedRevision = seed.acceptanceA.Revision,
                        ExpectedVersion = seed.acceptanceA.MilestoneVersion,
                        IsApproved = true,
                        Note = "Attempt with DB-level AuditLog INSERT failure after business UPDATE"
                    }, seed.signer.Id, false);
            });

            // Confirm business SQL UPDATE actually executed on SQL Server before AuditLog INSERT threw
            Assert.True(auditFaultInterceptor.BusinessUpdateCommandsExecuted > 0);
            Assert.True(auditFaultInterceptor.AuditInsertInterceptedAndFailed);

            // Confirm in a fresh context that the SQL Server transaction rolled back all business writes!
            await using var verifyDb = CreateDbContext();
            var mRow = await verifyDb.ContractMilestones.FindAsync(seed.milestoneA.Id);
            Assert.Equal(MilestoneStatus.Pending, mRow!.Status);
            Assert.Equal(1, mRow.Version);

            var aRow = await verifyDb.Acceptances.FindAsync(seed.acceptanceA.Id);
            Assert.Equal(AcceptanceStatus.TechnicalApproved, aRow!.Status);
            Assert.Null(aRow.ApprovedAt);

            var auditCount = await verifyDb.AuditLogs.CountAsync(a => a.EntityId == seed.acceptanceA.Id && a.Action == "APPROVE_MILESTONE_ACCEPTANCE");
            Assert.Equal(0, auditCount);
        }

        /// <summary>
        /// Test tuần tự (đặt tên đúng bản chất): Sau khi thu hồi tư cách thành viên, yêu cầu phê duyệt mới bị chặn ngay lập tức (403 Forbidden).
        /// </summary>
        [Fact]
        public async Task T06_Sequential_RevokeMemberThenApprove_BlocksApproveWith403()
        {
            await EnsureSqlServerAvailableAsync();

            await using var dbSetup = CreateDbContext();
            await dbSetup.Database.EnsureDeletedAsync();
            await dbSetup.Database.MigrateAsync();

            var seed = await SeedBaseDataAsync(dbSetup);

            await using var dbApprove = CreateDbContext();
            await using var dbRevoke = CreateDbContext();

            var memberService = new ProcuringEntityMemberService(dbRevoke, new FakeEmailService(), new ConfigurationManager(), NullLogger<ProcuringEntityMemberService>.Instance);
            var contractService = new ContractService(new UnitOfWork(dbApprove), new FileStorageService(), new FakeContractorService(), new FakeNotificationService());

            var memberRow = await dbRevoke.ProcuringEntityMembers.FirstAsync(m => m.UserId == seed.signer.Id && m.ProcuringEntityId == seed.peA.Id);
            var revokeRes = await memberService.RevokeMemberAsync(seed.peA.Id, memberRow.Id, seed.manager.Id, false);
            Assert.True(revokeRes.Success);

            var approveRes = await contractService.ApproveMilestoneAcceptanceAsync(
                seed.milestoneA.Id, new ApproveMilestoneAcceptanceRequest
                {
                    AcceptanceId = seed.acceptanceA.Id,
                    ExpectedRevision = seed.acceptanceA.Revision,
                    ExpectedVersion = seed.acceptanceA.MilestoneVersion,
                    IsApproved = true,
                    Note = "Sequential approval after member revoke"
                }, seed.signer.Id, false);

            Assert.False(approveRes.Success);
            Assert.Equal(403, approveRes.StatusCode);
        }

        /// <summary>
        /// R4-04: Kiểm chứng đường chạy migration thật qua IMigrator:
        /// 1. Migrate DB mới tới 20261028094453_AddTransparencyControlsIntegrityFixes (trước 20261029160000 và 20261029173000).
        /// 2. Seed dữ liệu legacy (Version = 0, Acceptance thiếu hash, gói thầu rõ nguồn gốc vs gói thầu mơ hồ nguồn gốc).
        /// 3. Chạy migrator.MigrateAsync() để áp dụng 2 migration forward.
        /// 4. Assert dữ liệu được sửa đúng policy và __EFMigrationsHistory ghi nhận đầy đủ.
        /// </summary>
        [Fact]
        public async Task T03_RealMigratorUpgrade_From20261028094453_ToLatest_RepairsLegacyDataAndBackfillsDeterministicOwnership()
        {
            await EnsureSqlServerAvailableAsync();

            await using var db = CreateDbContext();
            await db.Database.EnsureDeletedAsync();

            var migrator = db.Database.GetService<IMigrator>();

            // Step 1: Migrate only up to 20261028094453_AddTransparencyControlsIntegrityFixes
            await migrator.MigrateAsync("20261028094453_AddTransparencyControlsIntegrityFixes");

            var appliedBefore = (await db.Database.GetAppliedMigrationsAsync()).ToList();
            Assert.Contains("20261028094453_AddTransparencyControlsIntegrityFixes", appliedBefore);
            Assert.DoesNotContain("20261029160000_FixTransparencyControlsIntegrityUpgradeAndMemberConcurrency", appliedBefore);
            Assert.DoesNotContain("20261029173000_AddBidPackageProcuringEntityOwnership", appliedBefore);

            // Step 2: Seed legacy state at pre-R3/R4 schema using raw SQL
            await db.Database.ExecuteSqlRawAsync(@"
                INSERT INTO [Users] ([FullName], [Email], [PasswordHash], [IsActive], [MustChangePassword], [CreatedAt])
                VALUES
                    (N'Owner A', N'legacy_ownerA@test.invalid', N'hash', 1, 0, SYSUTCDATETIME()),
                    (N'Owner B', N'legacy_ownerB@test.invalid', N'hash', 1, 0, SYSUTCDATETIME()),
                    (N'Moved Officer', N'legacy_moved@test.invalid', N'hash', 1, 0, SYSUTCDATETIME()),
                    (N'Contractor Legacy', N'legacy_ctr@test.invalid', N'hash', 1, 0, SYSUTCDATETIME());

                DECLARE @OwnerAId INT = (SELECT [Id] FROM [Users] WHERE [Email] = N'legacy_ownerA@test.invalid');
                DECLARE @OwnerBId INT = (SELECT [Id] FROM [Users] WHERE [Email] = N'legacy_ownerB@test.invalid');
                DECLARE @MovedOfficerId INT = (SELECT [Id] FROM [Users] WHERE [Email] = N'legacy_moved@test.invalid');
                DECLARE @CtrUserId INT = (SELECT [Id] FROM [Users] WHERE [Email] = N'legacy_ctr@test.invalid');

                INSERT INTO [ProcuringEntities] ([UserId], [OrganizationName], [OrganizationType], [TaxCode], [RepresentativeName], [RepresentativeTitle], [EstablishmentDecisionFile], [IsDigitalSigned], [VerificationStatus], [CreatedAt])
                VALUES
                    (@OwnerAId, N'Legacy Entity A', N'DoanhNghiep', N'0108888001', N'Owner A', N'Giam Doc', N'a.pdf', 0, N'Approved', SYSUTCDATETIME()),
                    (@OwnerBId, N'Legacy Entity B', N'DoanhNghiep', N'0108888002', N'Owner B', N'Giam Doc', N'b.pdf', 0, N'Approved', SYSUTCDATETIME());

                DECLARE @PeAId INT = (SELECT [Id] FROM [ProcuringEntities] WHERE [TaxCode] = N'0108888001');
                DECLARE @PeBId INT = (SELECT [Id] FROM [ProcuringEntities] WHERE [TaxCode] = N'0108888002');

                -- OwnerA only ever belonged to PeA (deterministic)
                -- MovedOfficer belonged to PeA (Revoked) and PeB (Active) -> ambiguous!
                INSERT INTO [ProcuringEntityMembers] ([ProcuringEntityId], [UserId], [RoleInEntity], [Status], [JoinedAt])
                VALUES
                    (@PeAId, @OwnerAId, 1, 1, SYSUTCDATETIME()),
                    (@PeAId, @MovedOfficerId, 2, 2, SYSUTCDATETIME()),
                    (@PeBId, @MovedOfficerId, 2, 1, SYSUTCDATETIME());

                INSERT INTO [Contractors] ([UserId], [CompanyName], [TaxCode], [Address], [Rating], [VerificationStatus], [CreatedAt])
                VALUES (@CtrUserId, N'Legacy Contractor Co', N'0107777001', N'Hanoi', 5.0, N'Approved', SYSUTCDATETIME());
                DECLARE @ContractorId INT = SCOPE_IDENTITY();

                -- Package 1: Created by OwnerA (deterministic provenance -> should backfill ProcuringEntityId = @PeAId)
                -- Package 2: Created by MovedOfficer (ambiguous provenance -> must remain ProcuringEntityId = NULL)
                INSERT INTO [BidPackages] ([Code], [Name], [Type], [Budget], [Deadline], [Status], [CreatedBy], [CreatedAt])
                VALUES
                    (N'PKG-LEGACY-DET', N'Goi thau Legacy ro nguon goc', 1, 500000000, DATEADD(day, 30, SYSUTCDATETIME()), 5, @OwnerAId, SYSUTCDATETIME()),
                    (N'PKG-LEGACY-AMB', N'Goi thau Legacy mo ho nguon goc', 1, 500000000, DATEADD(day, 30, SYSUTCDATETIME()), 1, @MovedOfficerId, SYSUTCDATETIME());

                DECLARE @PkgDetId INT = (SELECT [Id] FROM [BidPackages] WHERE [Code] = N'PKG-LEGACY-DET');

                INSERT INTO [Contracts] ([BidPackageId], [ContractorId], [ContractNumber], [Value], [StartDate], [EndDate], [Status], [CreatedAt])
                VALUES (@PkgDetId, @ContractorId, N'CTR-LEGACY-001', 200000000, SYSUTCDATETIME(), DATEADD(day, 60, SYSUTCDATETIME()), 1, SYSUTCDATETIME());
                DECLARE @ContractId INT = SCOPE_IDENTITY();

                -- Legacy milestone with Version = 0
                INSERT INTO [ContractMilestones] ([ContractId], [Title], [Amount], [DueDate], [Status], [Version])
                VALUES (@ContractId, N'Moc Legacy v0', 100000000, DATEADD(day, 15, SYSUTCDATETIME()), 0, 0);
                DECLARE @MilestoneId INT = SCOPE_IDENTITY();

                -- Legacy acceptance in TechnicalApproved (Status = 3) with MilestoneVersion = 0, Revision = 0, and missing SnapshotDocumentHash
                INSERT INTO [Acceptances] ([ContractId], [MilestoneId], [Revision], [MilestoneVersion], [TechnicalReviewedBy], [TechnicalReviewedAt], [IsTechnicalPassed], [Status], [SnapshotAmount], [SnapshotTitle], [SnapshotDueDate], [SnapshotDocumentHash])
                VALUES (@ContractId, @MilestoneId, 0, 0, @OwnerAId, SYSUTCDATETIME(), 1, 3, 100000000, N'Moc Legacy v0', DATEADD(day, 15, SYSUTCDATETIME()), NULL);
            ");

            // Step 3: Run the actual EF Core forward migrations to latest!
            await migrator.MigrateAsync();

            // Step 4: Verify __EFMigrationsHistory and repaired data
            var appliedAfter = (await db.Database.GetAppliedMigrationsAsync()).ToList();
            Assert.Contains("20261029160000_FixTransparencyControlsIntegrityUpgradeAndMemberConcurrency", appliedAfter);
            Assert.Contains("20261029173000_AddBidPackageProcuringEntityOwnership", appliedAfter);

            await using var verifyDb = CreateDbContext();
            var repairedMilestone = await verifyDb.ContractMilestones.AsNoTracking().SingleAsync();
            Assert.Equal(1, repairedMilestone.Version);

            var repairedAcceptance = await verifyDb.Acceptances.AsNoTracking().SingleAsync();
            Assert.Equal(1, repairedAcceptance.MilestoneVersion);
            Assert.Equal(1, repairedAcceptance.Revision);
            Assert.Equal(AcceptanceStatus.ReviewInvalidated, repairedAcceptance.Status);
            Assert.False(repairedAcceptance.IsTechnicalPassed);
            Assert.Contains("[HỆ THỐNG YÊU CẦU THẨM TRA LẠI]", repairedAcceptance.Note);

            var members = await verifyDb.ProcuringEntityMembers.AsNoTracking().ToListAsync();
            Assert.All(members, m => Assert.True(m.RowVersion != null && m.RowVersion.Length == 8));

            var peA = await verifyDb.ProcuringEntities.AsNoTracking().SingleAsync(p => p.TaxCode == "0108888001");
            var pkgDet = await verifyDb.BidPackages.AsNoTracking().SingleAsync(p => p.Code == "PKG-LEGACY-DET");
            var pkgAmb = await verifyDb.BidPackages.AsNoTracking().SingleAsync(p => p.Code == "PKG-LEGACY-AMB");

            Assert.Null(pkgDet.ProcuringEntityId); // Owner/member identity alone is not historical provenance.
            Assert.True(pkgDet.OwnershipNeedsReview);
            Assert.Null(pkgAmb.ProcuringEntityId); // Ambiguous legacy package stays NULL (fail-closed)
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task R5_WriteVsRevokeMembership_RollsBackBusinessAndFile(bool upload)
        {
            await EnsureSqlServerAvailableAsync();
            await using var setup = CreateDbContext();
            await setup.Database.MigrateAsync();
            var seed = await SeedBaseDataAsync(setup);
            var gate = new PreSaveContentionInterceptor("UPDATE [");
            await using var writeDb = CreateDbContext(null, gate);
            await using var revokeDb = CreateDbContext();
            var writer = new ContractService(new UnitOfWork(writeDb), new FileStorageService(), new FakeContractorService(), new FakeNotificationService());
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes("%PDF-1.4 R5 new scan"));
            Task<int> writeTask = upload ? Upload() : Edit();
            async Task<int> Upload() => (await writer.UploadScannedContractAsync(seed.contractA.Id,
                new FormFile(stream, 0, stream.Length, "file", "race.pdf"), seed.officer.Id, false)).StatusCode;
            async Task<int> Edit() => (await writer.UpdateMilestoneAsync(seed.contractA.Id, seed.milestoneA.Id,
                new() { Title = "R5 race edit", Amount = seed.milestoneA.Amount, DueDate = seed.milestoneA.DueDate }, seed.officer.Id, false)).StatusCode;
            await gate.ReachedPreSave.Task.WaitAsync(TimeSpan.FromSeconds(10));
            try
            {
                var members = new ProcuringEntityMemberService(revokeDb, new FakeEmailService(), new ConfigurationManager(), NullLogger<ProcuringEntityMemberService>.Instance);
                var member = await revokeDb.ProcuringEntityMembers.SingleAsync(m => m.UserId == seed.officer.Id);
                Assert.True((await members.RevokeMemberAsync(seed.peA.Id, member.Id, seed.manager.Id, false)).Success);
            }
            finally { gate.ResumeSave.TrySetResult(true); }
            Assert.Equal(409, await writeTask);
            await using var verify = CreateDbContext();
            var milestone = await verify.ContractMilestones.AsNoTracking().SingleAsync();
            Assert.Equal(1, milestone.Version);
            Assert.Equal(seed.milestoneA.Title, milestone.Title);
            Assert.Equal(AcceptanceStatus.TechnicalApproved, (await verify.Acceptances.AsNoTracking().SingleAsync()).Status);
            Assert.Equal(seed.contractA.ScannedFilePath, (await verify.Contracts.AsNoTracking().SingleAsync()).ScannedFilePath);
        }

        [Fact]
        public async Task R5_ForwardRepair_QuarantinesOldCandidate_AndAuditedReviewRestoresScope()
        {
            await EnsureSqlServerAvailableAsync();
            await using var db = CreateDbContext();
            var migrator = db.Database.GetService<IMigrator>();
            await migrator.MigrateAsync("20261029173000_AddBidPackageProcuringEntityOwnership");
            // Simulate an already applied old backfill: preserve the assigned candidate
            // even though the only membership was created years after the package.
            await db.Database.ExecuteSqlRawAsync(@"
                INSERT INTO Users(FullName,Email,PasswordHash,IsActive,MustChangePassword,CreatedAt)
                VALUES(N'R5 Owner',N'r5-owner@test.invalid',N'hash',1,0,'2020-01-01');
                DECLARE @u int = SCOPE_IDENTITY();
                INSERT INTO ProcuringEntities(UserId,OrganizationName,OrganizationType,TaxCode,RepresentativeName,RepresentativeTitle,EstablishmentDecisionFile,IsDigitalSigned,VerificationStatus,CreatedAt)
                VALUES(@u,N'R5 Entity',N'DoanhNghiep',N'R5-REPAIR',N'Owner',N'Director',N'evidence.pdf',0,N'Approved',SYSUTCDATETIME());
                DECLARE @pe int = SCOPE_IDENTITY();
                INSERT INTO ProcuringEntityMembers(ProcuringEntityId,UserId,RoleInEntity,Status,JoinedAt)
                VALUES(@pe,@u,0,0,SYSUTCDATETIME());
                INSERT INTO BidPackages(Code,Name,Type,Budget,Deadline,Status,CreatedBy,CreatedAt,ProcuringEntityId)
                VALUES(N'R5-OLD-CANDIDATE',N'Historical candidate',1,1000000,DATEADD(day,20,SYSUTCDATETIME()),0,@u,'2020-01-01',@pe);
            ");
            await migrator.MigrateAsync();
            var package = await db.BidPackages.AsNoTracking().SingleAsync();
            Assert.NotNull(package.ProcuringEntityId);
            Assert.True(package.OwnershipNeedsReview);
            Assert.True(await db.AuditLogs.AnyAsync(a => a.Action == "QUARANTINE_PACKAGE_OWNERSHIP" && a.EntityId == package.Id));
            var service = new BidPackageService(new UnitOfWork(db), new FileStorageService(), null!, new FakeNotificationService());
            Assert.Single((await service.GetOwnershipReviewsAsync()).Data!);
            var edit = await service.UpdateBidPackageAsync(package.Id, new() { Name = "Forbidden before review", Budget = package.Budget, Deadline = package.Deadline }, package.CreatedBy, false);
            Assert.False(edit.Success);
            var request = new ReviewPackageOwnershipRequest { ProcuringEntityId = package.ProcuringEntityId.Value,
                ExpectedProcuringEntityId = package.ProcuringEntityId, EvidenceReference = "R5/test/evidence.pdf#sha256-record", Note = "Test administrator reviewed original ownership evidence" };
            Assert.Equal(403, (await service.ReviewOwnershipAsync(package.Id, request, package.CreatedBy)).StatusCode);
            var adminRole = new Role { Name = "Admin" };
            var admin = new User { FullName = "Ownership Reviewer", Email = "r5-reviewer@test.invalid", PasswordHash = "fixture" };
            db.UserRoles.Add(new UserRole { User = admin, Role = adminRole });
            await db.SaveChangesAsync();
            Assert.True((await service.ReviewOwnershipAsync(package.Id, request, admin.Id)).Success);
            await using var verify = CreateDbContext();
            Assert.False((await verify.BidPackages.AsNoTracking().SingleAsync()).OwnershipNeedsReview);
            Assert.True(await verify.AuditLogs.AnyAsync(a => a.Action == "RESOLVE_PACKAGE_OWNERSHIP"));
            Assert.Equal(409, (await service.ReviewOwnershipAsync(package.Id, request, admin.Id)).StatusCode);
        }

        private sealed class PreSaveContentionInterceptor : DbCommandInterceptor
        {
            private readonly string _sqlMarker;
            private int _triggered;
            public TaskCompletionSource<bool> ReachedPreSave { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
            public TaskCompletionSource<bool> ResumeSave { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

            public PreSaveContentionInterceptor(string sqlMarker)
            {
                _sqlMarker = sqlMarker;
            }

            private async Task PauseIfMatchingAsync(DbCommand command)
            {
                if (command.CommandText.Contains(_sqlMarker, StringComparison.OrdinalIgnoreCase) &&
                    Interlocked.CompareExchange(ref _triggered, 1, 0) == 0)
                {
                    ReachedPreSave.TrySetResult(true);
                    await ResumeSave.Task.WaitAsync(TimeSpan.FromSeconds(15));
                }
            }

            public override async ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
                DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
            {
                await PauseIfMatchingAsync(command);
                return await base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
            }

            public override async ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
                DbCommand command, CommandEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
            {
                await PauseIfMatchingAsync(command);
                return await base.NonQueryExecutingAsync(command, eventData, result, cancellationToken);
            }
        }

        private sealed class AuditCommandFailureInterceptor : DbCommandInterceptor
        {
            public int BusinessUpdateCommandsExecuted { get; private set; }
            public bool AuditInsertInterceptedAndFailed { get; private set; }

            private void InspectCommand(DbCommand command)
            {
                if (command.CommandText.Contains("UPDATE [ContractMilestones]", StringComparison.OrdinalIgnoreCase) ||
                    command.CommandText.Contains("UPDATE [Acceptances]", StringComparison.OrdinalIgnoreCase))
                {
                    BusinessUpdateCommandsExecuted++;
                }

                if (command.CommandText.Contains("INSERT INTO [AuditLogs]", StringComparison.OrdinalIgnoreCase))
                {
                    AuditInsertInterceptedAndFailed = true;
                    throw new InvalidOperationException("DB_COMMAND_LEVEL_AUDIT_LOG_INSERT_FAILURE");
                }
            }

            public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
                DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
            {
                InspectCommand(command);
                return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
            }

            public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
                DbCommand command, CommandEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
            {
                InspectCommand(command);
                return base.NonQueryExecutingAsync(command, eventData, result, cancellationToken);
            }
        }
    }
}
