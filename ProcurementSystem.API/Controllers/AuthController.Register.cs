using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProcurementSystem.Core.DTOs;
using ProcurementSystem.Core.DTOs.Auth;
using ProcurementSystem.Core.Interfaces;

namespace ProcurementSystem.API.Controllers
{
    public partial class AuthController
    {
        [HttpPost("register-contractor")]
        [Consumes("multipart/form-data")]
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
