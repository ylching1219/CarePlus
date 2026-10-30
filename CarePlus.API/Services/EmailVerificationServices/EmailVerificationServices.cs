using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using CarePlus.API.Data;
using CarePlus.API.DTO.UserDTO;
using CarePlus.API.Extension;
using CarePlus.API.Models.User;
using CarePlus.API.Services.EmailServices.Interface;
using CarePlus.API.Services.EmailVerificationServices.Interface;
using Microsoft.EntityFrameworkCore;

namespace CarePlus.API.Services.EmailVerificationServices;

public class EmailVerificationServices : IEmailVerificationServices
{
    private const string Purpose = "email";
    private const int CodeLifetimeMinutes = 10;
    private const int MaxAttempts = 5;

    private readonly AppDbContext _db;
    private readonly IEmailSender _emailSender;
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailVerificationServices> _logger;

    public EmailVerificationServices(
        AppDbContext db,
        IEmailSender emailSender,
        IConfiguration configuration,
        ILogger<EmailVerificationServices> logger)
    {
        _db = db;
        _emailSender = emailSender;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<ApiResponse<object>> SendCodeAsync(long userId)
    {
        var user = await _db.Users.FirstOrDefaultAsync(row => row.UserId == userId && row.Status == "Active");
        if (user == null)
        {
            return ApiResponse<object>.Fail("User account was not found.", 404);
        }

        if (user.EmailVerified)
        {
            return ApiResponse<object>.Fail("This email address is already verified.", 400);
        }

        return await CreateAndSendCodeAsync(user);
    }

    public async Task<ApiResponse<object>> ResendCodeAsync(string challengeId)
    {
        var previousChallenge = await _db.TwoFactorChallenges
            .Include(row => row.User)
            .FirstOrDefaultAsync(row => row.ChallengeId == challengeId && row.Purpose == Purpose);

        if (previousChallenge?.User == null || previousChallenge.User.Status != "Active" || previousChallenge.User.EmailVerified)
        {
            return ApiResponse<object>.Fail("This email verification request is no longer valid.", 400);
        }

        return await CreateAndSendCodeAsync(previousChallenge.User);
    }

    public async Task<ApiResponse<object>> VerifyCodeAsync(EmailVerificationRequest request)
    {
        var challengeId = request.ChallengeId.Trim();
        var now = DateTimeHelper.MalaysiaNow();
        var challenge = await _db.TwoFactorChallenges
            .Include(row => row.User)
            .FirstOrDefaultAsync(row => row.ChallengeId == challengeId && row.Purpose == Purpose && row.ConsumedAt == null);

        if (challenge?.User == null || challenge.User.Status != "Active")
        {
            return ApiResponse<object>.Fail("That verification request is no longer valid.", 400);
        }

        if (challenge.ExpiresAt <= now)
        {
            return ApiResponse<object>.Fail("That verification code has expired. Request a new code.", 400);
        }

        if (challenge.Attempts >= MaxAttempts)
        {
            challenge.ConsumedAt = now;
            await _db.SaveChangesAsync();
            return ApiResponse<object>.Fail("Too many incorrect attempts. Request a new code.", 429);
        }

        challenge.Attempts++;
        var expected = Convert.FromHexString(challenge.CodeHash);
        var actual = Convert.FromHexString(HashCode(challenge.ChallengeId, request.Code.Trim()));
        if (!CryptographicOperations.FixedTimeEquals(expected, actual))
        {
            if (challenge.Attempts >= MaxAttempts)
            {
                challenge.ConsumedAt = now;
            }

            await _db.SaveChangesAsync();
            return ApiResponse<object>.Fail(
                challenge.Attempts >= MaxAttempts
                    ? "Too many incorrect attempts. Request a new code."
                    : "That verification code is incorrect.",
                challenge.Attempts >= MaxAttempts ? 429 : 400);
        }

        challenge.User.EmailVerified = true;
        challenge.ConsumedAt = now;
        await _db.SaveChangesAsync();

        return ApiResponse<object>.Ok(new EmailVerificationStatusResponse { Verified = true }, "Email verified.");
    }

    private async Task<ApiResponse<object>> CreateAndSendCodeAsync(User user)
    {
        var now = DateTimeHelper.MalaysiaNow();
        var activeChallenges = await _db.TwoFactorChallenges
            .Where(row => row.UserId == user.UserId && row.Purpose == Purpose && row.ConsumedAt == null && row.CreatedAt > now.AddMinutes(-1))
            .ToListAsync();

        if (activeChallenges.Count > 0)
        {
            return ApiResponse<object>.Fail("A verification code was sent recently. Please wait before requesting another code.", 429);
        }

        var previousChallenges = await _db.TwoFactorChallenges
            .Where(row => row.UserId == user.UserId && row.Purpose == Purpose && row.ConsumedAt == null)
            .ToListAsync();

        var challengeId = Guid.NewGuid().ToString("N");
        var code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6", CultureInfo.InvariantCulture);
        var challenge = new TwoFactorChallenge
        {
            ChallengeId = challengeId,
            UserId = user.UserId,
            Purpose = Purpose,
            CodeHash = HashCode(challengeId, code),
            CreatedAt = now,
            ExpiresAt = now.AddMinutes(CodeLifetimeMinutes),
            Attempts = 0,
        };

        try
        {
            var clientBaseUrl = (_configuration["Client:BaseUrl"] ?? "http://localhost:5173").TrimEnd('/');
            var verificationUrl = $"{clientBaseUrl}/register?challengeId={Uri.EscapeDataString(challengeId)}&code={Uri.EscapeDataString(code)}&email={Uri.EscapeDataString(MaskEmail(user.Email))}";
            var safeName = WebUtility.HtmlEncode(user.Name);
            var safeVerificationUrl = WebUtility.HtmlEncode(verificationUrl);
            var emailBody = $"""
                <html>
                  <body style="font-family:Arial,sans-serif;color:#172b35;line-height:1.5">
                    <h2>Verify your CarePlus email address</h2>
                    <p>Hi {safeName},</p>
                    <p>Click the button below to verify your email and finish creating your CarePlus account.</p>
                    <p><a href="{safeVerificationUrl}" style="display:inline-block;background:#0b2638;color:#ffffff;padding:12px 20px;border-radius:6px;text-decoration:none;font-weight:600">Verify email</a></p>
                    <p>If the button does not work, enter this 6-digit code on the CarePlus registration page:</p>
                    <p style="font-size:32px;letter-spacing:8px;font-weight:700;margin:8px 0 20px 0"><strong>{code}</strong></p>
                    <p>This link and code expire in {CodeLifetimeMinutes} minutes. If you did not create this account, you can ignore this email.</p>
                  </body>
                </html>
                """;

            await _emailSender.SendAsync(
                user.Email,
                "Verify your CarePlus email address",
                emailBody,
                isBodyHtml: true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unable to send an email verification code for user {UserId}.", user.UserId);
            return ApiResponse<object>.Fail("We could not send the verification email. Please try again later.", 503);
        }

        foreach (var previousChallenge in previousChallenges)
        {
            previousChallenge.ConsumedAt = now;
        }

        await _db.TwoFactorChallenges.AddAsync(challenge);
        await _db.SaveChangesAsync();

        return ApiResponse<object>.Ok(new EmailVerificationCodeSentResponse
        {
            ChallengeId = challengeId,
            MaskedEmail = MaskEmail(user.Email),
            ExpiresInSeconds = CodeLifetimeMinutes * 60,
        }, "Verification email sent.");
    }

    private string HashCode(string challengeId, string code)
    {
        var secret = _configuration["JwtSettings:SecretKey"] ?? throw new InvalidOperationException("JWT secret key is missing.");
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{challengeId}:{code}:{secret}")));
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
