using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProcurementSystem.Core.DTOs;
using ProcurementSystem.Core.DTOs.Common;
using ProcurementSystem.Core.Interfaces;

namespace ProcurementSystem.API.Controllers
{
    [ApiController]
    [Route("api/admin/storage")]
    [Authorize(Roles = "Admin")]
    public class StorageAdminController : ControllerBase
    {
        private readonly IStorageMigrationService _migrationService;
        private readonly ProcurementSystem.Infrastructure.Services.SupabaseStorageService _supabaseService;

        public StorageAdminController(
            IStorageMigrationService migrationService,
            ProcurementSystem.Infrastructure.Services.SupabaseStorageService supabaseService)
        {
            _migrationService = migrationService;
            _supabaseService = supabaseService;
        }

        /// <summary>
        /// Kiểm tra kết nối S3 tới Supabase, danh sách Buckets hiện có và tự tạo Bucket nếu chưa có
        /// </summary>
        [HttpGet("test-connection")]
        public async Task<IActionResult> TestConnection()
        {
            var (success, message, buckets) = await _supabaseService.TestAndEnsureBucketAsync();
            return Ok(new
            {
                success,
                message,
                buckets
            });
        }

        /// <summary>
        /// Xem trạng thái cấu hình lưu trữ hiện tại (Local vs Supabase, dung lượng uploads trên ổ đĩa)
        /// </summary>
        [HttpGet("status")]
        [ProducesResponseType(typeof(ApiResponse<StorageStatusDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<StorageStatusDto>>> GetStatus()
        {
            var result = await _migrationService.GetStorageStatusAsync();
            return Ok(result);
        }

        /// <summary>
        /// 1-Click Sync: Tự động tạo bảng sao lưu CSDL, đẩy toàn bộ tệp uploads cục bộ lên Supabase và cập nhật CSDL
        /// </summary>
        [HttpPost("sync-to-cloud")]
        [ProducesResponseType(typeof(ApiResponse<StorageSyncReportDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<StorageSyncReportDto>>> SyncToCloud()
        {
            var adminId = GetCurrentUserId();
            var result = await _migrationService.SyncLocalFilesToCloudAsync(adminId);
            if (!result.Success)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }

        /// <summary>
        /// 1-Click Rollback: Khôi phục lại 100% đường dẫn tệp trong CSDL về trạng thái cục bộ từ bảng sao lưu
        /// </summary>
        [HttpPost("rollback")]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<string>>> Rollback([FromQuery] string? backupTable = null)
        {
            var adminId = GetCurrentUserId();
            var result = await _migrationService.RollbackFilePathsAsync(adminId, backupTable);
            if (!result.Success)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(claim, out var id) ? id : 0;
        }
    }
}
