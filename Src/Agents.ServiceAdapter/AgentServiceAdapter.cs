using Microsoft.Extensions.Logging;
using TheAssistant.Agents.ServiceAdapter.Formatting;
using TheAssistant.Core;
using TheAssistant.Core.Agents;

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
        public async Task<string> HandleMessageAsync(string userInput)
        {
            var routedMessages = await _router.RouteAsync(userInput);
            var userMessages = new List<AgentMessage>();

            var queue = new Queue<AgentMessage>();
            routedMessages.ForEach(queue.Enqueue);

            while (queue.Count > 0)
            {
                var message = queue.Dequeue();

                var agent = GetAgent(message.Receiver);
                if (agent == null)
                {
                    continue;
                }

                var response = await agent.HandleAsync(message);

                if (response.Receiver.Equals("User", StringComparison.OrdinalIgnoreCase))
                {
                    userMessages.Add(response);
                }
                else if (response.Receiver.Equals("Agent", StringComparison.OrdinalIgnoreCase))
                {
                    queue.Enqueue(response);
                }
                else if (response.Receiver.Equals("Router", StringComparison.OrdinalIgnoreCase))
                {
                    var newRoutes = await _router.RouteAsync(response.Content);
                    newRoutes.ForEach(queue.Enqueue);
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
