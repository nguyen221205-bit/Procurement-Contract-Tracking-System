using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProcurementSystem.Core.DTOs;
using ProcurementSystem.Core.DTOs.Backup;
using ProcurementSystem.Core.Interfaces;

namespace ProcurementSystem.API.Controllers
{
    [ApiController]
    [Route("api/admin/backups")]
    [Authorize(Roles = "Admin")]
    [Produces("application/json")]
    public class BackupsController : ControllerBase
    {
        private readonly IBackupService _backupService;

        public BackupsController(IBackupService backupService)
        {
            _backupService = backupService;
        }

        /// <summary>
        /// Xem tổng quan và danh sách tất cả các bản sao lưu cơ sở dữ liệu và tệp đính kèm (Admin only)
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<BackupOverviewDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<BackupOverviewDto>>> GetBackupsOverview()
        {
            var result = await _backupService.GetBackupOverviewAsync();
            return Ok(result);
        }

        /// <summary>
        /// Tạo bản sao lưu toàn vẹn cho cơ sở dữ liệu SQL Server (Admin only)
        /// </summary>
        [HttpPost("database")]
        [ProducesResponseType(typeof(ApiResponse<BackupItemDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<BackupItemDto>), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ApiResponse<BackupItemDto>>> CreateDatabaseBackup([FromBody] CreateBackupRequest? request)
        {
            var adminId = GetCurrentUserId();
            var result = await _backupService.CreateDatabaseBackupAsync(adminId, request?.Description);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Tạo bản sao lưu nén toàn bộ thư mục tệp đính kèm uploads thành tệp ZIP (Admin only)
        /// </summary>
        [HttpPost("documents")]
        [ProducesResponseType(typeof(ApiResponse<BackupItemDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<BackupItemDto>), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ApiResponse<BackupItemDto>>> CreateDocumentsZipBackup([FromBody] CreateBackupRequest? request)
        {
            var adminId = GetCurrentUserId();
            var result = await _backupService.CreateDocumentsZipBackupAsync(adminId, request?.Description);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Tải về tệp sao lưu (.bak hoặc .zip) an toàn (Admin only)
        /// </summary>
        [HttpGet("{fileName}/download")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DownloadBackup(string fileName)
        {
            var fileData = await _backupService.GetBackupFileForDownloadAsync(fileName);
            if (fileData == null)
            {
                return NotFound(ApiResponse<string>.Fail("Không tìm thấy tệp sao lưu yêu cầu."));
            }

            return File(fileData.Value.FileBytes, fileData.Value.ContentType, fileData.Value.FileName);
        }

        /// <summary>
        /// Phục hồi cơ sở dữ liệu SQL Server từ bản sao lưu đã chọn (Admin only - yêu cầu cụm từ xác nhận)
        /// </summary>
        [HttpPost("database/restore")]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ApiResponse<string>>> RestoreDatabaseBackup([FromBody] RestoreBackupRequest request)
        {
            var adminId = GetCurrentUserId();
            var result = await _backupService.RestoreDatabaseBackupAsync(adminId, request);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Xóa một tệp sao lưu khỏi máy chủ (Admin only)
        /// </summary>
        [HttpDelete("{fileName}")]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiResponse<string>>> DeleteBackup(string fileName)
        {
            var adminId = GetCurrentUserId();
            var result = await _backupService.DeleteBackupAsync(adminId, fileName);
            return StatusCode(result.StatusCode, result);
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out int userId) ? userId : 0;
        }
    }
}
