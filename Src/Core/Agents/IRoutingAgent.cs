using TheAssistant.Core.Infrastructure;

namespace TheAssistant.Core.Agents
{
    public interface IRoutingAgent
    {
        Task<List<AgentMessage>> RouteAsync(string message, UserDetails user);
    }

}
