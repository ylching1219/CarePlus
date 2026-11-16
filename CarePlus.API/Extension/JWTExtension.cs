using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace CarePlus.API.Extension;

public static class JWTExtension
{
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services,IConfiguration configuration)
    {
        var jwtSettings =
            configuration.GetSection("JwtSettings");

        var secretKey = jwtSettings["SecretKey"];
        if (string.IsNullOrWhiteSpace(secretKey))
        {
            throw new InvalidOperationException(
                "JWT secret key is missing. Set JwtSettings__SecretKey in the environment or user-secrets (do not commit it).");
        }

        services.AddAuthentication(
            JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters =
                    new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey =
                            new SymmetricSecurityKey(
                                Encoding.UTF8.GetBytes(secretKey)),

                        ValidateIssuer = true,
                        ValidIssuer = jwtSettings["Issuer"],

                        ValidateAudience = true,
                        ValidAudience = jwtSettings["Audience"],

                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.Zero,
                        LifetimeValidator = (notBefore, expires, securityToken, _) =>
                        {
                            if (securityToken is JwtSecurityToken jwt
                                && jwt.Claims.Any(claim => claim.Type == "remember" && claim.Value == "1"))
                            {
                                return true;
                            }

                            var now = DateTime.UtcNow;
                            if (notBefore is DateTime start && now < start) return false;
                            return expires is DateTime end && now < end;
                        },
                        RoleClaimType = ClaimTypes.Role,
                        NameClaimType = ClaimTypes.Name
                    };
            });

        // Closed by default; mark login/register/public reads with [AllowAnonymous].
        services.AddAuthorization(options =>
        {
            options.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();
        });

        return services;
    }
}
