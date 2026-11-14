using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using CarePlus.API.Data;
using CarePlus.API.DTO.UserDTO;
using CarePlus.API.Extension;
using CarePlus.API.Models.User;
using CarePlus.API.Services.EmailServices;
using CarePlus.API.Services.EmailServices.Interface;
using CarePlus.API.Services.RegistrationEmailVerificationServices.Interface;
using Microsoft.EntityFrameworkCore;

namespace CarePlus.API.Services.RegistrationEmailVerificationServices;

public sealed class RegistrationEmailVerificationServices : IRegistrationEmailVerificationServices
{
    private const int CodeLifetimeMinutes = 10;
    private const int TokenLifetimeMinutes = 15;
    private const int MaxAttempts = 5;

    private readonly AppDbContext _db;
    private readonly IEmailSender _emailSender;
    private readonly IConfiguration _configuration;
    private readonly ILogger<RegistrationEmailVerificationServices> _logger;

    public RegistrationEmailVerificationServices(
        AppDbContext db,
        IEmailSender emailSender,
        IConfiguration configuration,
        ILogger<RegistrationEmailVerificationServices> logger)
    {
        _db = db;
        _emailSender = emailSender;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<ApiResponse<object>> RequestCodeAsync(
        RegistrationEmailCodeRequest request,
        CancellationToken cancellationToken)
    {
        var email = NormalizeEmail(request.Email);
        if (await _db.Users.AsNoTracking().AnyAsync(user => user.Email.ToLower() == email, cancellationToken))
        {
            return ApiResponse<object>.Fail("Email already exists", 400);
        }

        var now = DateTimeHelper.MalaysiaNow();
        var recent = await _db.RegistrationEmailChallenges
            .AsNoTracking()
            .Where(challenge => challenge.Email == email
                && challenge.VerifiedAt == null
                && challenge.ConsumedAt == null
                && challenge.ExpiresAt > now
                && challenge.CreatedAt > now.AddMinutes(-1))
            .OrderByDescending(challenge => challenge.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (recent != null)
        {
            return ApiResponse<object>.Ok(new RegistrationEmailCodeSentResponse
            {
                ChallengeId = recent.ChallengeId,
                MaskedEmail = MaskEmail(email),
                ExpiresInSeconds = Math.Max(1, (int)Math.Ceiling((recent.ExpiresAt - now).TotalSeconds)),
            }, "A verification code is already active.");
        }

        var challengeId = Guid.NewGuid().ToString("N");
        var code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6", CultureInfo.InvariantCulture);
        var challenge = new RegistrationEmailChallenge
        {
            ChallengeId = challengeId,
            Email = email,
            SecretHash = HashSecret(challengeId, code, "code"),
            CreatedAt = now,
            ExpiresAt = now.AddMinutes(CodeLifetimeMinutes),
            Attempts = 0,
        };

        try
        {
            var emailBody = CarePlusEmailHtml.Otp(
                "Email verification",
                "Enter this verification code in the CarePlus registration form.",
                code,
                CodeLifetimeMinutes,
                "If you did not request this code, you can ignore this email.");
            await _emailSender.SendAsync(
                email,
                "Verify your CarePlus email address",
                emailBody,
                isBodyHtml: true,
                cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unable to send a registration email code to {Email}.", email);
            return ApiResponse<object>.Fail("We could not send the verification email. Please try again later.", 503);
        }

        var previous = await _db.RegistrationEmailChallenges
            .Where(row => row.Email == email && row.ConsumedAt == null)
            .ToListAsync(cancellationToken);
        foreach (var item in previous)
        {
            item.ConsumedAt = now;
        }

        await _db.RegistrationEmailChallenges.AddAsync(challenge, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        return ApiResponse<object>.Ok(new RegistrationEmailCodeSentResponse
        {
            ChallengeId = challengeId,
            MaskedEmail = MaskEmail(email),
            ExpiresInSeconds = CodeLifetimeMinutes * 60,
        }, "Verification code sent.");
    }

    public async Task<ApiResponse<object>> VerifyCodeAsync(
        RegistrationEmailCodeVerifyRequest request,
        CancellationToken cancellationToken)
    {
        var challengeId = request.ChallengeId.Trim();
        var now = DateTimeHelper.MalaysiaNow();
        var challenge = await _db.RegistrationEmailChallenges
            .FirstOrDefaultAsync(row => row.ChallengeId == challengeId
                && row.VerifiedAt == null
                && row.ConsumedAt == null, cancellationToken);
        if (challenge == null || challenge.ExpiresAt <= now)
        {
            return ApiResponse<object>.Fail("That verification code has expired. Request a new code.", 400);
        }

        if (challenge.Attempts >= MaxAttempts)
        {
            challenge.ConsumedAt = now;
            await _db.SaveChangesAsync(cancellationToken);
            return ApiResponse<object>.Fail("Too many incorrect attempts. Request a new code.", 429);
        }

        challenge.Attempts++;
        if (!SecretMatches(challenge.SecretHash, HashSecret(challengeId, request.Code.Trim(), "code")))
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

        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        challenge.SecretHash = HashSecret(challengeId, token, "token");
        challenge.VerifiedAt = now;
        challenge.ExpiresAt = now.AddMinutes(TokenLifetimeMinutes);
        challenge.Attempts = 0;
        await _db.SaveChangesAsync(cancellationToken);
        return ApiResponse<object>.Ok(new RegistrationEmailCodeVerifiedResponse
        {
            ChallengeId = challengeId,
            VerificationToken = token,
            ExpiresInSeconds = TokenLifetimeMinutes * 60,
        }, "Email verified.");
    }

    public async Task<bool> IsVerifiedAsync(
        string email,
        string challengeId,
        string verificationToken,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeHelper.MalaysiaNow();
        var challenge = await _db.RegistrationEmailChallenges
            .AsNoTracking()
            .FirstOrDefaultAsync(row => row.ChallengeId == challengeId.Trim()
                && row.Email == NormalizeEmail(email)
                && row.VerifiedAt != null
                && row.ConsumedAt == null
                && row.ExpiresAt > now, cancellationToken);
        return challenge != null
            && SecretMatches(challenge.SecretHash, HashSecret(challenge.ChallengeId, verificationToken.Trim(), "token"));
    }

    public async Task ConsumeAsync(string challengeId, CancellationToken cancellationToken = default)
    {
        var challenge = await _db.RegistrationEmailChallenges
            .FirstOrDefaultAsync(row => row.ChallengeId == challengeId.Trim() && row.ConsumedAt == null, cancellationToken);
        if (challenge != null)
        {
            challenge.ConsumedAt = DateTimeHelper.MalaysiaNow();
        }
    }

    private string HashSecret(string challengeId, string value, string purpose)
    {
        var secret = _configuration["JwtSettings:SecretKey"]
            ?? throw new InvalidOperationException("JWT secret key is missing.");
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"register:{purpose}:{challengeId}:{value}:{secret}")));
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

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    private static string MaskEmail(string email)
    {
        var parts = email.Split('@', 2);
        if (parts.Length != 2) return "your email address";
        var local = parts[0];
        var visible = local.Length <= 2 ? local[..1] : local[..2];
        return $"{visible}{new string('•', Math.Max(1, local.Length - visible.Length))}@{parts[1]}";
    }
}
