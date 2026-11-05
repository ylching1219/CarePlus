using System.ComponentModel.DataAnnotations;

namespace CarePlus.API.DTO.ChatDTO;

public sealed class ChatRequest
{
    [Required]
    [StringLength(2000, MinimumLength = 1)]
    public string Message { get; set; } = string.Empty;

    [MaxLength(12)]
    public List<ChatTurnRequest> History { get; set; } = [];
}

public sealed class ChatTurnRequest
{
    [Required]
    [RegularExpression("^(user|assistant)$")]
    public string Role { get; set; } = string.Empty;

    [Required]
    [StringLength(2000, MinimumLength = 1)]
    public string Content { get; set; } = string.Empty;
}

public sealed record ChatSourceResponse(string Title, string Source, double Score);

public sealed record ChatResponse(
    string Reply,
    string Disclaimer,
    IReadOnlyList<ChatSourceResponse> Sources);

public sealed class PersistedChatMessageRequest
{
    public string? ThreadId { get; set; }

    [Required]
    [StringLength(2000, MinimumLength = 1)]
    public string Message { get; set; } = string.Empty;
}

public sealed class DeleteChatThreadsRequest
{
    [Required]
    [MinLength(1)]
    [MaxLength(100)]
    public List<string> ThreadIds { get; set; } = [];
}

public sealed record ChatMessageResponse(
    string Id,
    string Role,
    string Text,
    long At,
    string? Disclaimer,
    IReadOnlyList<ChatSourceResponse>? Sources);

public sealed record ChatThreadResponse(
    string Id,
    string Title,
    long UpdatedAt,
    IReadOnlyList<ChatMessageResponse> Messages);

public sealed class IndexKnowledgeRequest
{
    [Required]
    [StringLength(120)]
    public string DocumentId { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(500)]
    public string Source { get; set; } = string.Empty;

    [Required]
    [StringLength(100_000, MinimumLength = 20)]
    public string Content { get; set; } = string.Empty;
}

public sealed record IndexKnowledgeResponse(string DocumentId, int ChunkCount);

