using Microsoft.AspNetCore.SignalR;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ProcurementSystem.API.BackgroundServices;
using ProcurementSystem.Core.Enums;
using ProcurementSystem.Core.Interfaces;
using ProcurementSystem.Infrastructure.Data;
using ProcurementSystem.Infrastructure.Entities;
using ProcurementSystem.Infrastructure.Hubs;
using Xunit;

namespace ProcurementSystem.Tests
{
    /// <summary>
    /// Bộ kiểm thử xác thực phạm vi người nhận cảnh báo (Notification Recipient Scope)
    /// theo Đơn vị (ProcuringEntity), vai trò thành viên (MemberStatus), trạng thái tài khoản (IsActive)
    /// và chính sách phân tách Quản trị viên (Admin) cho ContractExpiryNotificationWorker.
    /// </summary>
    public class NotificationRecipientScopeTests : IAsyncLifetime
    {
        private SqliteConnection _connection = null!;
        private DbContextOptions<AppDbContext> _dbContextOptions = null!;
        private FakeEmailServiceForRecipientTests _fakeEmailService = null!;
        private IServiceProvider _serviceProvider = null!;

        // Entities & Users Fixture
        public ProcuringEntity EntityA { get; private set; } = null!;
        public ProcuringEntity EntityB { get; private set; } = null!;
        public Contractor ContractorA { get; private set; } = null!;
        public Contractor ContractorB { get; private set; } = null!;

        public User AdminUser { get; private set; } = null!;
        public User ProcUserA { get; private set; } = null!;
        public User ProcUserB { get; private set; } = null!;
        public User DualUserAB { get; private set; } = null!;
        public User LockedUserA { get; private set; } = null!;
        public User RevokedMemberA { get; private set; } = null!;
        public User SuspendedMemberA { get; private set; } = null!;
        public User NoRoleUserA { get; private set; } = null!;
        public User MultiRoleAdminUserA { get; private set; } = null!;
        public User ContractorUserA { get; private set; } = null!;
        public User ContractorUserB { get; private set; } = null!;
        public User LockedContractorUser { get; private set; } = null!;

        public BidPackage PackageA { get; private set; } = null!;
        public BidPackage PackageB { get; private set; } = null!;
        public BidPackage PackageUnassigned { get; private set; } = null!;
        public BidPackage PackageNeedsReview { get; private set; } = null!;
        public Contract ContractA { get; private set; } = null!;
        public Contract ContractB { get; private set; } = null!;
        public ContractMilestone MilestoneA { get; private set; } = null!;
        public ContractGuarantee GuaranteeA { get; private set; } = null!;

        public async Task InitializeAsync()
        {
            _connection = new SqliteConnection("Data Source=:memory:");
            await _connection.OpenAsync();

            _dbContextOptions = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(_connection)
                .Options;

            using (var initDb = new TestDbContext(_dbContextOptions))
            {
                await initDb.Database.EnsureCreatedAsync();
            }

            _fakeEmailService = new FakeEmailServiceForRecipientTests();

            var services = new ServiceCollection();
            services.AddScoped<AppDbContext>(_ => new TestDbContext(_dbContextOptions));
            services.AddScoped<IEmailService>(_ => _fakeEmailService);
            services.AddSingleton<ILogger<ContractExpiryNotificationWorker>>(NullLogger<ContractExpiryNotificationWorker>.Instance);

            _serviceProvider = services.BuildServiceProvider();

            await SeedDataAsync();
        }

        public async Task DisposeAsync()
        {
            await _connection.DisposeAsync();
        }

        private AppDbContext CreateDbContext() => new TestDbContext(_dbContextOptions);

        private async Task SeedDataAsync()
        {
            using var db = CreateDbContext();

            // 1. Roles
            var adminRole = new Role { Name = "Admin", Description = "Platform Administrator" };
            var procRole = new Role { Name = "Procurement", Description = "Procurement Member" };
            var contractorRole = new Role { Name = "Contractor", Description = "Contractor" };
            db.Roles.AddRange(adminRole, procRole, contractorRole);
            await db.SaveChangesAsync();

            // 2. Users
            AdminUser = new User { FullName = "Platform Admin", Email = "admin@platform.com", PasswordHash = "hash", IsActive = true };
            ProcUserA = new User { FullName = "Procurement Officer A", Email = "proc_a@entity-a.com", PasswordHash = "hash", IsActive = true };
            ProcUserB = new User { FullName = "Procurement Officer B", Email = "proc_b@entity-b.com", PasswordHash = "hash", IsActive = true };
            DualUserAB = new User { FullName = "Dual Member A & B", Email = "dual@both-entities.com", PasswordHash = "hash", IsActive = true };
            LockedUserA = new User { FullName = "Locked User A", Email = "locked_a@entity-a.com", PasswordHash = "hash", IsActive = false }; // Locked
            RevokedMemberA = new User { FullName = "Revoked Member A", Email = "revoked_a@entity-a.com", PasswordHash = "hash", IsActive = true };
            SuspendedMemberA = new User { FullName = "Suspended Member A", Email = "suspended_a@entity-a.com", PasswordHash = "hash", IsActive = true };
            NoRoleUserA = new User { FullName = "No Role User A", Email = "norole_a@entity-a.com", PasswordHash = "hash", IsActive = true };
            MultiRoleAdminUserA = new User { FullName = "Admin & Procurement A", Email = "multirole_a@entity-a.com", PasswordHash = "hash", IsActive = true };
            ContractorUserA = new User { FullName = "Contractor Alpha Rep", Email = "contractor_a@alpha.com", PasswordHash = "hash", IsActive = true };
            ContractorUserB = new User { FullName = "Contractor Beta Rep", Email = "contractor_b@beta.com", PasswordHash = "hash", IsActive = true };
            LockedContractorUser = new User { FullName = "Locked Contractor Rep", Email = "locked_contractor@gamma.com", PasswordHash = "hash", IsActive = false };

            db.Users.AddRange(
                AdminUser, ProcUserA, ProcUserB, DualUserAB, LockedUserA,
                RevokedMemberA, SuspendedMemberA, NoRoleUserA, MultiRoleAdminUserA,
                ContractorUserA, ContractorUserB, LockedContractorUser);
            await db.SaveChangesAsync();

            // 3. UserRoles
            db.UserRoles.AddRange(
                new UserRole { UserId = AdminUser.Id, RoleId = adminRole.Id },
                new UserRole { UserId = ProcUserA.Id, RoleId = procRole.Id },
                new UserRole { UserId = ProcUserB.Id, RoleId = procRole.Id },
                new UserRole { UserId = DualUserAB.Id, RoleId = procRole.Id },
                new UserRole { UserId = LockedUserA.Id, RoleId = procRole.Id },
                new UserRole { UserId = RevokedMemberA.Id, RoleId = procRole.Id },
                new UserRole { UserId = SuspendedMemberA.Id, RoleId = procRole.Id },
                // NoRoleUserA: Không gán role Procurement
                new UserRole { UserId = MultiRoleAdminUserA.Id, RoleId = adminRole.Id },
                new UserRole { UserId = MultiRoleAdminUserA.Id, RoleId = procRole.Id },
                new UserRole { UserId = ContractorUserA.Id, RoleId = contractorRole.Id },
                new UserRole { UserId = ContractorUserB.Id, RoleId = contractorRole.Id },
                new UserRole { UserId = LockedContractorUser.Id, RoleId = contractorRole.Id });
            await db.SaveChangesAsync();

            // 4. Entities
            EntityA = new ProcuringEntity
            {
                OrganizationName = "Ban Quản lý Dự án Giao thông (Entity A)",
                OrganizationType = "BanQLDA",
                TaxCode = "0100000001",
                RepresentativeName = "Trần Đại Diện A",
                RepresentativeTitle = "Giám đốc",
                UserId = ProcUserA.Id,
                EstablishmentDecisionFile = "scanA.pdf"
            };
            EntityB = new ProcuringEntity
            {
                OrganizationName = "Ban Quản lý Xây dựng Dân dụng (Entity B)",
                OrganizationType = "BanQLDA",
                TaxCode = "0100000002",
                RepresentativeName = "Lê Đại Diện B",
                RepresentativeTitle = "Giám đốc",
                UserId = ProcUserB.Id,
                EstablishmentDecisionFile = "scanB.pdf"
            };
            db.ProcuringEntities.AddRange(EntityA, EntityB);
            await db.SaveChangesAsync();

            // 5. Memberships
            db.ProcuringEntityMembers.AddRange(
                new ProcuringEntityMember { ProcuringEntityId = EntityA.Id, UserId = ProcUserA.Id, RoleInEntity = ProcuringEntityMemberRole.Officer, Status = MemberStatus.Active },
                new ProcuringEntityMember { ProcuringEntityId = EntityB.Id, UserId = ProcUserB.Id, RoleInEntity = ProcuringEntityMemberRole.Officer, Status = MemberStatus.Active },
                // Dual user: Active in A (Officer), Active in B (Manager)
                new ProcuringEntityMember { ProcuringEntityId = EntityA.Id, UserId = DualUserAB.Id, RoleInEntity = ProcuringEntityMemberRole.Officer, Status = MemberStatus.Active },
                new ProcuringEntityMember { ProcuringEntityId = EntityB.Id, UserId = DualUserAB.Id, RoleInEntity = ProcuringEntityMemberRole.Manager, Status = MemberStatus.Active },
                // Locked user: member in A
                new ProcuringEntityMember { ProcuringEntityId = EntityA.Id, UserId = LockedUserA.Id, RoleInEntity = ProcuringEntityMemberRole.Officer, Status = MemberStatus.Active },
                // Revoked member in A
                new ProcuringEntityMember { ProcuringEntityId = EntityA.Id, UserId = RevokedMemberA.Id, RoleInEntity = ProcuringEntityMemberRole.Officer, Status = MemberStatus.Revoked },
                // Suspended member in A
                new ProcuringEntityMember { ProcuringEntityId = EntityA.Id, UserId = SuspendedMemberA.Id, RoleInEntity = ProcuringEntityMemberRole.Officer, Status = MemberStatus.Suspended },
                // NoRole user: member in A
                new ProcuringEntityMember { ProcuringEntityId = EntityA.Id, UserId = NoRoleUserA.Id, RoleInEntity = ProcuringEntityMemberRole.Officer, Status = MemberStatus.Active },
                // MultiRole admin: Manager in A
                new ProcuringEntityMember { ProcuringEntityId = EntityA.Id, UserId = MultiRoleAdminUserA.Id, RoleInEntity = ProcuringEntityMemberRole.Manager, Status = MemberStatus.Active });
            await db.SaveChangesAsync();

            // 6. Contractors
            ContractorA = new Contractor
            {
                CompanyName = "Công ty CP Xây dựng Alpha",
                TaxCode = "0109999001",
                Address = "Hà Nội",
                UserId = ContractorUserA.Id
            };
            ContractorB = new Contractor
            {
                CompanyName = "Công ty TNHH Kỹ thuật Beta",
                TaxCode = "0109999002",
                Address = "Hồ Chí Minh",
                UserId = ContractorUserB.Id
            };
            db.Contractors.AddRange(ContractorA, ContractorB);
            await db.SaveChangesAsync();

            // 7. Packages
            PackageA = new BidPackage
            {
                Code = "PKG-A-001",
                Name = "Gói thầu Thi công Cầu đường Entity A",
                ProcuringEntityId = EntityA.Id,
                OwnershipNeedsReview = false,
                CreatedBy = ProcUserA.Id,
                Budget = 5000000000m,
                Deadline = DateTime.UtcNow.AddHours(-2), // Overdue -> triggers auto-close
                Status = BidPackageStatus.Open
            };
            PackageB = new BidPackage
            {
                Code = "PKG-B-002",
                Name = "Gói thầu Thiết bị Y tế Entity B",
                ProcuringEntityId = EntityB.Id,
                OwnershipNeedsReview = false,
                CreatedBy = ProcUserB.Id,
                Budget = 3000000000m,
                Deadline = DateTime.UtcNow.AddDays(10), // Not overdue
                Status = BidPackageStatus.Open
            };
            PackageUnassigned = new BidPackage
            {
                Code = "PKG-UNASSIGNED",
                Name = "Gói thầu Chưa gán đơn vị chủ quản",
                ProcuringEntityId = null,
                OwnershipNeedsReview = false,
                CreatedBy = ProcUserA.Id,
                Budget = 1000000000m,
                Deadline = DateTime.UtcNow.AddHours(-1),
                Status = BidPackageStatus.Open
            };
            PackageNeedsReview = new BidPackage
            {
                Code = "PKG-REVIEW",
                Name = "Gói thầu Cần đối soát quyền sở hữu",
                ProcuringEntityId = EntityA.Id,
                OwnershipNeedsReview = true, // Ownership needs review
                CreatedBy = ProcUserA.Id,
                Budget = 2000000000m,
                Deadline = DateTime.UtcNow.AddHours(-1),
                Status = BidPackageStatus.Open
            };
            db.BidPackages.AddRange(PackageA, PackageB, PackageUnassigned, PackageNeedsReview);
            await db.SaveChangesAsync();

            // 8. Contracts
            ContractA = new Contract
            {
                ContractNumber = "CTR-2026-A01",
                BidPackageId = PackageA.Id,
                ContractorId = ContractorA.Id,
                Value = 4800000000m,
                StartDate = DateTime.UtcNow.AddMonths(-3),
                EndDate = DateTime.UtcNow.AddDays(5), // Sắp hết hạn (còn 5 ngày <= 7 ngày)
                Status = ContractStatus.Active
            };
            ContractB = new Contract
            {
                ContractNumber = "CTR-2026-B02",
                BidPackageId = PackageB.Id,
                ContractorId = ContractorB.Id,
                Value = 2900000000m,
                StartDate = DateTime.UtcNow.AddMonths(-2),
                EndDate = DateTime.UtcNow.AddDays(60), // Chưa hết hạn
                Status = ContractStatus.Active
            };
            db.Contracts.AddRange(ContractA, ContractB);
            await db.SaveChangesAsync();

            // 9. Milestones
            MilestoneA = new ContractMilestone
            {
                ContractId = ContractA.Id,
                Title = "Nghiệm thu phần thân cầu Đợt 1",
                Amount = 1500000000m,
                DueDate = DateTime.UtcNow.AddDays(2), // Sắp đến hạn (còn 2 ngày <= 3 ngày)
                Status = MilestoneStatus.Pending
            };
            db.ContractMilestones.Add(MilestoneA);
            await db.SaveChangesAsync();

            // 10. Guarantees
            GuaranteeA = new ContractGuarantee
            {
                ContractId = ContractA.Id,
                GuaranteeNumber = "GRT-BIDV-998811",
                BankName = "Ngân hàng TMCP Đầu tư và Phát triển Việt Nam",
                Amount = 240000000m,
                Percentage = 5.0m,
                EffectiveDate = DateTime.UtcNow.AddMonths(-3),
                ExpiryDate = DateTime.UtcNow.AddDays(-1), // Quá hạn -> triggers Expired
                Status = GuaranteeStatus.Valid
            };
            db.ContractGuarantees.Add(GuaranteeA);
            await db.SaveChangesAsync();
        }

        private ContractExpiryNotificationWorker CreateWorker()
        {
            var scopeFactory = _serviceProvider.GetRequiredService<IServiceScopeFactory>();
            var logger = _serviceProvider.GetRequiredService<ILogger<ContractExpiryNotificationWorker>>();
            return new ContractExpiryNotificationWorker(scopeFactory, logger);
        }

        #region Bắt buộc 1: Cảnh báo của Entity A chỉ tới nhóm A hợp lệ và Admin vận hành; Entity B không nhận

        [Fact]
        public async Task Test01_EntityAAlert_SentOnlyToValidEntityAGroupAndPlatformAdmin_EntityBReceivesNothing()
        {
            var worker = CreateWorker();
            await worker.RunNotificationCheckAsync();

            using var db = CreateDbContext();
            var notifs = await db.Notifications.ToListAsync();
            var emails = _fakeEmailService.SentEmails;

            // Người nhận hợp lệ của Entity A:
            // - ProcUserA (thành viên A)
            // - DualUserAB (thành viên A và B)
            // - MultiRoleAdminUserA (thành viên A + Admin)
            // - AdminUser (Platform Admin)
            Assert.Contains(notifs, n => n.UserId == ProcUserA.Id);
            Assert.Contains(notifs, n => n.UserId == DualUserAB.Id);
            Assert.Contains(notifs, n => n.UserId == MultiRoleAdminUserA.Id);
            Assert.Contains(notifs, n => n.UserId == AdminUser.Id);

            Assert.Contains(emails, e => e.To == ProcUserA.Email);
            Assert.Contains(emails, e => e.To == DualUserAB.Email);
            Assert.Contains(emails, e => e.To == MultiRoleAdminUserA.Email);
            Assert.Contains(emails, e => e.To == AdminUser.Email);

            // TUYỆT ĐỐI KHÔNG GỬI TỚI:
            // 1. ProcUserB (thành viên của Entity B - không thuộc A)
            Assert.DoesNotContain(notifs, n => n.UserId == ProcUserB.Id);
            Assert.DoesNotContain(emails, e => e.To == ProcUserB.Email);

            // 2. ContractorUserB (nhà thầu của hợp đồng khác)
            Assert.DoesNotContain(notifs, n => n.UserId == ContractorUserB.Id);
            Assert.DoesNotContain(emails, e => e.To == ContractorUserB.Email);
        }

        #endregion

        #region Bắt buộc 2: Thu hồi membership A rồi chạy lượt sau: user không nhận của A; vẫn có thể nhận của B

        [Fact]
        public async Task Test02_RevokeEntityAMembership_UserDoesNotReceiveEntityAAlert_StillReceivesEntityBAlert()
        {
            // Bước 1: Thu hồi tư cách thành viên của DualUserAB tại Entity A
            using (var db = CreateDbContext())
            {
                var membershipA = await db.ProcuringEntityMembers
                    .SingleAsync(m => m.UserId == DualUserAB.Id && m.ProcuringEntityId == EntityA.Id);
                membershipA.Status = MemberStatus.Revoked;
                membershipA.RevokedAt = DateTime.UtcNow;

                // Đồng thời tạo 1 gói thầu quá hạn mới cho Entity B để kiểm tra lượt quét của B
                var overduePackageB = new BidPackage
                {
                    Code = "PKG-B-OVERDUE",
                    Name = "Gói thầu Quá hạn của Entity B",
                    ProcuringEntityId = EntityB.Id,
                    OwnershipNeedsReview = false,
                    CreatedBy = ProcUserB.Id,
                    Budget = 800000000m,
                    Deadline = DateTime.UtcNow.AddHours(-1),
                    Status = BidPackageStatus.Open
                };
                db.BidPackages.Add(overduePackageB);

                await db.SaveChangesAsync();
            }

            // Bước 2: Chạy lượt quét sau khi đã thu hồi membership A
            var worker = CreateWorker();
            await worker.RunNotificationCheckAsync();

            using (var db = CreateDbContext())
            {
                var notifs = await db.Notifications.ToListAsync();
                var emails = _fakeEmailService.SentEmails;

                // DualUserAB KHÔNG ĐƯỢC NHẬN bất kỳ cảnh báo nào liên quan đến Entity A (PKG-A-001, CTR-2026-A01, etc.)
                var notifsForDualFromA = notifs.Where(n => n.UserId == DualUserAB.Id && n.Message.Contains("Entity A")).ToList();
                Assert.Empty(notifsForDualFromA);

                // Nhưng DualUserAB VẪN NHẬN ĐƯỢC cảnh báo của Entity B (vì membership tại B vẫn Active)
                var notifsForDualFromB = notifs.Where(n => n.UserId == DualUserAB.Id && n.Message.Contains("Entity B")).ToList();
                Assert.NotEmpty(notifsForDualFromB);

                Assert.Contains(emails, e => e.To == DualUserAB.Email && e.Message.Contains("Entity B"));
            }
        }

        #endregion

        #region Bắt buộc 3: User bị khóa hoặc thiếu role Procurement không nhận qua nhánh thành viên

        [Fact]
        public async Task Test03_LockedUserOrUserLackingProcurementRole_DoesNotReceiveViaMembership()
        {
            var worker = CreateWorker();
            await worker.RunNotificationCheckAsync();

            using var db = CreateDbContext();
            var notifs = await db.Notifications.ToListAsync();
            var emails = _fakeEmailService.SentEmails;

            // 1. User bị khóa (IsActive = false), dù có membership Active tại Entity A: KHÔNG NHẬN
            Assert.DoesNotContain(notifs, n => n.UserId == LockedUserA.Id);
            Assert.DoesNotContain(emails, e => e.To == LockedUserA.Email);

            // 2. User có membership Active tại A nhưng thiếu vai trò hệ thống Procurement: KHÔNG NHẬN
            Assert.DoesNotContain(notifs, n => n.UserId == NoRoleUserA.Id);
            Assert.DoesNotContain(emails, e => e.To == NoRoleUserA.Email);

            // 3. User có role Procurement nhưng membership bị Revoked: KHÔNG NHẬN
            Assert.DoesNotContain(notifs, n => n.UserId == RevokedMemberA.Id);
            Assert.DoesNotContain(emails, e => e.To == RevokedMemberA.Email);

            // 4. User có role Procurement nhưng membership bị Suspended: KHÔNG NHẬN
            Assert.DoesNotContain(notifs, n => n.UserId == SuspendedMemberA.Id);
            Assert.DoesNotContain(emails, e => e.To == SuspendedMemberA.Email);
        }

        #endregion

        #region Bắt buộc 4: Không duplicate khi nhiều role/membership; chống trùng 24 giờ

        [Fact]
        public async Task Test04_NoDuplicateNotificationsOrEmails_And24HourDeduplication()
        {
            var worker = CreateWorker();

            // Chạy lượt 1
            await worker.RunNotificationCheckAsync();

            using (var db = CreateDbContext())
            {
                // MultiRoleAdminUserA vừa có role Admin, vừa có role Procurement, vừa là thành viên Entity A
                // Đối với mỗi tiêu đề thông báo (ví dụ gói PKG-A-001 đóng thầu), chỉ được nhận DUY NHẤT 1 thông báo
                var packageTitle = "[Thông báo] Gói thầu PKG-A-001 đã hết hạn và tự động đóng thầu";
                var notifsForMultiRole = await db.Notifications
                    .Where(n => n.UserId == MultiRoleAdminUserA.Id && n.Title == packageTitle)
                    .ToListAsync();

                Assert.Single(notifsForMultiRole);

                var emailsForMultiRole = _fakeEmailService.SentEmails
                    .Where(e => e.To == MultiRoleAdminUserA.Email && e.Title == packageTitle)
                    .ToList();

                Assert.Single(emailsForMultiRole);
            }

            // Chạy lượt 2 ngay lập tức (trong cửa sổ 24 giờ chống trùng)
            var emailCountBefore = _fakeEmailService.SentEmails.Count;
            await worker.RunNotificationCheckAsync();

            using (var db = CreateDbContext())
            {
                var packageTitle = "[Thông báo] Gói thầu PKG-A-001 đã hết hạn và tự động đóng thầu";
                var notifsForMultiRoleAfter = await db.Notifications
                    .Where(n => n.UserId == MultiRoleAdminUserA.Id && n.Title == packageTitle)
                    .ToListAsync();

                // Số lượng thông báo trong DB vẫn giữ nguyên là 1, không sinh thêm
                Assert.Single(notifsForMultiRoleAfter);

                // Không gửi thêm email nào
                var emailCountAfter = _fakeEmailService.SentEmails.Count;
                Assert.Equal(emailCountBefore, emailCountAfter);
            }
        }

        #endregion

        #region Bắt buộc 5: Thiếu owner hoặc ownership đang chờ đối soát: không fallback creator; chỉ Admin nội bộ nhận

        [Fact]
        public async Task Test05_UnassignedOrNeedsReviewPackage_DoesNotFallbackToCreator_SentOnlyToPlatformAdmin()
        {
            var worker = CreateWorker();
            await worker.RunNotificationCheckAsync();

            using var db = CreateDbContext();

            // 1. Gói PKG-UNASSIGNED (ProcuringEntityId == null, CreatedBy = ProcUserA.Id):
            var unassignedTitle = "[Thông báo] Gói thầu PKG-UNASSIGNED đã hết hạn và tự động đóng thầu";
            var unassignedNotifs = await db.Notifications.Where(n => n.Title == unassignedTitle).ToListAsync();

            // Phải gửi tới Platform Admin
            Assert.Contains(unassignedNotifs, n => n.UserId == AdminUser.Id);
            Assert.Contains(unassignedNotifs, n => n.UserId == MultiRoleAdminUserA.Id);

            // TUYỆT ĐỐI KHÔNG FALLBACK TỚI CREATOR (ProcUserA)
            Assert.DoesNotContain(unassignedNotifs, n => n.UserId == ProcUserA.Id);

            // 2. Gói PKG-REVIEW (ProcuringEntityId = EntityA.Id, nhưng mang cờ OwnershipNeedsReview = true, CreatedBy = ProcUserA.Id):
            var reviewTitle = "[Thông báo] Gói thầu PKG-REVIEW đã hết hạn và tự động đóng thầu";
            var reviewNotifs = await db.Notifications.Where(n => n.Title == reviewTitle).ToListAsync();

            // Phải gửi tới Platform Admin
            Assert.Contains(reviewNotifs, n => n.UserId == AdminUser.Id);
            Assert.Contains(reviewNotifs, n => n.UserId == MultiRoleAdminUserA.Id);

            // KHÔNG GỬI TỚI THÀNH VIÊN THƯỜNG CỦA ENTITY A (ProcUserA, DualUserAB)
            Assert.DoesNotContain(reviewNotifs, n => n.UserId == ProcUserA.Id);
        }

        #endregion

        #region Bắt buộc 6: Nhà thầu đúng hợp đồng vẫn nhận; nhà thầu khác không nhận

        [Fact]
        public async Task Test06_ContractorRecipient_OnlyAssignedActiveContractorReceives_OtherContractorsDoNot()
        {
            var worker = CreateWorker();
            await worker.RunNotificationCheckAsync();

            using var db = CreateDbContext();
            var notifs = await db.Notifications.ToListAsync();
            var emails = _fakeEmailService.SentEmails;

            // Hợp đồng CTR-2026-A01 thuộc về ContractorA (ContractorUserA)
            var contractAlertTitle = $"[Khẩn cấp] Hợp đồng {ContractA.ContractNumber} còn 5 ngày là hết hạn";
            Assert.Contains(notifs, n => n.UserId == ContractorUserA.Id && n.Title == contractAlertTitle);
            Assert.Contains(emails, e => e.To == ContractorUserA.Email && e.Title == contractAlertTitle);

            // Mốc tiến độ MilestoneA thuộc về ContractA -> ContractorUserA
            var milestoneAlertTitle = $"[Khẩn cấp] Mốc \"{MilestoneA.Title}\" hợp đồng {ContractA.ContractNumber} còn 2 ngày là đến hạn";
            Assert.Contains(notifs, n => n.UserId == ContractorUserA.Id && n.Title == milestoneAlertTitle);
            Assert.Contains(emails, e => e.To == ContractorUserA.Email && e.Title == milestoneAlertTitle);

            // ContractorUserB (nhà thầu của hợp đồng khác) TUYỆT ĐỐI KHÔNG NHẬN cảnh báo của ContractA hay MilestoneA
            Assert.DoesNotContain(notifs, n => n.UserId == ContractorUserB.Id && n.Title == contractAlertTitle);
            Assert.DoesNotContain(notifs, n => n.UserId == ContractorUserB.Id && n.Title == milestoneAlertTitle);
            Assert.DoesNotContain(emails, e => e.To == ContractorUserB.Email && e.Title == contractAlertTitle);
            Assert.DoesNotContain(emails, e => e.To == ContractorUserB.Email && e.Title == milestoneAlertTitle);
        }

        #endregion

        #region Bắt buộc 7: Bao phủ các loại sự kiện đóng thầu/hợp đồng/milestone/bảo lãnh dùng policy chung

        [Fact]
        public async Task Test07_CoverageAcrossAllEventTypes_DemonstratesCommonScopedPolicyAcrossBranches()
        {
            // Bổ sung thêm 1 sự kiện Clarification sắp hết hạn (dưới 24h) để bao phủ đầy đủ 5 nhánh
            using (var db = CreateDbContext())
            {
                var submissionA = new BidSubmission
                {
                    BidPackageId = PackageA.Id,
                    ContractorId = ContractorA.Id,
                    BidPrice = 4800000000m,
                    SubmittedAt = DateTime.UtcNow.AddDays(-10),
                    Status = BidSubmissionStatus.Submitted
                };
                db.BidSubmissions.Add(submissionA);
                await db.SaveChangesAsync();

                var clar = new BidClarification
                {
                    BidPackageId = PackageA.Id,
                    BidSubmissionId = submissionA.Id,
                    Title = "Giải trình năng lực kỹ thuật thiết bị",
                    QuestionContent = "Đề nghị nhà thầu làm rõ chứng chỉ xuất xưởng CO/CQ của dòng Switch Core.",
                    AskedByUserId = ProcUserA.Id,
                    AskedAt = DateTime.UtcNow.AddDays(-1),
                    Type = ClarificationType.SubmissionClarification,
                    Status = ClarificationStatus.Pending,
                    Deadline = DateTime.UtcNow.AddHours(12) // Còn 12h <= 24h
                };
                db.BidClarifications.Add(clar);
                await db.SaveChangesAsync();
            }

            var worker = CreateWorker();
            await worker.RunNotificationCheckAsync();

            using var verifyDb = CreateDbContext();
            var notifs = await verifyDb.Notifications.ToListAsync();

            // 1. Nhánh A: Đóng thầu tự động (DeadlineApproaching)
            var overdueNotifs = notifs.Where(n => n.Type == NotificationType.DeadlineApproaching && n.Title.Contains("đã hết hạn và tự động đóng thầu")).ToList();
            Assert.NotEmpty(overdueNotifs);
            Assert.All(overdueNotifs, n => Assert.True(n.UserId == ProcUserA.Id || n.UserId == DualUserAB.Id || n.UserId == MultiRoleAdminUserA.Id || n.UserId == AdminUser.Id));

            // 2. Nhánh B: Hợp đồng sắp hết hạn (ContractExpiring)
            var contractNotifs = notifs.Where(n => n.Type == NotificationType.ContractExpiring && n.Title.Contains("CTR-2026-A01") && !n.Title.Contains("bảo lãnh")).ToList();
            Assert.NotEmpty(contractNotifs);
            Assert.All(contractNotifs, n => Assert.True(n.UserId == ContractorUserA.Id || n.UserId == ProcUserA.Id || n.UserId == DualUserAB.Id || n.UserId == MultiRoleAdminUserA.Id || n.UserId == AdminUser.Id));

            // 3. Nhánh C: Mốc tiến độ sắp đến hạn (ProgressOverdue)
            var milestoneNotifs = notifs.Where(n => n.Type == NotificationType.ProgressOverdue && n.Title.Contains("Nghiệm thu")).ToList();
            Assert.NotEmpty(milestoneNotifs);
            Assert.All(milestoneNotifs, n => Assert.True(n.UserId == ContractorUserA.Id || n.UserId == ProcUserA.Id || n.UserId == DualUserAB.Id || n.UserId == MultiRoleAdminUserA.Id || n.UserId == AdminUser.Id));

            // 4. Nhánh D: Bảo lãnh hết hạn (ContractExpiring)
            var guaranteeNotifs = notifs.Where(n => n.Type == NotificationType.ContractExpiring && n.Title.Contains("GRT-BIDV-998811")).ToList();
            Assert.NotEmpty(guaranteeNotifs);
            Assert.All(guaranteeNotifs, n => Assert.True(n.UserId == ProcUserA.Id || n.UserId == DualUserAB.Id || n.UserId == MultiRoleAdminUserA.Id || n.UserId == AdminUser.Id));

            // 5. Nhánh E: Giải trình HSDT (DeadlineApproaching)
            var clarNotifs = notifs.Where(n => n.Type == NotificationType.DeadlineApproaching && n.Title.Contains("giải trình HSDT")).ToList();
            Assert.NotEmpty(clarNotifs);
            Assert.All(clarNotifs, n => Assert.Equal(ContractorUserA.Id, n.UserId));

            // Khẳng định chắc chắn: Trong TẤT CẢ các sự kiện trên, ProcUserB và ContractorUserB KHÔNG BAO GIỜ nhận được thông báo
            Assert.DoesNotContain(notifs, n => n.UserId == ProcUserB.Id);
            Assert.DoesNotContain(notifs, n => n.UserId == ContractorUserB.Id);
        }

        #endregion

        #region Bắt buộc 8: Kiểm thử tích hợp trọn vẹn Worker: Cập nhật CSDL và gửi Email

        [Fact]
        public async Task Test08_EndToEndWorkerIntegration_VerifiesDatabaseUpdatesAndEmailDelivery()
        {
            var worker = CreateWorker();
            await worker.RunNotificationCheckAsync();

            using var db = CreateDbContext();

            // 1. Trạng thái gói thầu quá hạn tự động chuyển sang Closed
            var pkgA = await db.BidPackages.FindAsync(PackageA.Id);
            Assert.NotNull(pkgA);
            Assert.Equal(BidPackageStatus.Closed, pkgA.Status);

            // 2. Trạng thái chứng thư bảo lãnh quá hạn tự động chuyển sang Expired
            var grtA = await db.ContractGuarantees.FindAsync(GuaranteeA.Id);
            Assert.NotNull(grtA);
            Assert.Equal(GuaranteeStatus.Expired, grtA.Status);

            // 3. Thông báo được lưu đầy đủ vào CSDL
            var totalNotifs = await db.Notifications.CountAsync();
            Assert.True(totalNotifs > 0);

            // 4. Email cảnh báo được gửi qua IEmailService tương ứng với người nhận
            var totalEmails = _fakeEmailService.SentEmails.Count;
            Assert.True(totalEmails > 0);

            // Khẳng định email và notification luôn tương ứng nhau
            foreach (var email in _fakeEmailService.SentEmails)
            {
                Assert.False(string.IsNullOrWhiteSpace(email.To));
                Assert.False(string.IsNullOrWhiteSpace(email.Title));
                Assert.False(string.IsNullOrWhiteSpace(email.Message));
            }
        }

        #endregion
    }

    /// <summary>
    /// Fake Email Service dùng riêng cho bộ kiểm thử NotificationRecipientScopeTests
    /// theo yêu cầu phân tách độc lập (không sửa TestFakes.cs chung).
    /// </summary>
    public class FakeEmailServiceForRecipientTests : IEmailService
    {
        public List<(string To, string Title, string Message)> SentEmails { get; } = new();

        public Task<bool> SendEmailAsync(string toEmail, string subject, string body, bool isHtml = true)
        {
            SentEmails.Add((toEmail, subject, body));
            return Task.FromResult(true);
        }
    }
}
