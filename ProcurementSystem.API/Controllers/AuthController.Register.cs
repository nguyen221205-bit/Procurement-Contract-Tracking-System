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

        /// <summary>
        /// Đăng ký tài khoản Bên mời thầu / Chủ đầu tư mới kèm Quyết định thành lập / Giấy phép
        /// </summary>
        /// <param name="request">Thông tin đăng ký (Tên cơ quan, Loại hình, Mã số thuế, Quyết định thành lập, v.v.)</param>
        /// <param name="procuringEntityAuthService">Dịch vụ xác thực bên mời thầu</param>
        /// <response code="201">Đăng ký tài khoản bên mời thầu thành công, hồ sơ chờ phê duyệt.</response>
        /// <response code="400">Dữ liệu đăng ký không hợp lệ, email hoặc mã số thuế đã tồn tại.</response>
        [HttpPost("register-procuring-entity")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(ApiResponse<ProcuringEntityRegisterResponse>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<ProcuringEntityRegisterResponse>), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ApiResponse<ProcuringEntityRegisterResponse>>> RegisterProcuringEntity(
            [FromForm] RegisterProcuringEntityRequest request,
            [FromServices] IProcuringEntityAuthService procuringEntityAuthService)
        {
            var result = await procuringEntityAuthService.RegisterProcuringEntityAsync(request);
            if (!result.Success) return BadRequest(result);
            return StatusCode(StatusCodes.Status201Created, result);
        }

        /// <summary>
        /// Tra cứu thông tin doanh nghiệp qua Mã số thuế từ CSDL Quốc gia (VietQR API) phục vụ Autofill
        /// </summary>
        /// <param name="taxCode">Mã số thuế doanh nghiệp (10 hoặc 13 chữ số)</param>
        /// <param name="taxLookupService">Dịch vụ tra cứu mã số thuế</param>
        /// <response code="200">Tra cứu thành công, trả về thông tin tên công ty, địa chỉ.</response>
        /// <response code="404">Không tìm thấy mã số thuế trên hệ thống quốc gia.</response>
        [HttpGet("tax-lookup/{taxCode}")]
        [ProducesResponseType(typeof(ApiResponse<TaxBusinessData>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<TaxBusinessData>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiResponse<TaxBusinessData>>> LookupTaxCode(
            string taxCode,
            [FromServices] ITaxLookupService taxLookupService)
        {
            if (string.IsNullOrWhiteSpace(taxCode))
            {
                return BadRequest(ApiResponse<TaxBusinessData>.Fail("Vui lòng cung cấp mã số thuế hợp lệ."));
            }

            var result = await taxLookupService.VerifyTaxCodeAsync(taxCode);
            if (result == null)
            {
                return NotFound(ApiResponse<TaxBusinessData>.Fail("Không tìm thấy thông tin doanh nghiệp với mã số thuế này trên Cổng Quốc gia."));
            }

            return Ok(ApiResponse<TaxBusinessData>.Ok(result, "Tra cứu thông tin doanh nghiệp thành công."));
        }
    }
}
