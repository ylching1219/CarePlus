using CarePlus.API.Data;
using CarePlus.API.DTO.ChatDTO;
using CarePlus.API.Models.Chat;
using CarePlus.API.Services.ChatServices.Interface;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace CarePlus.API.Services.ChatServices;

public sealed class ChatHistoryServices : IChatHistoryServices
{
    private const int MaxMessagesPerHour = 15;
    private static readonly TimeSpan MinimumMessageInterval = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan DuplicateMessageWindow = TimeSpan.FromMinutes(1);

    private readonly AppDbContext _db;
    private readonly IChatServices _chat;

    public ChatHistoryServices(AppDbContext db, IChatServices chat)
    {
        _db = db;
        _chat = chat;
    }

    public async Task<IReadOnlyList<ChatThreadResponse>> ListAsync(
        string patientId,
        CancellationToken cancellationToken)
    {
        var threads = await _db.ChatThreads
            .AsNoTracking()
            .Where(thread => thread.PatientId == patientId)
            .Include(thread => thread.Messages)
            .OrderByDescending(thread => thread.UpdatedAt)
            .ToListAsync(cancellationToken);

        return threads.Select(ToResponse).ToArray();
    }

    public async Task<ChatThreadResponse?> SendAsync(
        string patientId,
        PersistedChatMessageRequest request,
        CancellationToken cancellationToken)
    {
        var text = request.Message.Trim();
        await EnforceSpamLimitsAsync(patientId, text, cancellationToken);
        ChatThread? thread = null;

        if (!string.IsNullOrWhiteSpace(request.ThreadId))
        {
            if (!long.TryParse(request.ThreadId, out var threadId))
            {
                return null;
            }

            thread = await _db.ChatThreads
                .Include(item => item.Messages)
                .SingleOrDefaultAsync(
                    item => item.ChatThreadId == threadId && item.PatientId == patientId,
                    cancellationToken);

            if (thread == null)
            {
                return null;
            }
        }

        var history = thread?.Messages
            .OrderBy(message => message.CreatedAt)
            .ThenBy(message => message.ChatMessageId)
            .TakeLast(12)
            .Select(message => new ChatTurnRequest
            {
                Role = message.Role,
                Content = message.Content
            })
            .ToList() ?? [];

        // Generate the response before tracking new rows. A provider failure must
        // not leave an unanswered user message in the database.
        var answer = await _chat.AnswerAsync(new ChatRequest
        {
            Message = text,
            History = history
        }, cancellationToken);

        var now = DateTime.UtcNow;
        if (thread == null)
        {
            thread = new ChatThread
            {
                PatientId = patientId,
                Title = TitleFrom(text),
                CreatedAt = now,
                UpdatedAt = now
            };
            _db.ChatThreads.Add(thread);
        }
        else
        {
            thread.UpdatedAt = now;
        }

        thread.Messages.Add(new ChatMessage
        {
            Role = "user",
            Content = text,
            CreatedAt = now
        });
        thread.Messages.Add(new ChatMessage
        {
            Role = "assistant",
            Content = answer.Reply,
            Disclaimer = string.IsNullOrWhiteSpace(answer.Disclaimer) ? null : answer.Disclaimer,
            SourcesJson = answer.Sources.Count == 0 ? null : JsonSerializer.Serialize(answer.Sources),
            CreatedAt = now.AddTicks(1)
        });

        await _db.SaveChangesAsync(cancellationToken);
        return ToResponse(thread);
    }

    public async Task<int> DeleteAsync(
        string patientId,
        IReadOnlyCollection<string> threadIds,
        CancellationToken cancellationToken)
    {
        var parsedIds = threadIds
            .Select(id => long.TryParse(id, out var value) ? value : (long?)null)
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct()
            .ToArray();

        if (parsedIds.Length == 0)
        {
            return 0;
        }

        var threads = await _db.ChatThreads
            .Where(thread => thread.PatientId == patientId && parsedIds.Contains(thread.ChatThreadId))
            .ToListAsync(cancellationToken);

        _db.ChatThreads.RemoveRange(threads);
        await _db.SaveChangesAsync(cancellationToken);
        return threads.Count;
    }

    private async Task EnforceSpamLimitsAsync(
        string patientId,
        string text,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var hourAgo = now.Subtract(TimeSpan.FromHours(1));
        var recentMessages = await _db.ChatMessages
            .AsNoTracking()
            .Where(message =>
                message.ChatThread.PatientId == patientId &&
                message.Role == "user" &&
                message.CreatedAt >= hourAgo)
            .OrderByDescending(message => message.CreatedAt)
            .Take(MaxMessagesPerHour)
            .Select(message => new { message.Content, message.CreatedAt })
            .ToListAsync(cancellationToken);

        if (recentMessages.Count >= MaxMessagesPerHour)
        {
            throw new ChatRateLimitException("rate_limit", "You can send up to 15 messages per hour. Please try again later.");
        }

        var normalized = NormalizeMessage(text);
        if (recentMessages.Any(message =>
                message.CreatedAt >= now.Subtract(DuplicateMessageWindow) &&
                NormalizeMessage(message.Content) == normalized))
        {
            throw new ChatRateLimitException("duplicate", "You recently sent the same message. Please wait before sending it again.");
        }

        if (recentMessages.FirstOrDefault()?.CreatedAt >= now.Subtract(MinimumMessageInterval))
        {
            throw new ChatRateLimitException("cooldown", "Please wait a few seconds before sending another message.");
        }
    }

    private static string NormalizeMessage(string value) =>
        string.Join(' ', value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)).ToUpperInvariant();

    private static string TitleFrom(string text)
    {
        var normalized = string.Join(' ', text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        return normalized.Length > 42 ? $"{normalized[..39]}…" : normalized;
    }

    private static ChatThreadResponse ToResponse(ChatThread thread) => new(
        thread.ChatThreadId.ToString(),
        thread.Title,
        ToUnixMilliseconds(thread.UpdatedAt),
        thread.Messages
            .OrderBy(message => message.CreatedAt)
            .ThenBy(message => message.ChatMessageId)
            .Select(message => new ChatMessageResponse(
                message.ChatMessageId.ToString(),
                message.Role,
                message.Content,
                ToUnixMilliseconds(message.CreatedAt),
                message.Disclaimer,
                DeserializeSources(message.SourcesJson)))
            .ToArray());

    private static IReadOnlyList<ChatSourceResponse>? DeserializeSources(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<ChatSourceResponse[]>(json);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static long ToUnixMilliseconds(DateTime value) =>
        new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Utc)).ToUnixTimeMilliseconds();
}

public sealed class ChatRateLimitException : Exception
{
    public ChatRateLimitException(string reason, string message) : base(message)
    {
        Reason = reason;
    }

    public string Reason { get; }
}
