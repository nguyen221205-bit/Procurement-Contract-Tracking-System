using System.Security.Claims;

namespace ProcurementSystem.Core.Interfaces
{
    public interface ITokenService
    {
        string GenerateAccessToken(int userId, string email, string fullName, IEnumerable<string> roles);
        string GenerateRefreshToken();
        ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
    }
}
