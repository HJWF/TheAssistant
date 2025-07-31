namespace TheAssistant.Core
{
    public interface IAgentServiceAdapter
    {
        Task<string> HandleMessageAsync(string message, string userId);
    }
}
