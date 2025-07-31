using Microsoft.SemanticKernel;
using TheAssistant.Core.Agents;

namespace TheAssistant.Agents.ServiceAdapter.DailyUpdate
{
    public class DailyUpdateAgent : IDailyUpdateAgent
    {
        public string Name => AgentConstants.Names.DailyUpdate;

        [KernelFunction]
        public Task<IEnumerable<AgentMessage>> HandleAsync(AgentMessage message) => Task.FromResult(new List<AgentMessage>
            {
            new(message.UserId, Name, AgentConstants.Names.Agenda, AgentConstants.Roles.User, "What are today's meetings?", new Dictionary<string, string> { { "replyTo", Name } }),
            new(message.UserId, Name, AgentConstants.Names.Weather, AgentConstants.Roles.User, "What's the weather forecast for today?", new Dictionary<string, string> { { "replyTo", Name } }),
            }.AsEnumerable());
    }
}