using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProcurementSystem.Core.DTOs;
using ProcurementSystem.Infrastructure.Data;

namespace ProcurementSystem.API.Controllers
{
    /// <summary>
    /// Bộ điều khiển kiểm tra trạng thái hoạt động của hệ thống và kết nối cơ sở dữ liệu
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class HealthController : ControllerBase
    {
        private readonly AppDbContext _context;

        public HealthController(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Kiểm tra sức khỏe hệ thống (Health Check), kiểm tra kết nối cơ sở dữ liệu và tổng số tài khoản
        /// </summary>
        /// <returns>Trạng thái hoạt động chi tiết của máy chủ Web API và kết nối CSDL SQL Server</returns>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
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
