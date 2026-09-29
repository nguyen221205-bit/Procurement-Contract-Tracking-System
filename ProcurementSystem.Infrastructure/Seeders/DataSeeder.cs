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

            // 2. Seed Internal Users (Admin, Procurement, Evaluator)
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

            // 2.3 Evaluator
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

            // 3. Seed Contractor Users & Contractor Entities
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
                    CompanyName = "CONG TY CO PHAN CONG NGHE TEST",
                    TaxCode = "0300588569",
                    Address = "123 Cong Hoa, Tan Binh, TP.HCM",
                    Rating = 4.80m
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
                    CompanyName = "CONG TY CO PHAN PHAN MEM ABC",
                    TaxCode = "0102030405",
                    Address = "456 Le Duan, Quan 1, TP.HCM",
                    Rating = 4.50m
                };
                await context.Contractors.AddAsync(contractor2);
                await context.SaveChangesAsync();
            }

            // 4. Seed Bid Packages across Full Lifecycles
            // 4.1 Sample Package 1: Status = Open
            if (!await context.BidPackages.AnyAsync(p => p.Code == "PKG-SAMPLE-OPEN"))
            {
                var pkgOpen = new BidPackage
                {
                    Code = "PKG-SAMPLE-OPEN",
                    Name = "Gói thầu Mua sắm Hệ thống Máy chủ & Thiết bị Lưu trữ DC 2026",
                    Type = BidPackageType.Goods,
                    Budget = 1500000000,
                    Deadline = DateTime.UtcNow.AddDays(30),
                    Status = BidPackageStatus.Open,
                    Description = "Cung cấp máy chủ rack và hệ thống SAN Storage phục vụ mở rộng dung lượng xử lý cơ sở dữ liệu doanh nghiệp",
                    CreatedBy = adminUser.Id,
                    CreatedAt = DateTime.UtcNow.AddDays(-3)
                };
                await context.BidPackages.AddAsync(pkgOpen);
                await context.SaveChangesAsync();

                // Criteria for Open package
                await context.EvaluationCriteria.AddRangeAsync(
                    new EvaluationCriteria
                    {
                        BidPackageId = pkgOpen.Id,
                        Name = "Năng lực kỹ thuật, cấu hình phần cứng và kiến trúc mở rộng",
                        MaxScore = 100,
                        Weight = 60
                    },
                    new EvaluationCriteria
                    {
                        BidPackageId = pkgOpen.Id,
                        Name = "Thời gian bảo hành, cam kết SLA thay thế linh kiện 24/7",
                        MaxScore = 100,
                        Weight = 40
                    }
                );
                await context.SaveChangesAsync();
            }

            // 4.2 Sample Package 2: Status = Evaluating
            if (!await context.BidPackages.AnyAsync(p => p.Code == "PKG-SAMPLE-EVAL"))
            {
                var pkgEval = new BidPackage
                {
                    Code = "PKG-SAMPLE-EVAL",
                    Name = "Gói thầu Triển khai Phần mềm Quản trị Doanh nghiệp ERP",
                    Type = BidPackageType.Service,
                    Budget = 2000000000,
                    Deadline = DateTime.UtcNow.AddDays(-2),
                    Status = BidPackageStatus.Evaluating,
                    Description = "Xây dựng giải pháp ERP tích hợp các phân hệ quản lý mua sắm thầu, kho bãi và kế toán tài chính",
                    CreatedBy = adminUser.Id,
                    CreatedAt = DateTime.UtcNow.AddDays(-15)
                };
                await context.BidPackages.AddAsync(pkgEval);
                await context.SaveChangesAsync();

                // Criteria for Evaluating package
                var crit1 = new EvaluationCriteria
                {
                    BidPackageId = pkgEval.Id,
                    Name = "Năng lực giải pháp phần mềm và tính tương thích kiến trúc hệ thống",
                    MaxScore = 100,
                    Weight = 60
                };
                var crit2 = new EvaluationCriteria
                {
                    BidPackageId = pkgEval.Id,
                    Name = "Kế hoạch đào tạo, chuyển giao công nghệ và chi phí dịch vụ",
                    MaxScore = 100,
                    Weight = 40
                };
                await context.EvaluationCriteria.AddRangeAsync(crit1, crit2);
                await context.SaveChangesAsync();

                // 2 Submissions for this package
                var sub1 = new BidSubmission
                {
                    BidPackageId = pkgEval.Id,
                    ContractorId = contractor1.Id,
                    SubmittedAt = DateTime.UtcNow.AddDays(-4),
                    Status = "Submitted"
                };
                var sub2 = new BidSubmission
                {
                    BidPackageId = pkgEval.Id,
                    ContractorId = contractor2.Id,
                    SubmittedAt = DateTime.UtcNow.AddDays(-3),
                    Status = "Submitted"
                };
                await context.BidSubmissions.AddRangeAsync(sub1, sub2);
                await context.SaveChangesAsync();
            }

            // 4.3 Sample Package 3: Status = Contracted (Full Pipeline Winner + Contract + Milestones)
            if (!await context.BidPackages.AnyAsync(p => p.Code == "PKG-SAMPLE-CONTRACTED"))
            {
                var pkgContracted = new BidPackage
                {
                    Code = "PKG-SAMPLE-CONTRACTED",
                    Name = "Gói thầu Thuê Dịch vụ Hạ tầng Đám mây & Trung tâm Dữ liệu",
                    Type = BidPackageType.Service,
                    Budget = 1200000000,
                    Deadline = DateTime.UtcNow.AddDays(-20),
                    Status = BidPackageStatus.Contracted,
                    Description = "Dịch vụ Cloud IaaS chuẩn Tier III phục vụ lưu trữ dữ liệu an toàn và dự phòng thảm họa",
                    CreatedBy = adminUser.Id,
                    CreatedAt = DateTime.UtcNow.AddDays(-30)
                };
                await context.BidPackages.AddAsync(pkgContracted);
                await context.SaveChangesAsync();

                // Criteria
                var cCrit1 = new EvaluationCriteria
                {
                    BidPackageId = pkgContracted.Id,
                    Name = "Năng lực hạ tầng đám mây và cam kết SLA 99.99%",
                    MaxScore = 100,
                    Weight = 60
                };
                var cCrit2 = new EvaluationCriteria
                {
                    BidPackageId = pkgContracted.Id,
                    Name = "Đơn giá dịch vụ và chính sách tối ưu hóa chi phí",
                    MaxScore = 100,
                    Weight = 40
                };
                await context.EvaluationCriteria.AddRangeAsync(cCrit1, cCrit2);
                await context.SaveChangesAsync();

                // Winning Submission (Contractor 1)
                var winningSub = new BidSubmission
                {
                    BidPackageId = pkgContracted.Id,
                    ContractorId = contractor1.Id,
                    SubmittedAt = DateTime.UtcNow.AddDays(-22),
                    TotalScore = 92.50m,
                    Rank = 1,
                    Status = "Selected"
                };
                await context.BidSubmissions.AddAsync(winningSub);
                await context.SaveChangesAsync();

                // Evaluation Scores for winner
                await context.EvaluationScores.AddRangeAsync(
                    new EvaluationScore
                    {
                        BidSubmissionId = winningSub.Id,
                        CriteriaId = cCrit1.Id,
                        EvaluatorId = adminUser.Id,
                        Score = 95.0m,
                        Comment = "Hạ tầng đạt chuẩn quốc tế, chứng chỉ bảo mật ISO 27001",
                        ScoredAt = DateTime.UtcNow.AddDays(-18)
                    },
                    new EvaluationScore
                    {
                        BidSubmissionId = winningSub.Id,
                        CriteriaId = cCrit2.Id,
                        EvaluatorId = adminUser.Id,
                        Score = 88.75m,
                        Comment = "Đơn giá cạnh tranh, chính sách hỗ trợ kỹ thuật rõ ràng",
                        ScoredAt = DateTime.UtcNow.AddDays(-18)
                    }
                );
                await context.SaveChangesAsync();

                // Contract
                var sampleContract = new Contract
                {
                    BidPackageId = pkgContracted.Id,
                    ContractorId = contractor1.Id,
                    ContractNumber = "CTR-2026-CLOUD-01",
                    Value = 1150000000, // Tiết kiệm 50 triệu VNĐ so với dự toán 1.2 tỷ
                    Terms = "Bảo hành và hỗ trợ kỹ thuật 24/7 trong 12 tháng kể từ ngày ký biên bản bàn giao",
                    StartDate = DateTime.UtcNow.AddDays(-15),
                    EndDate = DateTime.UtcNow.AddDays(350),
                    Status = ContractStatus.Active,
                    CreatedAt = DateTime.UtcNow.AddDays(-15)
                };
                await context.Contracts.AddAsync(sampleContract);
                await context.SaveChangesAsync();

                // Contract Milestones
                await context.ContractMilestones.AddRangeAsync(
                    new ContractMilestone
                    {
                        ContractId = sampleContract.Id,
                        Title = "Nghiệm thu Giai đoạn 1: Bàn giao kiến trúc và cấu hình hệ thống Cloud ban đầu",
                        DueDate = DateTime.UtcNow.AddDays(-5),
                        Amount = 500000000,
                        Status = MilestoneStatus.Completed
                    },
                    new ContractMilestone
                    {
                        ContractId = sampleContract.Id,
                        Title = "Nghiệm thu Giai đoạn 2: Triển khai toàn diện và chuyển giao vận hành",
                        DueDate = DateTime.UtcNow.AddDays(180),
                        Amount = 650000000,
                        Status = MilestoneStatus.Pending
                    }
                );
                await context.SaveChangesAsync();
            }
        }
    }
}
