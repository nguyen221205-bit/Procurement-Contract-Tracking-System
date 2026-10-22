using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProcurementSystem.Core.Enums;
using ProcurementSystem.Infrastructure.Data;
using ProcurementSystem.Infrastructure.Entities;

namespace ProcurementSystem.Infrastructure.Seeders
{
    public static class DataSeeder
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            await context.Database.MigrateAsync();

            // Bảo đảm bảng BidPackageEvaluators luôn tồn tại trên mọi máy môi trường dev
            await context.Database.ExecuteSqlRawAsync(@"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'BidPackageEvaluators')
                BEGIN
                    CREATE TABLE BidPackageEvaluators (
                        BidPackageId INT NOT NULL,
                        EvaluatorId INT NOT NULL,
                        AssignedAt DATETIME2 NOT NULL CONSTRAINT DF_BidPackageEvaluators_AssignedAt DEFAULT SYSUTCDATETIME(),
                        AssignedBy INT NULL,
                        CONSTRAINT PK_BidPackageEvaluators PRIMARY KEY CLUSTERED (BidPackageId, EvaluatorId),
                        CONSTRAINT FK_BidPackageEvaluators_BidPackages FOREIGN KEY (BidPackageId) REFERENCES BidPackages(Id) ON DELETE CASCADE,
                        CONSTRAINT FK_BidPackageEvaluators_Users_Evaluator FOREIGN KEY (EvaluatorId) REFERENCES Users(Id) ON DELETE NO ACTION,
                        CONSTRAINT FK_BidPackageEvaluators_Users_Assigner FOREIGN KEY (AssignedBy) REFERENCES Users(Id) ON DELETE SET NULL
                    );
                    CREATE NONCLUSTERED INDEX IX_BidPackageEvaluators_EvaluatorId ON BidPackageEvaluators (EvaluatorId);
                END

                -- Bảo đảm cột BidPrice luôn tồn tại trên bảng BidSubmissions
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[BidSubmissions]') AND name = 'BidPrice')
                BEGIN
                    ALTER TABLE [BidSubmissions] ADD [BidPrice] DECIMAL(18,2) NULL;
                END

                -- Bảo đảm cột SelectionReason luôn tồn tại trên bảng BidSubmissions
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[BidSubmissions]') AND name = 'SelectionReason')
                BEGIN
                    ALTER TABLE [BidSubmissions] ADD [SelectionReason] NVARCHAR(1000) NULL;
                END

                -- Bảo đảm cột VerificationStatus luôn tồn tại trên bảng Contractors
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[Contractors]') AND name = 'VerificationStatus')
                BEGIN
                    ALTER TABLE [Contractors] ADD [VerificationStatus] NVARCHAR(50) NOT NULL CONSTRAINT DF_Contractors_VerificationStatus DEFAULT N'Approved';
                END

                -- Bảo đảm các cột Chữ ký số điện tử luôn tồn tại trên bảng Contractors
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[Contractors]') AND name = 'IsDigitalSigned')
                BEGIN
                    ALTER TABLE [Contractors] ADD [IsDigitalSigned] BIT NOT NULL CONSTRAINT DF_Contractors_IsDigitalSigned DEFAULT 0;
                END
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[Contractors]') AND name = 'DigitalSignerName')
                BEGIN
                    ALTER TABLE [Contractors] ADD [DigitalSignerName] NVARCHAR(250) NULL;
                END
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[Contractors]') AND name = 'DigitalSignDate')
                BEGIN
                    ALTER TABLE [Contractors] ADD [DigitalSignDate] DATETIME2 NULL;
                END
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[Contractors]') AND name = 'DigitalSignMessage')
                BEGIN
                    ALTER TABLE [Contractors] ADD [DigitalSignMessage] NVARCHAR(500) NULL;
                END

                -- R2-3: Bảo đảm cột IsPriceCriteria luôn tồn tại trên bảng EvaluationCriteria
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[EvaluationCriteria]') AND name = 'IsPriceCriteria')
                BEGIN
                    ALTER TABLE [EvaluationCriteria] ADD [IsPriceCriteria] BIT NOT NULL CONSTRAINT DF_EvaluationCriteria_IsPriceCriteria DEFAULT 0;
                END
                -- Migrate dữ liệu cũ: Các tiêu chí có tên liên quan đến Giá / Tài chính / Chi phí chuyển sang IsPriceCriteria = 1
                UPDATE [EvaluationCriteria]
                SET [IsPriceCriteria] = 1
                WHERE [IsPriceCriteria] = 0 AND (
                    [Name] LIKE N'%Giá%' OR [Name] LIKE N'%giá%' OR
                    [Name] LIKE N'%Tài chính%' OR [Name] LIKE N'%tài chính%' OR
                    [Name] LIKE N'%Chi phí%' OR [Name] LIKE N'%chi phí%' OR
                    [Name] LIKE N'%Price%' OR [Name] LIKE N'%price%'
                );

                -- Bảo đảm bảng EvaluatorProposals luôn tồn tại
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'EvaluatorProposals')
                BEGIN
                    CREATE TABLE [EvaluatorProposals] (
                        [Id] INT IDENTITY(1,1) NOT NULL,
                        [ProposerUserId] INT NOT NULL,
                        [FullName] NVARCHAR(100) NOT NULL,
                        [Email] NVARCHAR(150) NOT NULL,
                        [Phone] NVARCHAR(20) NULL,
                        [Specialization] NVARCHAR(200) NULL,
                        [Workplace] NVARCHAR(250) NULL,
                        [ExperienceYears] INT NULL,
                        [CertificateName] NVARCHAR(200) NULL,
                        [CertificateFile] NVARCHAR(500) NULL,
                        [Notes] NVARCHAR(1000) NULL,
                        [Status] NVARCHAR(50) NOT NULL CONSTRAINT DF_EvaluatorProposals_Status DEFAULT N'Pending',
                        [AdminNotes] NVARCHAR(1000) NULL,
                        [ReviewedByUserId] INT NULL,
                        [ReviewedAt] DATETIME2 NULL,
                        [CreatedUserId] INT NULL,
                        [CreatedAt] DATETIME2 NOT NULL CONSTRAINT DF_EvaluatorProposals_CreatedAt DEFAULT SYSUTCDATETIME(),
                        CONSTRAINT PK_EvaluatorProposals PRIMARY KEY CLUSTERED ([Id] ASC),
                        CONSTRAINT FK_EvaluatorProposals_Users_Proposer FOREIGN KEY ([ProposerUserId]) REFERENCES [Users]([Id]),
                        CONSTRAINT FK_EvaluatorProposals_Users_Reviewer FOREIGN KEY ([ReviewedByUserId]) REFERENCES [Users]([Id]),
                        CONSTRAINT FK_EvaluatorProposals_Users_Created FOREIGN KEY ([CreatedUserId]) REFERENCES [Users]([Id])
                    );
                    CREATE NONCLUSTERED INDEX IX_EvaluatorProposals_Status ON [EvaluatorProposals] ([Status]);
                    CREATE NONCLUSTERED INDEX IX_EvaluatorProposals_Email ON [EvaluatorProposals] ([Email]);
                    CREATE NONCLUSTERED INDEX IX_EvaluatorProposals_ProposerUserId ON [EvaluatorProposals] ([ProposerUserId]);
                END

                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ProcuringEntities')
                BEGIN
                    CREATE TABLE [ProcuringEntities] (
                        [Id] INT IDENTITY(1,1) NOT NULL,
                        [UserId] INT NOT NULL,
                        [OrganizationName] NVARCHAR(250) NOT NULL,
                        [OrganizationType] NVARCHAR(100) NOT NULL,
                        [TaxCode] NVARCHAR(20) NOT NULL,
                        [BudgetCode] NVARCHAR(50) NULL,
                        [Address] NVARCHAR(500) NULL,
                        [RepresentativeName] NVARCHAR(100) NOT NULL,
                        [RepresentativeTitle] NVARCHAR(100) NOT NULL,
                        [RepresentativePhone] NVARCHAR(20) NULL,
                        [EstablishmentDecisionFile] NVARCHAR(500) NOT NULL,
                        [AppointmentDecisionFile] NVARCHAR(500) NULL,
                        [VerificationStatus] NVARCHAR(50) NOT NULL CONSTRAINT DF_ProcuringEntities_VerificationStatus DEFAULT N'Pending',
                        [IsDigitalSigned] BIT NOT NULL CONSTRAINT DF_ProcuringEntities_IsDigitalSigned DEFAULT 0,
                        [DigitalSignerName] NVARCHAR(250) NULL,
                        [DigitalSignDate] DATETIME2 NULL,
                        [DigitalSignMessage] NVARCHAR(500) NULL,
                        [AdminNotes] NVARCHAR(1000) NULL,
                        [ReviewedByUserId] INT NULL,
                        [ReviewedAt] DATETIME2 NULL,
                        [CreatedAt] DATETIME2 NOT NULL CONSTRAINT DF_ProcuringEntities_CreatedAt DEFAULT SYSUTCDATETIME(),
                        [UpdatedAt] DATETIME2 NULL,
                        CONSTRAINT PK_ProcuringEntities PRIMARY KEY CLUSTERED ([Id] ASC),
                        CONSTRAINT FK_ProcuringEntities_Users_User FOREIGN KEY ([UserId]) REFERENCES [Users]([Id]) ON DELETE CASCADE,
                        CONSTRAINT FK_ProcuringEntities_Users_Reviewer FOREIGN KEY ([ReviewedByUserId]) REFERENCES [Users]([Id])
                    );
                    CREATE UNIQUE NONCLUSTERED INDEX IX_ProcuringEntities_UserId ON [ProcuringEntities] ([UserId]);
                    CREATE NONCLUSTERED INDEX IX_ProcuringEntities_TaxCode ON [ProcuringEntities] ([TaxCode]);
                    CREATE NONCLUSTERED INDEX IX_ProcuringEntities_VerificationStatus ON [ProcuringEntities] ([VerificationStatus]);
                END

                -- Bảo đảm các cột Chữ ký số điện tử luôn tồn tại trên bảng ProcuringEntities (trường hợp bảng đã tồn tại từ trước)
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[ProcuringEntities]') AND name = 'IsDigitalSigned')
                BEGIN
                    ALTER TABLE [ProcuringEntities] ADD [IsDigitalSigned] BIT NOT NULL CONSTRAINT DF_ProcuringEntities_IsDigitalSigned_Alt DEFAULT 0;
                END
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[ProcuringEntities]') AND name = 'DigitalSignerName')
                BEGIN
                    ALTER TABLE [ProcuringEntities] ADD [DigitalSignerName] NVARCHAR(250) NULL;
                END
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[ProcuringEntities]') AND name = 'DigitalSignDate')
                BEGIN
                    ALTER TABLE [ProcuringEntities] ADD [DigitalSignDate] DATETIME2 NULL;
                END
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[ProcuringEntities]') AND name = 'DigitalSignMessage')
                BEGIN
                    ALTER TABLE [ProcuringEntities] ADD [DigitalSignMessage] NVARCHAR(500) NULL;
                END

                -- Tự động khởi tạo bảng PackageStatusApprovalRequests (Thẩm quyền phê duyệt đóng/mở/hủy thầu)
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'PackageStatusApprovalRequests')
                BEGIN
                    CREATE TABLE [PackageStatusApprovalRequests] (
                        [Id] INT IDENTITY(1,1) NOT NULL,
                        [BidPackageId] INT NOT NULL,
                        [RequestedByUserId] INT NOT NULL,
                        [CurrentStatus] INT NOT NULL,
                        [TargetStatus] INT NOT NULL,
                        [Reason] NVARCHAR(1000) NOT NULL,
                        [NewDeadline] DATETIME2 NULL,
                        [Status] INT NOT NULL CONSTRAINT DF_PackageStatusApprovalRequests_Status DEFAULT 0,
                        [AdminComment] NVARCHAR(1000) NULL,
                        [ReviewedByUserId] INT NULL,
                        [ReviewedAt] DATETIME2 NULL,
                        [CreatedAt] DATETIME2 NOT NULL CONSTRAINT DF_PackageStatusApprovalRequests_CreatedAt DEFAULT SYSUTCDATETIME(),
                        CONSTRAINT PK_PackageStatusApprovalRequests PRIMARY KEY CLUSTERED ([Id] ASC),
                        CONSTRAINT FK_PackageStatusApprovalRequests_BidPackages FOREIGN KEY ([BidPackageId]) REFERENCES [BidPackages]([Id]) ON DELETE CASCADE,
                        CONSTRAINT FK_PackageStatusApprovalRequests_Users_Requested FOREIGN KEY ([RequestedByUserId]) REFERENCES [Users]([Id]),
                        CONSTRAINT FK_PackageStatusApprovalRequests_Users_Reviewed FOREIGN KEY ([ReviewedByUserId]) REFERENCES [Users]([Id])
                    );
                    CREATE NONCLUSTERED INDEX IX_PackageStatusApprovalRequests_Status ON [PackageStatusApprovalRequests] ([Status]);
                    CREATE NONCLUSTERED INDEX IX_PackageStatusApprovalRequests_BidPackageId ON [PackageStatusApprovalRequests] ([BidPackageId]);
                END

                -- Tự động khởi tạo bảng BidClarifications (Kênh hỏi đáp làm rõ HSMT & HSDT)
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'BidClarifications')
                BEGIN
                    CREATE TABLE [BidClarifications] (
                        [Id] INT IDENTITY(1,1) NOT NULL,
                        [BidPackageId] INT NOT NULL,
                        [BidSubmissionId] INT NULL,
                        [Type] INT NOT NULL CONSTRAINT DF_BidClarifications_Type DEFAULT 0,
                        [Title] NVARCHAR(250) NOT NULL,
                        [QuestionContent] NVARCHAR(3000) NOT NULL,
                        [QuestionAttachmentUrl] NVARCHAR(500) NULL,
                        [AskedByUserId] INT NOT NULL,
                        [AskedAt] DATETIME2 NOT NULL CONSTRAINT DF_BidClarifications_AskedAt DEFAULT SYSUTCDATETIME(),
                        [Deadline] DATETIME2 NULL,
                        [AnswerContent] NVARCHAR(3000) NULL,
                        [AnswerAttachmentUrl] NVARCHAR(500) NULL,
                        [AnsweredByUserId] INT NULL,
                        [AnsweredAt] DATETIME2 NULL,
                        [Status] INT NOT NULL CONSTRAINT DF_BidClarifications_Status DEFAULT 0,
                        CONSTRAINT PK_BidClarifications PRIMARY KEY CLUSTERED ([Id] ASC),
                        CONSTRAINT FK_BidClarifications_BidPackages FOREIGN KEY ([BidPackageId]) REFERENCES [BidPackages]([Id]) ON DELETE CASCADE,
                        CONSTRAINT FK_BidClarifications_BidSubmissions FOREIGN KEY ([BidSubmissionId]) REFERENCES [BidSubmissions]([Id]) ON DELETE SET NULL,
                        CONSTRAINT FK_BidClarifications_Users_Asked FOREIGN KEY ([AskedByUserId]) REFERENCES [Users]([Id]),
                        CONSTRAINT FK_BidClarifications_Users_Answered FOREIGN KEY ([AnsweredByUserId]) REFERENCES [Users]([Id])
                    );
                    CREATE NONCLUSTERED INDEX IX_BidClarifications_BidPackageId ON [BidClarifications] ([BidPackageId]);
                    CREATE NONCLUSTERED INDEX IX_BidClarifications_BidSubmissionId ON [BidClarifications] ([BidSubmissionId]);
                    CREATE NONCLUSTERED INDEX IX_BidClarifications_Type ON [BidClarifications] ([Type]);
                END
            ");

            // Tự động mở khóa toàn bộ tài khoản Giám khảo không bắt buộc đổi mật khẩu
            await context.Database.ExecuteSqlRawAsync(@"
                UPDATE u
                SET u.MustChangePassword = 0
                FROM Users u
                INNER JOIN UserRoles ur ON u.Id = ur.UserId
                INNER JOIN Roles r ON ur.RoleId = r.Id
                WHERE r.Name = 'Evaluator';

                UPDATE Users
                SET MustChangePassword = 0
                WHERE Email LIKE '%gk%' OR Email LIKE '%evaluator%';
            ");

            // 1. Seed Roles
            var roleNames = new[] { "Admin", "Procurement", "Evaluator", "Contractor" };
            var roleDescriptions = new Dictionary<string, string>
            {
                ["Admin"] = "Quản trị viên hệ thống",
                ["Procurement"] = "Bộ phận quản lý và mua sắm",
                ["Evaluator"] = "Ban thẩm định và chấm điểm",
                ["Contractor"] = "Nhà thầu tham gia đấu thầu"
            };

            foreach (var rName in roleNames)
            {
                if (!await context.Roles.AnyAsync(r => r.Name == rName))
                {
                    await context.Roles.AddAsync(new Role
                    {
                        Name = rName,
                        Description = roleDescriptions[rName]
                    });
                }
            }
            await context.SaveChangesAsync();

            var adminRole = await context.Roles.FirstAsync(r => r.Name == "Admin");
            var procRole = await context.Roles.FirstAsync(r => r.Name == "Procurement");
            var evalRole = await context.Roles.FirstAsync(r => r.Name == "Evaluator");
            var contractorRole = await context.Roles.FirstAsync(r => r.Name == "Contractor");

            // 2. Seed Internal Users (Admin, Procurement, Evaluators)
            // 2.1 Admin
            var adminUser = await context.Users.FirstOrDefaultAsync(u => u.Email == "admin@procurement.com");
            if (adminUser == null)
            {
                adminUser = new User
                {
                    FullName = "System Administrator",
                    Email = "admin@procurement.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
                    Phone = "0900000000",
                    IsActive = true
                };
                await context.Users.AddAsync(adminUser);
                await context.SaveChangesAsync();

                await context.UserRoles.AddAsync(new UserRole
                {
                    UserId = adminUser.Id,
                    RoleId = adminRole.Id
                });
                await context.SaveChangesAsync();
            }

            // 2.2 Procurement Staff
            var procUser = await context.Users.FirstOrDefaultAsync(u => u.Email == "procurement@procurement.com");
            if (procUser == null)
            {
                procUser = new User
                {
                    FullName = "Nguyen Thi Chuyen Vien Mua Sam",
                    Email = "procurement@procurement.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
                    Phone = "0901111111",
                    IsActive = true
                };
                await context.Users.AddAsync(procUser);
                await context.SaveChangesAsync();

                await context.UserRoles.AddAsync(new UserRole
                {
                    UserId = procUser.Id,
                    RoleId = procRole.Id
                });
                await context.SaveChangesAsync();
            }

            // 2.2.1 Seed ProcuringEntity profile for procurement@procurement.com
            if (procUser != null && !await context.ProcuringEntities.AnyAsync(pe => pe.UserId == procUser.Id))
            {
                await context.ProcuringEntities.AddAsync(new ProcuringEntity
                {
                    UserId = procUser.Id,
                    OrganizationName = "Ban Quản lý Dự án Đầu tư Xây dựng Mua sắm Tập trung",
                    OrganizationType = "Ban Quản lý Dự án",
                    TaxCode = "0100109106",
                    BudgetCode = "1054321",
                    Address = "Số 123 Đường Trần Phú, Quận Ba Đình, Hà Nội",
                    RepresentativeName = "Nguyen Thi Chuyen Vien Mua Sam",
                    RepresentativeTitle = "Phó Giám đốc Ban QLDA",
                    RepresentativePhone = "0901111111",
                    EstablishmentDecisionFile = "uploads/procuring-entities/seed_qd_thanh_lap.pdf",
                    VerificationStatus = "Approved",
                    IsDigitalSigned = true,
                    DigitalSignerName = "Nguyen Thi Chuyen Vien Mua Sam",
                    DigitalSignDate = DateTime.UtcNow.AddMonths(-3),
                    DigitalSignMessage = "Chứng thư số Ban QLDA hợp lệ",
                    ReviewedAt = DateTime.UtcNow
                });
                await context.SaveChangesAsync();
            }

            // 2.3 Evaluator 1 (Trưởng ban thẩm định)
            var evalUser = await context.Users.FirstOrDefaultAsync(u => u.Email == "evaluator@procurement.com");
            if (evalUser == null)
            {
                evalUser = new User
                {
                    FullName = "Tran Van Giam Khao Cham Thau",
                    Email = "evaluator@procurement.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
                    Phone = "0902222222",
                    IsActive = true
                };
                await context.Users.AddAsync(evalUser);
                await context.SaveChangesAsync();

                await context.UserRoles.AddAsync(new UserRole
                {
                    UserId = evalUser.Id,
                    RoleId = evalRole.Id
                });
                await context.SaveChangesAsync();
            }

            // 2.4 Evaluator 2 (Giám khảo Kỹ thuật - Luật Đấu thầu: tổ chuyên gia tối thiểu 3 thành viên số lẻ)
            var evalUser2 = await context.Users.FirstOrDefaultAsync(u => u.Email == "evaluator2@procurement.com");
            if (evalUser2 == null)
            {
                evalUser2 = new User
                {
                    FullName = "Le Thi Giam Khao Ky Thuat",
                    Email = "evaluator2@procurement.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
                    Phone = "0903333333",
                    IsActive = true
                };
                await context.Users.AddAsync(evalUser2);
                await context.SaveChangesAsync();

                await context.UserRoles.AddAsync(new UserRole
                {
                    UserId = evalUser2.Id,
                    RoleId = evalRole.Id
                });
                await context.SaveChangesAsync();
            }

            // 2.5 Evaluator 3 (Giám khảo Tài chính & Giá)
            var evalUser3 = await context.Users.FirstOrDefaultAsync(u => u.Email == "evaluator3@procurement.com");
            if (evalUser3 == null)
            {
                evalUser3 = new User
                {
                    FullName = "Pham Minh Tai - Giam Khao Tai Chinh",
                    Email = "evaluator3@procurement.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
                    Phone = "0904444444",
                    IsActive = true
                };
                await context.Users.AddAsync(evalUser3);
                await context.SaveChangesAsync();

                await context.UserRoles.AddAsync(new UserRole
                {
                    UserId = evalUser3.Id,
                    RoleId = evalRole.Id
                });
                await context.SaveChangesAsync();
            }

            // 3. Seed Contractor Users & Verified Profiles
            // 3.1 Contractor 1: Tech Corp
            var cont1User = await context.Users.FirstOrDefaultAsync(u => u.Email == "contractor1@test.com");
            if (cont1User == null)
            {
                cont1User = new User
                {
                    FullName = "Nguyen Van Contractor",
                    Email = "contractor1@test.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
                    Phone = "0911223344",
                    IsActive = true
                };
                await context.Users.AddAsync(cont1User);
                await context.SaveChangesAsync();

                await context.UserRoles.AddAsync(new UserRole
                {
                    UserId = cont1User.Id,
                    RoleId = contractorRole.Id
                });
                await context.SaveChangesAsync();
            }

            var contractor1 = await context.Contractors.FirstOrDefaultAsync(c => c.UserId == cont1User.Id);
            if (contractor1 == null)
            {
                contractor1 = new Contractor
                {
                    UserId = cont1User.Id,
                    CompanyName = "CÔNG TY CỔ PHẦN CÔNG NGHỆ TECH CORP",
                    TaxCode = "0300588569",
                    Address = "123 Cộng Hòa, Tân Bình, TP.HCM",
                    Rating = 96.00m,
                    VerificationStatus = "Approved",
                    IsDigitalSigned = true,
                    DigitalSignerName = "Nguyen Van Contractor",
                    DigitalSignDate = DateTime.UtcNow.AddMonths(-2),
                    DigitalSignMessage = "Chứng thư số CA hợp lệ"
                };
                await context.Contractors.AddAsync(contractor1);
                await context.SaveChangesAsync();
            }

            // 3.2 Contractor 2: ABC Software
            var cont2User = await context.Users.FirstOrDefaultAsync(u => u.Email == "contractor2@test.com");
            if (cont2User == null)
            {
                cont2User = new User
                {
                    FullName = "Tran Van Contractor",
                    Email = "contractor2@test.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
                    Phone = "0912345678",
                    IsActive = true
                };
                await context.Users.AddAsync(cont2User);
                await context.SaveChangesAsync();

                await context.UserRoles.AddAsync(new UserRole
                {
                    UserId = cont2User.Id,
                    RoleId = contractorRole.Id
                });
                await context.SaveChangesAsync();
            }

            var contractor2 = await context.Contractors.FirstOrDefaultAsync(c => c.UserId == cont2User.Id);
            if (contractor2 == null)
            {
                contractor2 = new Contractor
                {
                    UserId = cont2User.Id,
                    CompanyName = "CÔNG TY CỔ PHẦN PHẦN MỀM ABC",
                    TaxCode = "0102030405",
                    Address = "456 Lê Duẩn, Quận 1, TP.HCM",
                    Rating = 90.00m,
                    VerificationStatus = "Approved",
                    IsDigitalSigned = true,
                    DigitalSignerName = "Tran Van Contractor",
                    DigitalSignDate = DateTime.UtcNow.AddMonths(-2),
                    DigitalSignMessage = "Chứng thư số CA hợp lệ"
                };
                await context.Contractors.AddAsync(contractor2);
                await context.SaveChangesAsync();
            }

            // 3.3 Contractor 3: Khang Nghị (Tài khoản chính của User)
            var khangNghiUser = await context.Users.FirstOrDefaultAsync(u => u.Email == "nguyen221205@gmail.com");
            if (khangNghiUser == null)
            {
                khangNghiUser = new User
                {
                    FullName = "Phạm Minh Tài",
                    Email = "nguyen221205@gmail.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
                    Phone = "0938123456",
                    IsActive = true
                };
                await context.Users.AddAsync(khangNghiUser);
                await context.SaveChangesAsync();

                await context.UserRoles.AddAsync(new UserRole
                {
                    UserId = khangNghiUser.Id,
                    RoleId = contractorRole.Id
                });
                await context.SaveChangesAsync();
            }

            var contractorKhangNghi = await context.Contractors.FirstOrDefaultAsync(c => c.UserId == khangNghiUser.Id);
            if (contractorKhangNghi == null)
            {
                contractorKhangNghi = new Contractor
                {
                    UserId = khangNghiUser.Id,
                    CompanyName = "Công ty TNHH Thương mại và Dịch vụ Kỹ thuật Khang Nghị",
                    TaxCode = "0318059830",
                    Address = "Số 45/12 Đường 3/2, Phường 11, Quận 10, TP. Hồ Chí Minh",
                    Rating = 98.00m,
                    VerificationStatus = "Approved",
                    IsDigitalSigned = true,
                    DigitalSignerName = "Phạm Minh Tài",
                    DigitalSignDate = DateTime.UtcNow.AddMonths(-3),
                    DigitalSignMessage = "Chứng thư số USB Token Viettel-CA hợp lệ"
                };
                await context.Contractors.AddAsync(contractorKhangNghi);
                await context.SaveChangesAsync();
            }

            // 3.4 Contractor 4: TEL4VN (Tài khoản kiểm thử đối tác của User)
            var tel4vnUser = await context.Users.FirstOrDefaultAsync(u => u.Email == "phamtai221205@gmail.com");
            if (tel4vnUser == null)
            {
                tel4vnUser = new User
                {
                    FullName = "Trần Nguyên - TEL4VN",
                    Email = "phamtai221205@gmail.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
                    Phone = "0909888999",
                    IsActive = true
                };
                await context.Users.AddAsync(tel4vnUser);
                await context.SaveChangesAsync();

                await context.UserRoles.AddAsync(new UserRole
                {
                    UserId = tel4vnUser.Id,
                    RoleId = contractorRole.Id
                });
                await context.SaveChangesAsync();
            }

            var contractorTel4vn = await context.Contractors.FirstOrDefaultAsync(c => c.UserId == tel4vnUser.Id);
            if (contractorTel4vn == null)
            {
                contractorTel4vn = new Contractor
                {
                    UserId = tel4vnUser.Id,
                    CompanyName = "CÔNG TY TNHH GIẢI PHÁP VIỄN THÔNG TEL4VN",
                    TaxCode = "0314758161",
                    Address = "40/12 Huỳnh Mẫn Đạt, Phường 19, Quận Bình Thạnh, TP. Hồ Chí Minh",
                    Rating = 97.00m,
                    VerificationStatus = "Approved",
                    IsDigitalSigned = true,
                    DigitalSignerName = "Trần Nguyên",
                    DigitalSignDate = DateTime.UtcNow.AddMonths(-2),
                    DigitalSignMessage = "Chứng thư số VNPT-CA hợp lệ"
                };
                await context.Contractors.AddAsync(contractorTel4vn);
                await context.SaveChangesAsync();
            }

            // 4. Seed Standard Lifecycle Bid Packages (Tuân thủ triệt để Luật Đấu thầu 2023 & Business Rules N1-N26)
            var procUserId = procUser?.Id ?? adminUser.Id;
            var evaluators = new[] { evalUser.Id, evalUser2.Id, evalUser3.Id };

            // =========================================================================
            // 4.1 GÓI 1: Trạng thái OPEN (Đang tiếp nhận E-HSDT, hạn tương lai)
            // =========================================================================
            if (!await context.BidPackages.AnyAsync(p => p.Code == "PKG-2026-0001"))
            {
                var pkg1 = new BidPackage
                {
                    Code = "PKG-2026-0001",
                    Name = "Gói thầu Mua sắm Hệ thống Máy chủ & Thiết bị Lưu trữ DC 2026",
                    Type = BidPackageType.Goods,
                    Budget = 1500000000m,
                    Deadline = DateTime.UtcNow.AddDays(30),
                    Status = BidPackageStatus.Open,
                    Description = "Cung cấp máy chủ rack và hệ thống SAN Storage phục vụ mở rộng dung lượng xử lý cơ sở dữ liệu doanh nghiệp.",
                    CreatedBy = procUserId,
                    CreatedAt = DateTime.UtcNow.AddDays(-3)
                };
                await context.BidPackages.AddAsync(pkg1);
                await context.SaveChangesAsync();

                // Gán Tổ chuyên gia (Rule N4: Tối thiểu 3 thành viên số lẻ)
                foreach (var evId in evaluators)
                {
                    await context.BidPackageEvaluators.AddAsync(new BidPackageEvaluator
                    {
                        BidPackageId = pkg1.Id,
                        EvaluatorId = evId,
                        AssignedBy = procUserId,
                        AssignedAt = DateTime.UtcNow.AddDays(-3)
                    });
                }

                // Tiêu chí đánh giá (Rule N5: Đúng 1 tiêu chí giá, tổng trọng số = 100%)
                await context.EvaluationCriteria.AddRangeAsync(
                    new EvaluationCriteria
                    {
                        BidPackageId = pkg1.Id,
                        Name = "Năng lực kỹ thuật, cấu hình phần cứng và kiến trúc mở rộng",
                        MaxScore = 100,
                        Weight = 60,
                        IsPriceCriteria = false
                    },
                    new EvaluationCriteria
                    {
                        BidPackageId = pkg1.Id,
                        Name = "Đơn giá dự thầu và cam kết SLA thay thế linh kiện 24/7",
                        MaxScore = 100,
                        Weight = 40,
                        IsPriceCriteria = true
                    }
                );
                await context.SaveChangesAsync();
            }

            // =========================================================================
            // 4.2 GÓI 2: Trạng thái CLOSED (Đã đóng thầu, chờ mở thầu)
            // =========================================================================
            if (!await context.BidPackages.AnyAsync(p => p.Code == "PKG-2026-0002"))
            {
                var pkg2 = new BidPackage
                {
                    Code = "PKG-2026-0002",
                    Name = "Gói thầu Mua sắm Bản quyền Hệ điều hành và Phần mềm Văn phòng 2026",
                    Type = BidPackageType.Goods,
                    Budget = 500000000m,
                    Deadline = DateTime.UtcNow.AddHours(-4),
                    Status = BidPackageStatus.Closed,
                    Description = "Trang bị gói bản quyền Windows Server và Microsoft Office thương mại cho cơ quan hành chính.",
                    CreatedBy = procUserId,
                    CreatedAt = DateTime.UtcNow.AddDays(-10)
                };
                await context.BidPackages.AddAsync(pkg2);
                await context.SaveChangesAsync();

                // Gán Tổ chuyên gia
                foreach (var evId in evaluators)
                {
                    await context.BidPackageEvaluators.AddAsync(new BidPackageEvaluator
                    {
                        BidPackageId = pkg2.Id,
                        EvaluatorId = evId,
                        AssignedBy = procUserId,
                        AssignedAt = DateTime.UtcNow.AddDays(-10)
                    });
                }

                // Tiêu chí đánh giá (Rule N5)
                await context.EvaluationCriteria.AddRangeAsync(
                    new EvaluationCriteria
                    {
                        BidPackageId = pkg2.Id,
                        Name = "Tính tương thích hệ điều hành và chứng chỉ bản quyền OEM/FPP",
                        MaxScore = 100,
                        Weight = 60,
                        IsPriceCriteria = false
                    },
                    new EvaluationCriteria
                    {
                        BidPackageId = pkg2.Id,
                        Name = "Đơn giá gói bản quyền và dịch vụ hỗ trợ kích hoạt, cài đặt",
                        MaxScore = 100,
                        Weight = 40,
                        IsPriceCriteria = true
                    }
                );

                // 2 Nhà thầu nộp trước thời hạn đóng thầu
                await context.BidSubmissions.AddRangeAsync(
                    new BidSubmission
                    {
                        BidPackageId = pkg2.Id,
                        ContractorId = contractor1.Id,
                        BidPrice = 480000000m,
                        SubmittedAt = DateTime.UtcNow.AddDays(-2),
                        Status = "Submitted"
                    },
                    new BidSubmission
                    {
                        BidPackageId = pkg2.Id,
                        ContractorId = contractorTel4vn.Id,
                        BidPrice = 495000000m,
                        SubmittedAt = DateTime.UtcNow.AddDays(-1),
                        Status = "Submitted"
                    }
                );
                await context.SaveChangesAsync();
            }

            // =========================================================================
            // 4.3 GÓI 3: Trạng thái EVALUATING (Đang chấm điểm, Tổ chuyên gia đang đánh giá)
            // =========================================================================
            if (!await context.BidPackages.AnyAsync(p => p.Code == "PKG-2026-0003"))
            {
                var pkg3 = new BidPackage
                {
                    Code = "PKG-2026-0003",
                    Name = "Gói thầu Triển khai Phần mềm Quản trị Doanh nghiệp ERP",
                    Type = BidPackageType.Service,
                    Budget = 2000000000m,
                    Deadline = DateTime.UtcNow.AddDays(-5),
                    Status = BidPackageStatus.Evaluating,
                    Description = "Xây dựng giải pháp ERP tích hợp các phân hệ quản lý mua sắm thầu, kho bãi và kế toán tài chính.",
                    CreatedBy = procUserId,
                    CreatedAt = DateTime.UtcNow.AddDays(-15)
                };
                await context.BidPackages.AddAsync(pkg3);
                await context.SaveChangesAsync();

                // Gán Tổ chuyên gia
                foreach (var evId in evaluators)
                {
                    await context.BidPackageEvaluators.AddAsync(new BidPackageEvaluator
                    {
                        BidPackageId = pkg3.Id,
                        EvaluatorId = evId,
                        AssignedBy = procUserId,
                        AssignedAt = DateTime.UtcNow.AddDays(-15)
                    });
                }

                // Tiêu chí đánh giá
                var p3Crit1 = new EvaluationCriteria
                {
                    BidPackageId = pkg3.Id,
                    Name = "Năng lực giải pháp phần mềm và tính tương thích kiến trúc hệ thống",
                    MaxScore = 100,
                    Weight = 60,
                    IsPriceCriteria = false
                };
                var p3Crit2 = new EvaluationCriteria
                {
                    BidPackageId = pkg3.Id,
                    Name = "Chi phí bản quyền, triển khai dịch vụ và chuyển giao công nghệ",
                    MaxScore = 100,
                    Weight = 40,
                    IsPriceCriteria = true
                };
                await context.EvaluationCriteria.AddRangeAsync(p3Crit1, p3Crit2);
                await context.SaveChangesAsync();

                // 2 Nhà thầu nộp hồ sơ
                var sub3_1 = new BidSubmission
                {
                    BidPackageId = pkg3.Id,
                    ContractorId = contractorKhangNghi.Id,
                    BidPrice = 1850000000m,
                    SubmittedAt = DateTime.UtcNow.AddDays(-6),
                    Status = "Submitted"
                };
                var sub3_2 = new BidSubmission
                {
                    BidPackageId = pkg3.Id,
                    ContractorId = contractor2.Id,
                    BidPrice = 1920000000m,
                    SubmittedAt = DateTime.UtcNow.AddDays(-6),
                    Status = "Submitted"
                };
                await context.BidSubmissions.AddRangeAsync(sub3_1, sub3_2);
                await context.SaveChangesAsync();

                // Điểm chấm đang diễn ra: Giám khảo 1 chấm cả 2, Giám khảo 2 chấm 1, Giám khảo 3 chưa chấm
                // Tuyệt đối không set TotalScore hay Rank ở giai đoạn này (Rule N20: che giấu khi đang chấm)
                await context.EvaluationScores.AddRangeAsync(
                    new EvaluationScore
                    {
                        BidSubmissionId = sub3_1.Id,
                        CriteriaId = p3Crit1.Id,
                        EvaluatorId = evalUser.Id,
                        Score = 90.0m,
                        Comment = "Giải pháp module linh hoạt, giao diện thân thiện",
                        ScoredAt = DateTime.UtcNow.AddDays(-2)
                    },
                    new EvaluationScore
                    {
                        BidSubmissionId = sub3_1.Id,
                        CriteriaId = p3Crit2.Id,
                        EvaluatorId = evalUser.Id,
                        Score = 88.0m,
                        Comment = "Chi phí bảo trì năm thứ 2 hợp lý",
                        ScoredAt = DateTime.UtcNow.AddDays(-2)
                    },
                    new EvaluationScore
                    {
                        BidSubmissionId = sub3_2.Id,
                        CriteriaId = p3Crit1.Id,
                        EvaluatorId = evalUser.Id,
                        Score = 82.0m,
                        Comment = "Đáp ứng cơ bản yêu cầu tính năng",
                        ScoredAt = DateTime.UtcNow.AddDays(-2)
                    },
                    new EvaluationScore
                    {
                        BidSubmissionId = sub3_2.Id,
                        CriteriaId = p3Crit2.Id,
                        EvaluatorId = evalUser.Id,
                        Score = 80.0m,
                        Comment = "Đơn giá bản quyền cao hơn tương đối",
                        ScoredAt = DateTime.UtcNow.AddDays(-2)
                    },
                    new EvaluationScore
                    {
                        BidSubmissionId = sub3_1.Id,
                        CriteriaId = p3Crit1.Id,
                        EvaluatorId = evalUser2.Id,
                        Score = 92.0m,
                        Comment = "Kiến trúc microservices hiện đại, tương thích tốt",
                        ScoredAt = DateTime.UtcNow.AddDays(-1)
                    },
                    new EvaluationScore
                    {
                        BidSubmissionId = sub3_1.Id,
                        CriteriaId = p3Crit2.Id,
                        EvaluatorId = evalUser2.Id,
                        Score = 86.0m,
                        Comment = "Kế hoạch chuyển giao công nghệ chi tiết",
                        ScoredAt = DateTime.UtcNow.AddDays(-1)
                    }
                );
                await context.SaveChangesAsync();
            }

            // =========================================================================
            // 4.4 GÓI 4: Trạng thái AWARDED (Đã phê duyệt kết quả chọn thầu, đầy đủ điểm)
            // =========================================================================
            if (!await context.BidPackages.AnyAsync(p => p.Code == "PKG-2026-0004"))
            {
                var pkg4 = new BidPackage
                {
                    Code = "PKG-2026-0004",
                    Name = "Gói thầu Nâng cấp Hạ tầng Mạng & Bảo mật Trung tâm Điều hành 2026",
                    Type = BidPackageType.Goods,
                    Budget = 800000000m,
                    Deadline = DateTime.UtcNow.AddDays(-15),
                    Status = BidPackageStatus.Awarded,
                    Description = "Cung cấp thiết bị chuyển mạch Core Switch, Firewall thế hệ mới và dịch vụ cấu hình an ninh thông tin.",
                    CreatedBy = procUserId,
                    CreatedAt = DateTime.UtcNow.AddDays(-25)
                };
                await context.BidPackages.AddAsync(pkg4);
                await context.SaveChangesAsync();

                // Gán Tổ chuyên gia
                foreach (var evId in evaluators)
                {
                    await context.BidPackageEvaluators.AddAsync(new BidPackageEvaluator
                    {
                        BidPackageId = pkg4.Id,
                        EvaluatorId = evId,
                        AssignedBy = procUserId,
                        AssignedAt = DateTime.UtcNow.AddDays(-25)
                    });
                }

                var p4Crit1 = new EvaluationCriteria
                {
                    BidPackageId = pkg4.Id,
                    Name = "Năng lực thiết bị mạng, thông lượng tường lửa và giải pháp bảo mật UTM",
                    MaxScore = 100,
                    Weight = 60,
                    IsPriceCriteria = false
                };
                var p4Crit2 = new EvaluationCriteria
                {
                    BidPackageId = pkg4.Id,
                    Name = "Giá dự thầu và gói dịch vụ bảo trì, ứng cứu sự cố 24/7",
                    MaxScore = 100,
                    Weight = 40,
                    IsPriceCriteria = true
                };
                await context.EvaluationCriteria.AddRangeAsync(p4Crit1, p4Crit2);
                await context.SaveChangesAsync();

                // 2 Hồ sơ: Tech Corp trúng thầu (Rank 1, Selected), ABC Software (Rank 2, Rejected)
                var sub4_1 = new BidSubmission
                {
                    BidPackageId = pkg4.Id,
                    ContractorId = contractor1.Id,
                    BidPrice = 760000000m,
                    SubmittedAt = DateTime.UtcNow.AddDays(-18),
                    TotalScore = 91.50m,
                    Rank = 1,
                    Status = "Selected",
                    SelectionReason = "Đáp ứng xuất sắc yêu cầu kỹ thuật, đạt điểm đánh giá cao nhất và giá dự thầu cạnh tranh, tiết kiệm ngân sách nhà nước."
                };
                var sub4_2 = new BidSubmission
                {
                    BidPackageId = pkg4.Id,
                    ContractorId = contractor2.Id,
                    BidPrice = 785000000m,
                    SubmittedAt = DateTime.UtcNow.AddDays(-17),
                    TotalScore = 83.20m,
                    Rank = 2,
                    Status = "Rejected",
                    SelectionReason = "Điểm đánh giá kỹ thuật thấp hơn và giá dự thầu cao hơn nhà thầu xếp hạng 1."
                };
                await context.BidSubmissions.AddRangeAsync(sub4_1, sub4_2);
                await context.SaveChangesAsync();

                // Cả 3 giám khảo chấm điểm đầy đủ
                await context.EvaluationScores.AddRangeAsync(
                    // Giám khảo 1
                    new EvaluationScore { BidSubmissionId = sub4_1.Id, CriteriaId = p4Crit1.Id, EvaluatorId = evalUser.Id, Score = 92.0m, ScoredAt = DateTime.UtcNow.AddDays(-10) },
                    new EvaluationScore { BidSubmissionId = sub4_1.Id, CriteriaId = p4Crit2.Id, EvaluatorId = evalUser.Id, Score = 90.0m, ScoredAt = DateTime.UtcNow.AddDays(-10) },
                    new EvaluationScore { BidSubmissionId = sub4_2.Id, CriteriaId = p4Crit1.Id, EvaluatorId = evalUser.Id, Score = 84.0m, ScoredAt = DateTime.UtcNow.AddDays(-10) },
                    new EvaluationScore { BidSubmissionId = sub4_2.Id, CriteriaId = p4Crit2.Id, EvaluatorId = evalUser.Id, Score = 82.0m, ScoredAt = DateTime.UtcNow.AddDays(-10) },
                    // Giám khảo 2
                    new EvaluationScore { BidSubmissionId = sub4_1.Id, CriteriaId = p4Crit1.Id, EvaluatorId = evalUser2.Id, Score = 94.0m, ScoredAt = DateTime.UtcNow.AddDays(-9) },
                    new EvaluationScore { BidSubmissionId = sub4_1.Id, CriteriaId = p4Crit2.Id, EvaluatorId = evalUser2.Id, Score = 91.0m, ScoredAt = DateTime.UtcNow.AddDays(-9) },
                    new EvaluationScore { BidSubmissionId = sub4_2.Id, CriteriaId = p4Crit1.Id, EvaluatorId = evalUser2.Id, Score = 85.0m, ScoredAt = DateTime.UtcNow.AddDays(-9) },
                    new EvaluationScore { BidSubmissionId = sub4_2.Id, CriteriaId = p4Crit2.Id, EvaluatorId = evalUser2.Id, Score = 83.0m, ScoredAt = DateTime.UtcNow.AddDays(-9) },
                    // Giám khảo 3
                    new EvaluationScore { BidSubmissionId = sub4_1.Id, CriteriaId = p4Crit1.Id, EvaluatorId = evalUser3.Id, Score = 90.0m, ScoredAt = DateTime.UtcNow.AddDays(-8) },
                    new EvaluationScore { BidSubmissionId = sub4_1.Id, CriteriaId = p4Crit2.Id, EvaluatorId = evalUser3.Id, Score = 92.0m, ScoredAt = DateTime.UtcNow.AddDays(-8) },
                    new EvaluationScore { BidSubmissionId = sub4_2.Id, CriteriaId = p4Crit1.Id, EvaluatorId = evalUser3.Id, Score = 82.0m, ScoredAt = DateTime.UtcNow.AddDays(-8) },
                    new EvaluationScore { BidSubmissionId = sub4_2.Id, CriteriaId = p4Crit2.Id, EvaluatorId = evalUser3.Id, Score = 83.0m, ScoredAt = DateTime.UtcNow.AddDays(-8) }
                );
                await context.SaveChangesAsync();
            }

            // =========================================================================
            // 4.5 GÓI 5: Trạng thái CONTRACTED (Đã ký hợp đồng & nghiệm thu, Rule N9)
            // =========================================================================
            if (!await context.BidPackages.AnyAsync(p => p.Code == "PKG-2026-0005"))
            {
                var pkg5 = new BidPackage
                {
                    Code = "PKG-2026-0005",
                    Name = "Gói thầu Thuê Dịch vụ Hạ tầng Đám mây & Trung tâm Dữ liệu Dự phòng",
                    Type = BidPackageType.Service,
                    Budget = 1200000000m,
                    Deadline = DateTime.UtcNow.AddDays(-30),
                    Status = BidPackageStatus.Contracted,
                    Description = "Dịch vụ Cloud IaaS chuẩn Tier III phục vụ lưu trữ dữ liệu an toàn và dự phòng thảm họa.",
                    CreatedBy = procUserId,
                    CreatedAt = DateTime.UtcNow.AddDays(-40)
                };
                await context.BidPackages.AddAsync(pkg5);
                await context.SaveChangesAsync();

                // Gán Tổ chuyên gia
                foreach (var evId in evaluators)
                {
                    await context.BidPackageEvaluators.AddAsync(new BidPackageEvaluator
                    {
                        BidPackageId = pkg5.Id,
                        EvaluatorId = evId,
                        AssignedBy = procUserId,
                        AssignedAt = DateTime.UtcNow.AddDays(-40)
                    });
                }

                var p5Crit1 = new EvaluationCriteria
                {
                    BidPackageId = pkg5.Id,
                    Name = "Năng lực hạ tầng đám mây đạt chuẩn Tier III và cam kết SLA 99.99%",
                    MaxScore = 100,
                    Weight = 60,
                    IsPriceCriteria = false
                };
                var p5Crit2 = new EvaluationCriteria
                {
                    BidPackageId = pkg5.Id,
                    Name = "Đơn giá dịch vụ và chính sách tối ưu hóa chi phí",
                    MaxScore = 100,
                    Weight = 40,
                    IsPriceCriteria = true
                };
                await context.EvaluationCriteria.AddRangeAsync(p5Crit1, p5Crit2);
                await context.SaveChangesAsync();

                // Khang Nghị trúng thầu (Selected, Rank 1)
                var sub5_Winner = new BidSubmission
                {
                    BidPackageId = pkg5.Id,
                    ContractorId = contractorKhangNghi.Id,
                    BidPrice = 1150000000m,
                    SubmittedAt = DateTime.UtcNow.AddDays(-32),
                    TotalScore = 93.50m,
                    Rank = 1,
                    Status = "Selected",
                    SelectionReason = "Giải pháp đám mây đạt chuẩn an toàn thông tin ISO 27001, cam kết SLA cao và tối ưu chi phí vận hành."
                };
                var sub5_Competitor = new BidSubmission
                {
                    BidPackageId = pkg5.Id,
                    ContractorId = contractor1.Id,
                    BidPrice = 1180000000m,
                    SubmittedAt = DateTime.UtcNow.AddDays(-31),
                    TotalScore = 89.00m,
                    Rank = 2,
                    Status = "Rejected",
                    SelectionReason = "Giá dự thầu cao hơn và cam kết SLA dự phòng thấp hơn nhà thầu xếp hạng 1."
                };
                await context.BidSubmissions.AddRangeAsync(sub5_Winner, sub5_Competitor);
                await context.SaveChangesAsync();

                // Cả 3 giám khảo chấm điểm
                await context.EvaluationScores.AddRangeAsync(
                    // Giám khảo 1
                    new EvaluationScore { BidSubmissionId = sub5_Winner.Id, CriteriaId = p5Crit1.Id, EvaluatorId = evalUser.Id, Score = 94.0m, ScoredAt = DateTime.UtcNow.AddDays(-25) },
                    new EvaluationScore { BidSubmissionId = sub5_Winner.Id, CriteriaId = p5Crit2.Id, EvaluatorId = evalUser.Id, Score = 92.0m, ScoredAt = DateTime.UtcNow.AddDays(-25) },
                    new EvaluationScore { BidSubmissionId = sub5_Competitor.Id, CriteriaId = p5Crit1.Id, EvaluatorId = evalUser.Id, Score = 89.0m, ScoredAt = DateTime.UtcNow.AddDays(-25) },
                    new EvaluationScore { BidSubmissionId = sub5_Competitor.Id, CriteriaId = p5Crit2.Id, EvaluatorId = evalUser.Id, Score = 88.0m, ScoredAt = DateTime.UtcNow.AddDays(-25) },
                    // Giám khảo 2
                    new EvaluationScore { BidSubmissionId = sub5_Winner.Id, CriteriaId = p5Crit1.Id, EvaluatorId = evalUser2.Id, Score = 95.0m, ScoredAt = DateTime.UtcNow.AddDays(-24) },
                    new EvaluationScore { BidSubmissionId = sub5_Winner.Id, CriteriaId = p5Crit2.Id, EvaluatorId = evalUser2.Id, Score = 93.0m, ScoredAt = DateTime.UtcNow.AddDays(-24) },
                    new EvaluationScore { BidSubmissionId = sub5_Competitor.Id, CriteriaId = p5Crit1.Id, EvaluatorId = evalUser2.Id, Score = 90.0m, ScoredAt = DateTime.UtcNow.AddDays(-24) },
                    new EvaluationScore { BidSubmissionId = sub5_Competitor.Id, CriteriaId = p5Crit2.Id, EvaluatorId = evalUser2.Id, Score = 89.0m, ScoredAt = DateTime.UtcNow.AddDays(-24) },
                    // Giám khảo 3
                    new EvaluationScore { BidSubmissionId = sub5_Winner.Id, CriteriaId = p5Crit1.Id, EvaluatorId = evalUser3.Id, Score = 93.0m, ScoredAt = DateTime.UtcNow.AddDays(-23) },
                    new EvaluationScore { BidSubmissionId = sub5_Winner.Id, CriteriaId = p5Crit2.Id, EvaluatorId = evalUser3.Id, Score = 94.0m, ScoredAt = DateTime.UtcNow.AddDays(-23) },
                    new EvaluationScore { BidSubmissionId = sub5_Competitor.Id, CriteriaId = p5Crit1.Id, EvaluatorId = evalUser3.Id, Score = 88.0m, ScoredAt = DateTime.UtcNow.AddDays(-23) },
                    new EvaluationScore { BidSubmissionId = sub5_Competitor.Id, CriteriaId = p5Crit2.Id, EvaluatorId = evalUser3.Id, Score = 90.0m, ScoredAt = DateTime.UtcNow.AddDays(-23) }
                );
                await context.SaveChangesAsync();

                // Hợp đồng chính thức (Rule N9: Active contract requires scanned PDF)
                var sampleContract = new Contract
                {
                    BidPackageId = pkg5.Id,
                    ContractorId = contractorKhangNghi.Id,
                    ContractNumber = "CTR-2026-CLOUD-01",
                    Value = 1150000000m,
                    Terms = "Bảo hành và hỗ trợ kỹ thuật 24/7 trong 12 tháng kể từ ngày ký biên bản bàn giao.",
                    StartDate = DateTime.UtcNow.AddDays(-20),
                    EndDate = DateTime.UtcNow.AddDays(345),
                    Status = ContractStatus.Active,
                    ScannedFilePath = "/uploads/contracts/sample_signed_contract.pdf",
                    CreatedAt = DateTime.UtcNow.AddDays(-20)
                };
                await context.Contracts.AddAsync(sampleContract);
                await context.SaveChangesAsync();

                // Mốc nghiệm thu (Rule N9: Tổng số tiền các mốc = 100% giá trị hợp đồng: 500M + 400M + 250M = 1,150,000,000m)
                await context.ContractMilestones.AddRangeAsync(
                    new ContractMilestone
                    {
                        ContractId = sampleContract.Id,
                        Title = "Nghiệm thu Giai đoạn 1: Bàn giao kiến trúc và cấu hình hệ thống Cloud ban đầu",
                        DueDate = DateTime.UtcNow.AddDays(-5),
                        Amount = 500000000m,
                        Status = MilestoneStatus.Completed
                    },
                    new ContractMilestone
                    {
                        ContractId = sampleContract.Id,
                        Title = "Nghiệm thu Giai đoạn 2: Triển khai toàn diện và di chuyển ứng dụng",
                        DueDate = DateTime.UtcNow.AddDays(90),
                        Amount = 400000000m,
                        Status = MilestoneStatus.InProgress
                    },
                    new ContractMilestone
                    {
                        ContractId = sampleContract.Id,
                        Title = "Nghiệm thu Giai đoạn 3: Bàn giao tổng thể và chuyển giao vận hành",
                        DueDate = DateTime.UtcNow.AddDays(180),
                        Amount = 250000000m,
                        Status = MilestoneStatus.Pending
                    }
                );
                await context.SaveChangesAsync();
            }
        }
    }
}
