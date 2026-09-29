using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProcurementSystem.Core.DTOs;
using ProcurementSystem.Core.DTOs.Auth;
using ProcurementSystem.Core.Interfaces;

namespace ProcurementSystem.API.Controllers
{
    public partial class AuthController
    {
        /// <summary>
        /// Đăng ký tài khoản Nhà thầu mới kèm hồ sơ năng lực và giấy phép kinh doanh
        /// </summary>
        /// <param name="request">Thông tin đăng ký (Tên công ty, Mã số thuế, Email, Mật khẩu, Số điện thoại, Địa chỉ, File giấy phép)</param>
        /// <param name="contractorAuthService">Dịch vụ xác thực nhà thầu</param>
        /// <response code="201">Đăng ký tài khoản nhà thầu thành công.</response>
        /// <response code="400">Dữ liệu đăng ký không hợp lệ, email hoặc mã số thuế đã tồn tại.</response>
        [HttpPost("register-contractor")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(ApiResponse<ContractorRegisterResponse>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<ContractorRegisterResponse>), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ApiResponse<ContractorRegisterResponse>>> RegisterContractor(
            [FromForm] RegisterContractorRequest request,
            [FromServices] IContractorAuthService contractorAuthService)
        {
            var result = await contractorAuthService.RegisterContractorAsync(request);
            if (!result.Success) return BadRequest(result);
            return StatusCode(StatusCodes.Status201Created, result);
        }
    }
}
