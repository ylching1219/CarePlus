using CarePlus.API.DTO.ChatDTO;

namespace CarePlus.API.Services.ChatServices.Interface;

public interface IChatServices
{
    Task<ChatResponse> AnswerAsync(ChatRequest request, CancellationToken cancellationToken);
    Task<IndexKnowledgeResponse> IndexAsync(IndexKnowledgeRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<IndexKnowledgeResponse>> IndexBundledKnowledgeAsync(CancellationToken cancellationToken);
}

public interface IVectorKnowledgeStore
{
    Task ReplaceDocumentAsync(KnowledgeDocument document, IReadOnlyList<KnowledgeChunk> chunks, CancellationToken cancellationToken);
    Task<IReadOnlyList<KnowledgeMatch>> SearchAsync(string query, CancellationToken cancellationToken);
}

public interface IGroqChatClient
{
    Task<string> CompleteAsync(
        string question,
        IReadOnlyList<ChatTurnRequest> history,
        IReadOnlyList<KnowledgeMatch> context,
        CancellationToken cancellationToken);
}

public sealed record KnowledgeDocument(string Id, string Title, string Source);
public sealed record KnowledgeChunk(int Index, string Content);
public sealed record KnowledgeMatch(string DocumentId, string Title, string Source, string Content, double Score);

