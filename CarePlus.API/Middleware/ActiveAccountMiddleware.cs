using CarePlus.API.Data;
using CarePlus.API.DTO;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CarePlus.API.Middleware
{
    public class ActiveAccountMiddleware
    {
        private readonly RequestDelegate _next;

        public ActiveAccountMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, AppDbContext db)
        {
            if (context.User.Identity?.IsAuthenticated == true &&
                long.TryParse(context.User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            {
                var status = await db.Users
                    .AsNoTracking()
                    .Where(user => user.UserId == userId)
                    .Select(user => user.Status)
                    .FirstOrDefaultAsync();

                if (!string.Equals(status, "Active", StringComparison.OrdinalIgnoreCase))
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await context.Response.WriteAsJsonAsync(ApiResponse<object>.Fail("This account is not active.", 401));
                    return;
                }
            }

            await _next(context);
        }
    }
}
