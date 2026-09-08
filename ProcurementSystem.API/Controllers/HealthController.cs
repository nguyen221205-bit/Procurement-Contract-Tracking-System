using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProcurementSystem.Core.DTOs;
using ProcurementSystem.Infrastructure.Data;

namespace ProcurementSystem.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HealthController : ControllerBase
    {
        private readonly AppDbContext _context;

        public HealthController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> CheckHealth()
        {
            var canConnect = await _context.Database.CanConnectAsync();
            var rolesCount = await _context.Roles.CountAsync();
            var usersCount = await _context.Users.CountAsync();

            var data = new
            {
                Status = "Healthy",
                DatabaseConnected = canConnect,
                RolesCount = rolesCount,
                UsersCount = usersCount,
                Timestamp = DateTime.UtcNow
            };

            return Ok(ApiResponse<object>.SuccessResponse(data, "Hệ thống đang hoạt động bình thường"));
        }
    }
}
