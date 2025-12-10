namespace TheAssistant.Agents.ServiceAdapter.AI
{
    public interface IChatCompletionService
    {
        Task<ChatMessage> GetChatMessageContentAsync(ChatHistory history, CancellationToken cancellationToken = default);
    }
}
