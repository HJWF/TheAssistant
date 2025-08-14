using Microsoft.Extensions.Logging;
using TheAssistant.Agents.ServiceAdapter.Formatting;
using TheAssistant.Core;
using TheAssistant.Core.Agents;
using TheAssistant.Core.Infrastructure;

namespace TheAssistant.Agents.ServiceAdapter
{
    public class AgentServiceAdapter : IAgentServiceAdapter
    {
        private readonly IAgentRouter _router;
        private readonly IEnumerable<IAgent> _agents;
        private readonly IFormattingAgent _formattingAgent;
        private readonly ILogger<AgentServiceAdapter> _logger;

        private readonly Dictionary<string, IAgent> _agentMap;

        public AgentServiceAdapter(IAgentRouter router, IEnumerable<IAgent> agents, IFormattingAgent formattingAgent, ILogger<AgentServiceAdapter> logger)
        {
            _router = router;
            _agents = agents;
            _formattingAgent = formattingAgent;

            _agentMap = _agents.ToDictionary(a => a.Name, StringComparer.OrdinalIgnoreCase);
            _logger = logger;
        }

        public async Task<string> HandleMessageAsync(string userInput, UserDetails user)
        {
            if (user == null || string.IsNullOrWhiteSpace(userInput))
            {
                throw new ArgumentException("User and input message cannot be null or empty.");
            }

            var routedMessages = await _router.RouteAsync(userInput, user);
            var userMessages = new List<AgentMessage>();

            var queue = new Queue<AgentMessage>(routedMessages);

            while (queue.Count > 0)
            {
                var message = queue.Dequeue();

                if (string.IsNullOrWhiteSpace(message.Receiver))
                {
                    continue;
                }

                var agent = GetAgent(message.Receiver);
                if (agent == null)
                {
                    continue;
                }

                var responses = await agent.HandleAsync(message);

                foreach (var response in responses)
                {
                    switch (response.Receiver.ToLowerInvariant())
                    {
                        case AgentConstants.Roles.User:
                            userMessages.Add(response);
                            break;
                        case AgentConstants.Roles.Router:
                            var newRoutes = await _router.RouteAsync(response.Content, response.User);
                            newRoutes.ForEach(queue.Enqueue);
                            break;
                        default:
                            queue.Enqueue(response);
                            break;
                    }
                }
            }

            var agentResponses = userMessages.Select(m => new AgentResponse(m.Sender, m.Content)).ToList();

            return await _formattingAgent.HandleAsync(agentResponses);
        }

        private IAgent? GetAgent(string agentName)
        {
            if(_agentMap.TryGetValue(agentName, out var agent))
            {
                return agent;
            }

            _logger.LogWarning("Agent '{AgentName}' not found.", agentName);

            return null;
        }
    }
}
