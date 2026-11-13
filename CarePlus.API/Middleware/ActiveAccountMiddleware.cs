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
                var account = await db.Users
                    .AsNoTracking()
                    .Where(user => user.UserId == userId)
                    .Select(user => new { user.Status, user.PasswordLockoutUntilUtc })
                    .FirstOrDefaultAsync();

                if (!string.Equals(account?.Status, "Active", StringComparison.OrdinalIgnoreCase))
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await context.Response.WriteAsJsonAsync(ApiResponse<object>.Fail("This account is not active.", 401));
                    return;
                }

                if (account?.PasswordLockoutUntilUtc is DateTime lockoutUntil && lockoutUntil > DateTime.UtcNow)
                {
                    var remaining = lockoutUntil - DateTime.UtcNow;
                    var minutes = Math.Max(1, (int)Math.Ceiling(remaining.TotalMinutes));
                    context.Response.StatusCode = StatusCodes.Status423Locked;
                    context.Response.Headers.RetryAfter = Math.Max(1, (int)Math.Ceiling(remaining.TotalSeconds)).ToString();
                    await context.Response.WriteAsJsonAsync(ApiResponse<object>.Fail(
                        $"Account temporarily locked. Try again in {minutes} minute(s).", 423));
                    return;
                }
            }

            await _next(context);
        }
    }
}
