using TheAssistant.Core.Infrastructure;

namespace TheAssistant.Core
{
    public interface IAgentServiceAdapter
    {
        Task<string> HandleMessageAsync(string message, UserDetails user);
    }
}
