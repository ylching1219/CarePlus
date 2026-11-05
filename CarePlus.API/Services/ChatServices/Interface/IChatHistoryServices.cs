using CarePlus.API.DTO.ChatDTO;

namespace CarePlus.API.Services.ChatServices.Interface;

public interface IChatHistoryServices
{
    Task<IReadOnlyList<ChatThreadResponse>> ListAsync(string patientId, CancellationToken cancellationToken);
    Task<ChatThreadResponse?> SendAsync(string patientId, PersistedChatMessageRequest request, CancellationToken cancellationToken);
    Task<int> DeleteAsync(string patientId, IReadOnlyCollection<string> threadIds, CancellationToken cancellationToken);
}
