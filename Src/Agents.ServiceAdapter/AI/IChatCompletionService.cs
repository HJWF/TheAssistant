using Microsoft.Extensions.AI;

namespace TheAssistant.Agents.ServiceAdapter.AI
{
    public interface IChatCompletionService
    {
        Task<ChatMessage> GetChatMessageContentAsync(ChatHistory history, CancellationToken cancellationToken = default);
        Task<ChatMessage> GetChatMessageContentAsync(ChatHistory history, ChatOptions? options, CancellationToken cancellationToken = default);
    }
}
