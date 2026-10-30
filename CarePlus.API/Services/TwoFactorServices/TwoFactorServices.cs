using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using CarePlus.API.Data;
using CarePlus.API.DTO.UserDTO;
using CarePlus.API.Extension;
using CarePlus.API.Models.User;
using CarePlus.API.Services.EmailServices.Interface;
using CarePlus.API.Services.TwoFactorServices.Interface;
using Microsoft.EntityFrameworkCore;

namespace CarePlus.API.Services.TwoFactorServices;

public class TwoFactorServices : ITwoFactorServices
{
    private const int CodeLifetimeMinutes = 10;
    private const int MaxAttempts = 5;
    private readonly AppDbContext _db;
    private readonly IEmailSender _emailSender;
    private readonly IConfiguration _configuration;
    private readonly ILogger<TwoFactorServices> _logger;

    public TwoFactorServices(AppDbContext db, IEmailSender emailSender, IConfiguration configuration, ILogger<TwoFactorServices> logger)
    {
        _db = db;
        _emailSender = emailSender;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<ApiResponse<object>> SendCodeAsync(long userId, TwoFactorSendCodeRequest request)
    {
        var purpose = NormalizePurpose(request.Action);
        if (purpose == null)
        {
            return ApiResponse<object>.Fail("The two-factor action is invalid.", 400);
        }

        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(row => row.UserId == userId && row.Status == "Active");
        if (user == null)
        {
            return ApiResponse<object>.Fail("User account was not found.", 404);
        }

        if (string.IsNullOrWhiteSpace(user.Email))
        {
            return ApiResponse<object>.Fail("Add an email address before enabling two-factor authentication.", 400);
        }

        var setting = await _db.AppSettings.FirstOrDefaultAsync(row => row.UserId == userId);
        if (purpose == "disable" && setting?.TwoFactorEnabled != true)
        {
            return ApiResponse<object>.Fail("Two-factor authentication is already disabled.", 400);
        }

        var now = DateTimeHelper.MalaysiaNow();
        var recent = await _db.TwoFactorChallenges
            .Where(row => row.UserId == userId
                && row.Purpose == purpose
                && row.ConsumedAt == null
                && row.ExpiresAt > now
                && row.CreatedAt > now.AddMinutes(-1))
            .OrderByDescending(row => row.CreatedAt)
            .FirstOrDefaultAsync();
        if (recent != null)
        {
            // The user may have closed the verification dialog or browser tab. Re-open
            // the existing challenge instead of returning a rate-limit error. The code
            // is still bound to the original challenge and keeps the original expiry.
            return ApiResponse<object>.Ok(new TwoFactorCodeSentResponse
            {
                MaskedEmail = MaskEmail(user.Email),
                ExpiresInSeconds = Math.Max(1, (int)Math.Ceiling((recent.ExpiresAt - now).TotalSeconds)),
            }, "A verification code is already active.");
        }

        var challengeId = Guid.NewGuid().ToString("N");
        var code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6", CultureInfo.InvariantCulture);
        var challenge = new TwoFactorChallenge
        {
            ChallengeId = challengeId,
            UserId = userId,
            Purpose = purpose,
            CodeHash = HashCode(challengeId, code),
            CreatedAt = now,
            ExpiresAt = now.AddMinutes(CodeLifetimeMinutes),
            Attempts = 0,
        };

        try
        {
            var emailBody = $"""
                <html>
                  <body style="font-family:Arial,sans-serif;color:#172b35;line-height:1.5">
                    <h2>CarePlus two-factor authentication code</h2>
                    <p>Your CarePlus verification code is:</p>
                    <p style="font-size:36px;letter-spacing:10px;font-weight:700;line-height:1.2;margin:8px 0 20px 0;color:#0b2638"><strong>{code}</strong></p>
                    <p>This code expires in {CodeLifetimeMinutes} minutes. If you did not request this, you can ignore this email.</p>
                  </body>
                </html>
                """;

            await _emailSender.SendAsync(
                user.Email,
                purpose == "enable" ? "CarePlus two-factor authentication code" : "CarePlus two-factor authentication change",
                emailBody,
                isBodyHtml: true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unable to send a two-factor code for user {UserId}.", userId);
            return ApiResponse<object>.Fail("We could not send the verification email. Please try again later.", 503);
        }

        await _db.TwoFactorChallenges.AddAsync(challenge);
        await _db.SaveChangesAsync();

        return ApiResponse<object>.Ok(new TwoFactorCodeSentResponse
        {
            MaskedEmail = MaskEmail(user.Email),
            ExpiresInSeconds = CodeLifetimeMinutes * 60,
        }, "Verification code sent.");
    }

    public async Task<ApiResponse<object>> VerifyCodeAsync(long userId, TwoFactorVerifyRequest request)
    {
        var purpose = NormalizePurpose(request.Action);
        if (purpose == null)
        {
            return ApiResponse<object>.Fail("The two-factor action is invalid.", 400);
        }

        var now = DateTimeHelper.MalaysiaNow();
        var challenge = await _db.TwoFactorChallenges
            .Where(row => row.UserId == userId && row.Purpose == purpose && row.ConsumedAt == null)
            .OrderByDescending(row => row.CreatedAt)
            .FirstOrDefaultAsync();
        if (challenge == null || challenge.ExpiresAt <= now)
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
            return ApiResponse<object>.Fail(challenge.Attempts >= MaxAttempts ? "Too many incorrect attempts. Request a new code." : "That verification code is incorrect.", 400);
        }

        var setting = await _db.AppSettings.FirstOrDefaultAsync(row => row.UserId == userId);
        if (setting == null)
        {
            var isStaff = await _db.Staffs.AnyAsync(row => row.UserId == userId);
            setting = new AppSetting
            {
                UserId = userId,
                Appearance = "light",
                Language = isStaff ? null : "en",
                Notification = "all",
            };
            _db.AppSettings.Add(setting);
        }

        setting.TwoFactorEnabled = purpose == "enable";
        challenge.ConsumedAt = now;
        await _db.SaveChangesAsync();

        return ApiResponse<object>.Ok(new TwoFactorStatusResponse { Enabled = setting.TwoFactorEnabled }, "Two-factor authentication updated.");
    }

    private string HashCode(string challengeId, string code)
    {
        var secret = _configuration["JwtSettings:SecretKey"] ?? throw new InvalidOperationException("JWT secret key is missing.");
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{challengeId}:{code}:{secret}")));
    }

    private static string? NormalizePurpose(string? value)
    {
        var purpose = value?.Trim().ToLowerInvariant();
        return purpose is "enable" or "disable" ? purpose : null;
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
