using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProcurementSystem.Core.DTOs;
using ProcurementSystem.Core.DTOs.Clarification;
using ProcurementSystem.Core.Enums;
using ProcurementSystem.Core.Interfaces;

namespace ProcurementSystem.API.Controllers
{
    /// <summary>
    /// Phân hệ Làm rõ Hồ sơ mời thầu (HSMT) và Làm rõ Hồ sơ dự thầu (HSDT)
    /// Hỗ trợ kênh Hỏi - Đáp và Yêu cầu giải trình bổ sung trực tiếp trên nền tảng web
    /// </summary>
    [ApiController]
    [Route("api")]
    [Produces("application/json")]
    [Authorize]
    public class ClarificationsController : ControllerBase
    {
        private readonly IClarificationService _clarificationService;

        public ClarificationsController(IClarificationService clarificationService)
        {
            _clarificationService = clarificationService;
        }

        /// <summary>
        /// Lấy danh sách yêu cầu làm rõ của gói thầu
        /// </summary>
        /// <param name="packageId">Định danh gói thầu</param>
        /// <param name="type">Loại làm rõ (0: HSMT - công khai, 1: HSDT - riêng tư)</param>
        [HttpGet("bid-packages/{packageId:int}/clarifications")]
        [ProducesResponseType(typeof(ApiResponse<List<BidClarificationDto>>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<List<BidClarificationDto>>>> GetClarifications(
            int packageId,
            [FromQuery] ClarificationType? type = null)
        {
            var userId = GetCurrentUserId();
            var isAdmin = User.IsInRole("Admin");
            var isProcurement = User.IsInRole("Procurement");
            var isEvaluator = User.IsInRole("Evaluator");
            var isContractor = User.IsInRole("Contractor");

            var result = await _clarificationService.GetClarificationsAsync(
                packageId,
                userId,
                isAdmin,
                isProcurement,
                isEvaluator,
                isContractor,
                type);

            return Ok(result);
        }

        /// <summary>
        /// Nhà thầu gửi câu hỏi thắc mắc / yêu cầu làm rõ Hồ sơ mời thầu (HSMT)
        /// </summary>
        [HttpPost("bid-packages/{packageId:int}/clarifications/package")]
        [Authorize(Roles = "Contractor")]
        [ProducesResponseType(typeof(ApiResponse<BidClarificationDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<BidClarificationDto>), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ApiResponse<BidClarificationDto>>> CreatePackageClarification(
            int packageId,
            [FromBody] CreatePackageClarificationRequest request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<BidClarificationDto>.Fail(errors));
            }

            var userId = GetCurrentUserId();
            var result = await _clarificationService.CreatePackageClarificationAsync(packageId, request, userId);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        /// <summary>
        /// Tổ chuyên gia hoặc Bên mời thầu gửi yêu cầu làm rõ / giải trình Hồ sơ dự thầu (HSDT)
        /// </summary>
        [HttpPost("bid-packages/{packageId:int}/clarifications/submission")]
        [Authorize(Roles = "Admin,Procurement,Evaluator")]
        [ProducesResponseType(typeof(ApiResponse<BidClarificationDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<BidClarificationDto>), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ApiResponse<BidClarificationDto>>> CreateSubmissionClarification(
            int packageId,
            [FromBody] CreateSubmissionClarificationRequest request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<BidClarificationDto>.Fail(errors));
            }

            var userId = GetCurrentUserId();
            var result = await _clarificationService.CreateSubmissionClarificationAsync(packageId, request, userId);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        /// <summary>
        /// Phản hồi làm rõ HSMT (Procurement/Admin) hoặc nộp văn bản giải trình HSDT (Nhà thầu)
        /// </summary>
        [HttpPost("clarifications/{id:int}/answer")]
        [Authorize(Roles = "Admin,Procurement,Contractor")]
        [ProducesResponseType(typeof(ApiResponse<BidClarificationDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<BidClarificationDto>), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ApiResponse<BidClarificationDto>>> AnswerClarification(
            int id,
            [FromBody] AnswerClarificationRequest request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<BidClarificationDto>.Fail(errors));
            }

            var userId = GetCurrentUserId();
            var isAdmin = User.IsInRole("Admin");
            var isProcurement = User.IsInRole("Procurement");
            var isContractor = User.IsInRole("Contractor");

            var result = await _clarificationService.AnswerClarificationAsync(
                id,
                request,
                userId,
                isAdmin,
                isProcurement,
                isContractor);

            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            int.TryParse(claim, out int userId);
            return userId;
        }
    }
}
