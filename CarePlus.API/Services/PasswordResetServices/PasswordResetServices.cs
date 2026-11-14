using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using CarePlus.API.Data;
using CarePlus.API.DTO.UserDTO;
using CarePlus.API.Extension;
using CarePlus.API.Models.User;
using CarePlus.API.Services.EmailServices;
using CarePlus.API.Services.EmailServices.Interface;
using CarePlus.API.Services.PasswordResetServices.Interface;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CarePlus.API.Services.PasswordResetServices;

public sealed class PasswordResetServices : IPasswordResetServices
{
    private const string CodePurpose = "pwd-reset";
    private const string TokenPurpose = "pwd-token";
    private const int LifetimeMinutes = 10;
    private const int MaxAttempts = 5;

    private readonly AppDbContext _db;
    private readonly IEmailSender _emailSender;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PasswordResetServices> _logger;
    private readonly PasswordHasher<User> _passwordHasher = new();

    public PasswordResetServices(
        AppDbContext db,
        IEmailSender emailSender,
        IConfiguration configuration,
        ILogger<PasswordResetServices> logger)
    {
        _db = db;
        _emailSender = emailSender;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<ApiResponse<object>> RequestCodeAsync(
        PasswordResetRequestCodeRequest request,
        CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _db.Patients
            .AsNoTracking()
            .Where(patient => patient.User.Status == "Active"
                && patient.User.EmailVerified
                && patient.User.Email.ToLower() == email)
            .Select(patient => patient.User)
            .FirstOrDefaultAsync(cancellationToken);

        // Return the same response shape for unknown addresses so this endpoint
        // cannot be used to discover which people have CarePlus accounts.
        if (user == null)
        {
            return ApiResponse<object>.Ok(new PasswordResetCodeSentResponse
            {
                ChallengeId = Guid.NewGuid().ToString("N"),
                MaskedEmail = MaskEmail(email),
                ExpiresInSeconds = LifetimeMinutes * 60,
            }, "If an active CarePlus account uses this email, a verification code has been sent.");
        }

        var now = DateTimeHelper.MalaysiaNow();
        var recent = await _db.TwoFactorChallenges
            .AsNoTracking()
            .Where(challenge => challenge.UserId == user.UserId
                && challenge.Purpose == CodePurpose
                && challenge.ConsumedAt == null
                && challenge.ExpiresAt > now
                && challenge.CreatedAt > now.AddMinutes(-1))
            .OrderByDescending(challenge => challenge.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (recent != null)
        {
            return ApiResponse<object>.Ok(new PasswordResetCodeSentResponse
            {
                ChallengeId = recent.ChallengeId,
                MaskedEmail = MaskEmail(user.Email),
                ExpiresInSeconds = Math.Max(1, (int)Math.Ceiling((recent.ExpiresAt - now).TotalSeconds)),
            }, "A password reset code is already active.");
        }

        var challengeId = Guid.NewGuid().ToString("N");
        var code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6", CultureInfo.InvariantCulture);
        var challenge = new TwoFactorChallenge
        {
            ChallengeId = challengeId,
            UserId = user.UserId,
            Purpose = CodePurpose,
            CodeHash = HashSecret(challengeId, code, CodePurpose),
            CreatedAt = now,
            ExpiresAt = now.AddMinutes(LifetimeMinutes),
            Attempts = 0,
        };

        try
        {
            var emailBody = CarePlusEmailHtml.Otp(
                "Password reset",
                $"Hi {user.Name}, enter this verification code on the CarePlus password reset page.",
                code,
                LifetimeMinutes,
                "If you did not request a password reset, you can ignore this email.");

            await _emailSender.SendAsync(
                user.Email,
                "Reset your CarePlus password",
                emailBody,
                isBodyHtml: true,
                cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unable to send a password reset code for user {UserId}.", user.UserId);
            return ApiResponse<object>.Fail("We could not send the password reset email. Please try again later.", 503);
        }

        var previousChallenges = await _db.TwoFactorChallenges
            .Where(row => row.UserId == user.UserId
                && (row.Purpose == CodePurpose || row.Purpose == TokenPurpose)
                && row.ConsumedAt == null)
            .ToListAsync(cancellationToken);
        foreach (var previousChallenge in previousChallenges)
        {
            previousChallenge.ConsumedAt = now;
        }

        await _db.TwoFactorChallenges.AddAsync(challenge, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        return ApiResponse<object>.Ok(new PasswordResetCodeSentResponse
        {
            ChallengeId = challengeId,
            MaskedEmail = MaskEmail(user.Email),
            ExpiresInSeconds = LifetimeMinutes * 60,
        }, "Password reset code sent.");
    }

    public async Task<ApiResponse<object>> VerifyCodeAsync(
        PasswordResetVerifyCodeRequest request,
        CancellationToken cancellationToken)
    {
        var challengeId = request.ChallengeId.Trim();
        var now = DateTimeHelper.MalaysiaNow();
        var challenge = await _db.TwoFactorChallenges
            .FirstOrDefaultAsync(row => row.ChallengeId == challengeId
                && row.Purpose == CodePurpose
                && row.ConsumedAt == null, cancellationToken);

        if (challenge == null || challenge.ExpiresAt <= now)
        {
            return ApiResponse<object>.Fail("That verification code has expired or is invalid. Request a new code.", 400);
        }

        if (challenge.Attempts >= MaxAttempts)
        {
            challenge.ConsumedAt = now;
            await _db.SaveChangesAsync(cancellationToken);
            return ApiResponse<object>.Fail("Too many incorrect attempts. Request a new code.", 429);
        }

        challenge.Attempts++;
        if (!SecretMatches(challenge.CodeHash, HashSecret(challengeId, request.Code.Trim(), CodePurpose)))
        {
            if (challenge.Attempts >= MaxAttempts)
            {
                challenge.ConsumedAt = now;
            }

            await _db.SaveChangesAsync(cancellationToken);
            return ApiResponse<object>.Fail(
                challenge.Attempts >= MaxAttempts
                    ? "Too many incorrect attempts. Request a new code."
                    : "That verification code is incorrect.",
                challenge.Attempts >= MaxAttempts ? 429 : 400);
        }

        var resetToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        challenge.Purpose = TokenPurpose;
        challenge.CodeHash = HashSecret(challengeId, resetToken, TokenPurpose);
        challenge.CreatedAt = now;
        challenge.ExpiresAt = now.AddMinutes(LifetimeMinutes);
        challenge.Attempts = 0;
        await _db.SaveChangesAsync(cancellationToken);

        return ApiResponse<object>.Ok(new PasswordResetCodeVerifiedResponse
        {
            ChallengeId = challengeId,
            ResetToken = resetToken,
            ExpiresInSeconds = LifetimeMinutes * 60,
        }, "Verification code accepted.");
    }

    public async Task<ApiResponse<object>> ResetPasswordAsync(
        PasswordResetCompleteRequest request,
        CancellationToken cancellationToken)
    {
        var passwordError = PasswordRuleError(request.NewPassword);
        if (passwordError != null)
        {
            return ApiResponse<object>.Fail(passwordError, 400);
        }

        var challengeId = request.ChallengeId.Trim();
        var now = DateTimeHelper.MalaysiaNow();
        var challenge = await _db.TwoFactorChallenges
            .Include(row => row.User)
            .FirstOrDefaultAsync(row => row.ChallengeId == challengeId
                && row.Purpose == TokenPurpose
                && row.ConsumedAt == null, cancellationToken);

        if (challenge?.User == null
            || challenge.User.Status != "Active"
            || challenge.ExpiresAt <= now
            || !SecretMatches(challenge.CodeHash, HashSecret(challengeId, request.ResetToken.Trim(), TokenPurpose)))
        {
            return ApiResponse<object>.Fail("That password reset request has expired or is invalid. Request a new code.", 400);
        }

        challenge.User.PasswordHash = _passwordHasher.HashPassword(challenge.User, request.NewPassword);
        challenge.User.FailedPasswordAttempts = 0;
        challenge.User.PasswordLockoutUntilUtc = null;
        challenge.ConsumedAt = now;

        var otherChallenges = await _db.TwoFactorChallenges
            .Where(row => row.UserId == challenge.UserId
                && row.ChallengeId != challenge.ChallengeId
                && (row.Purpose == CodePurpose || row.Purpose == TokenPurpose)
                && row.ConsumedAt == null)
            .ToListAsync(cancellationToken);
        foreach (var otherChallenge in otherChallenges)
        {
            otherChallenge.ConsumedAt = now;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return ApiResponse<object>.Ok(new PasswordResetCompletedResponse { Reset = true }, "Password reset successfully.");
    }

    private string HashSecret(string challengeId, string value, string purpose)
    {
        var secret = _configuration["JwtSettings:SecretKey"]
            ?? throw new InvalidOperationException("JWT secret key is missing.");
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{purpose}:{challengeId}:{value}:{secret}")));
    }

    private static bool SecretMatches(string expectedHash, string actualHash)
    {
        try
        {
            return CryptographicOperations.FixedTimeEquals(
                Convert.FromHexString(expectedHash),
                Convert.FromHexString(actualHash));
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static string? PasswordRuleError(string password)
    {
        if (string.IsNullOrEmpty(password) || password.Length < 8 || password.Length > 72)
        {
            return "Use 8 to 72 characters.";
        }

        if (!password.Any(char.IsLower)) return "Add a lowercase letter.";
        if (!password.Any(char.IsUpper)) return "Add a capital letter.";
        if (!password.Any(char.IsDigit)) return "Add a number.";
        if (!password.Any(character => !char.IsLetterOrDigit(character))) return "Add a symbol.";
        return null;
    }

    private static string MaskEmail(string email)
    {
        var parts = email.Split('@', 2);
        if (parts.Length != 2) return "your email address";
        var local = parts[0];
        var visible = local.Length <= 2 ? local[..1] : local[..2];
        return $"{visible}{new string('•', Math.Max(1, local.Length - visible.Length))}@{parts[1]}";
    }
}
