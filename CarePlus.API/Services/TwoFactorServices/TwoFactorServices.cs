using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using CarePlus.API.Data;
using CarePlus.API.DTO.UserDTO;
using CarePlus.API.Extension;
using CarePlus.API.Models.User;
using CarePlus.API.Services.EmailServices;
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
            var emailBody = CarePlusEmailHtml.Otp(
                purpose == "enable" ? "Two-factor authentication" : "Security change",
                purpose == "enable"
                    ? "Use this code to turn on two-factor authentication for your CarePlus account."
                    : "Use this code to turn off two-factor authentication for your CarePlus account.",
                code,
                CodeLifetimeMinutes,
                "If you did not request this, you can ignore this email.");

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

    public async Task<ApiResponse<object>> StartLoginChallengeAsync(User user)
    {
        if (string.IsNullOrWhiteSpace(user.Email))
        {
            return ApiResponse<object>.Fail("Add an email address before using two-factor authentication.", 400);
        }

        return await CreateAndSendLoginChallengeAsync(user);
    }

    public async Task<ApiResponse<object>> ResendLoginChallengeAsync(string challengeId)
    {
        var id = (challengeId ?? "").Trim();
        if (string.IsNullOrWhiteSpace(id))
        {
            return ApiResponse<object>.Fail("This verification request is no longer valid.", 400);
        }

        var existing = await _db.TwoFactorChallenges
            .AsNoTracking()
            .FirstOrDefaultAsync(row => row.ChallengeId == id && row.Purpose == "login");
        if (existing == null)
        {
            return ApiResponse<object>.Fail("This verification request is no longer valid.", 400);
        }

        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(row => row.UserId == existing.UserId && row.Status == "Active");
        if (user == null || string.IsNullOrWhiteSpace(user.Email))
        {
            return ApiResponse<object>.Fail("User account was not found.", 404);
        }

        return await CreateAndSendLoginChallengeAsync(user);
    }

    public async Task<ApiResponse<long>> ConsumeLoginChallengeAsync(string challengeId, string code)
    {
        var id = (challengeId ?? "").Trim();
        var now = DateTimeHelper.MalaysiaNow();
        var challenge = await _db.TwoFactorChallenges
            .FirstOrDefaultAsync(row => row.ChallengeId == id && row.Purpose == "login" && row.ConsumedAt == null);
        if (challenge == null || challenge.ExpiresAt <= now)
        {
            return ApiResponse<long>.Fail("That verification code has expired. Request a new code.", 400);
        }

        if (challenge.Attempts >= MaxAttempts)
        {
            challenge.ConsumedAt = now;
            await _db.SaveChangesAsync();
            return ApiResponse<long>.Fail("Too many incorrect attempts. Request a new code.", 429);
        }

        challenge.Attempts++;
        var expected = Convert.FromHexString(challenge.CodeHash);
        var actual = Convert.FromHexString(HashCode(challenge.ChallengeId, code.Trim()));
        if (!CryptographicOperations.FixedTimeEquals(expected, actual))
        {
            if (challenge.Attempts >= MaxAttempts)
            {
                challenge.ConsumedAt = now;
            }
            await _db.SaveChangesAsync();
            return ApiResponse<long>.Fail(
                challenge.Attempts >= MaxAttempts ? "Too many incorrect attempts. Request a new code." : "That verification code is incorrect.",
                challenge.Attempts >= MaxAttempts ? 429 : 400);
        }

        challenge.ConsumedAt = now;
        await _db.SaveChangesAsync();
        return ApiResponse<long>.Ok(challenge.UserId, "Verification successful.");
    }

    private async Task<ApiResponse<object>> CreateAndSendLoginChallengeAsync(User user)
    {
        var now = DateTimeHelper.MalaysiaNow();
        var recent = await _db.TwoFactorChallenges
            .Where(row => row.UserId == user.UserId
                && row.Purpose == "login"
                && row.ConsumedAt == null
                && row.ExpiresAt > now
                && row.CreatedAt > now.AddMinutes(-1))
            .OrderByDescending(row => row.CreatedAt)
            .FirstOrDefaultAsync();
        if (recent != null)
        {
            return ApiResponse<object>.Ok(new LoginTwoFactorRequiredResponse
            {
                ChallengeId = recent.ChallengeId,
                MaskedEmail = MaskEmail(user.Email),
                ExpiresInSeconds = Math.Max(1, (int)Math.Ceiling((recent.ExpiresAt - now).TotalSeconds)),
            }, "A verification code is already active.");
        }

        var previous = await _db.TwoFactorChallenges
            .Where(row => row.UserId == user.UserId && row.Purpose == "login" && row.ConsumedAt == null)
            .ToListAsync();
        foreach (var row in previous)
        {
            row.ConsumedAt = now;
        }

        var challengeId = Guid.NewGuid().ToString("N");
        var code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6", CultureInfo.InvariantCulture);
        var challenge = new TwoFactorChallenge
        {
            ChallengeId = challengeId,
            UserId = user.UserId,
            Purpose = "login",
            CodeHash = HashCode(challengeId, code),
            CreatedAt = now,
            ExpiresAt = now.AddMinutes(CodeLifetimeMinutes),
            Attempts = 0,
        };

        try
        {
            var emailBody = CarePlusEmailHtml.Otp(
                "Sign-in verification",
                "Use this code to finish signing in to your CarePlus account.",
                code,
                CodeLifetimeMinutes,
                "If you did not try to sign in, you can ignore this email.");

            await _emailSender.SendAsync(
                user.Email,
                "CarePlus sign-in verification code",
                emailBody,
                isBodyHtml: true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unable to send a login two-factor code for user {UserId}.", user.UserId);
            return ApiResponse<object>.Fail("We could not send the verification email. Please try again later.", 503);
        }

        await _db.TwoFactorChallenges.AddAsync(challenge);
        await _db.SaveChangesAsync();

        return ApiResponse<object>.Ok(new LoginTwoFactorRequiredResponse
        {
            ChallengeId = challengeId,
            MaskedEmail = MaskEmail(user.Email),
            ExpiresInSeconds = CodeLifetimeMinutes * 60,
        }, "Verification code sent.");
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
