using CarePlus.API.Services.JWTServices.Interface;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace CarePlus.API.Services.JwtServices;

public class JWTServices : IJWTServices
{
    private readonly IConfiguration _configuration;

    public JWTServices(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GenerateToken(
        long userId,
        string username,
        string accountType,
        string role,
        string? patientId = null,
        string? staffId = null,
        bool rememberMe = false)
    {
        var settings = _configuration.GetSection("JwtSettings");
        var secretKey = settings["SecretKey"]?? throw new InvalidOperationException("JWT secret key is missing.");

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Name, username),
            new(ClaimTypes.Role, role),
            new("accountType", accountType)
        };

        if (patientId != null)
            claims.Add(new Claim("patientId", patientId));

        if (staffId != null)
            claims.Add(new Claim("staffId", staffId));

        if (rememberMe)
            claims.Add(new Claim("remember", "1"));

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(secretKey));

        var credentials = new SigningCredentials(
            key,
            SecurityAlgorithms.HmacSha256);

        // ponytail: Remember Me has no session timer. Logout clears the token.
        // The exp claim is only there because JWT validation expects one; remember tokens skip it.
        var expiryMinutes = rememberMe
            ? 60 * 24 * 365 * 10
            : settings.GetValue("AccessTokenExpiryMinutes", 60);

        var token = new JwtSecurityToken(
            issuer: settings["Issuer"],
            audience: settings["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}