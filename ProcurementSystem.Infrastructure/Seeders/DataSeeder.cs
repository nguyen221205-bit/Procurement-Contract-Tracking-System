using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
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

            // Seed Roles
            if (!await context.Roles.AnyAsync())
            {
                var roles = new List<Role>
                {
                    new() { Name = "Admin", Description = "Quản trị viên hệ thống" },
                    new() { Name = "Procurement", Description = "Bộ phận mua sắm" },
                    new() { Name = "Evaluator", Description = "Ban chấm điểm" },
                    new() { Name = "Contractor", Description = "Nhà thầu" }
                };

                await context.Roles.AddRangeAsync(roles);
                await context.SaveChangesAsync();
            }

            // Seed Admin user
            if (!await context.Users.AnyAsync(u => u.Email == "admin@procurement.com"))
            {
                var adminUser = new User
                {
                    FullName = "System Administrator",
                    Email = "admin@procurement.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
                    Phone = "0900000000",
                    IsActive = true
                };

                await context.Users.AddAsync(adminUser);
                await context.SaveChangesAsync();

                // Assign Admin role
                var adminRole = await context.Roles.FirstAsync(r => r.Name == "Admin");
                await context.UserRoles.AddAsync(new UserRole
                {
                    UserId = adminUser.Id,
                    RoleId = adminRole.Id
                });
                await context.SaveChangesAsync();
            }
        }
    }
}
