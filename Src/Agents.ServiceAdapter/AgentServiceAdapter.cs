using TheAssistant.Agents.ServiceAdapter.Agenda;
using TheAssistant.Agents.ServiceAdapter.Formatting;
using TheAssistant.Agents.ServiceAdapter.Routing;
using TheAssistant.Agents.ServiceAdapter.Weather;
using TheAssistant.Core;
using TheAssistant.Core.Agents;

namespace TheAssistant.Agents.ServiceAdapter
{
    public class AgentServiceAdapter : IAgentServiceAdapter
    {
        private readonly IRouter _router;
        private readonly IAgendaAgent _agendaAgent;
        private readonly IWeatherAgent _weatherAgent;
        private readonly IFormattingAgent _formattingAgent;

        public AgentServiceAdapter(IRouter router, IAgendaAgent agendaAgent, IWeatherAgent weatherAgent, IFormattingAgent formattingAgent)
        {
            _router = router;
            _agendaAgent = agendaAgent;
            _weatherAgent = weatherAgent;
            _formattingAgent = formattingAgent;
        }

        public async Task<string> HandleMessageAsync(string message)
        {
            var responses = new List<AgentResponse>();

            var routes = await _router.RouteAsync(message);

            foreach (var route in routes)
            {
                var agent = GetAgent(route.Name);
                if (agent != null)
                {
                    var response = await agent.HandleAsync(route.Input);
                    responses.Add(new(route.Name, response));
                }
            }

            return await _formattingAgent.HandleAsync(responses);
        }

        private IAgent GetAgent(string agentName)
        {
            if (agentName.Equals("agenda", StringComparison.InvariantCultureIgnoreCase))
            {
                return _agendaAgent;
            }
            if (agentName.Equals("weather", StringComparison.InvariantCultureIgnoreCase))
            {
                return _weatherAgent;
            }

            throw new NotSupportedException($"Agent '{agentName}' is not supported.");
        } 
    }
}
