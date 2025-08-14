using TheAssistant.Core.Infrastructure;

namespace TheAssistant.Core.Agents
{
    public interface IAgentRouter
    {
        Task<List<AgentMessage>> RouteAsync(string message, UserDetails user);
    }

}
