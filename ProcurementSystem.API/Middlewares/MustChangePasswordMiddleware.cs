using System.Text.Json;
using ProcurementSystem.Core.DTOs;

namespace ProcurementSystem.API.Middlewares
{
    public class MustChangePasswordMiddleware
    {
        private readonly RequestDelegate _next;

        public MustChangePasswordMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.User.Identity?.IsAuthenticated == true)
            {
                var mustChangeClaim = context.User.FindFirst("MustChangePassword")?.Value;
                if (string.Equals(mustChangeClaim, "true", StringComparison.OrdinalIgnoreCase))
                {
                    var path = context.Request.Path.Value?.ToLowerInvariant() ?? string.Empty;
                    // Bỏ qua các endpoint bắt buộc để đổi mật khẩu, refresh token và đăng xuất
                    if (!path.EndsWith("/auth/change-password") &&
                        !path.EndsWith("/auth/logout") &&
                        !path.EndsWith("/auth/refresh-token") &&
                        !path.EndsWith("/auth/me"))
                    {
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        context.Response.ContentType = "application/json";

                        var failResponse = ApiResponse<object>.Fail("Tài khoản bắt buộc phải đổi mật khẩu lần đầu trước khi thực hiện các thao tác khác.");
                        var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
                        await context.Response.WriteAsync(JsonSerializer.Serialize(failResponse, jsonOptions));
                        return;
                    }
                }
            }

            await _next(context);
        }
    }
}
