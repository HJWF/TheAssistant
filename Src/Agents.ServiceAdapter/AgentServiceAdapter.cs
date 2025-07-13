using TheAssistant.Agents.ServiceAdapter.Agenda;
using TheAssistant.Core;
using TheAssistant.Core.Messaging;

namespace TheAssistant.Agents.ServiceAdapter
{
    public class AgentServiceAdapter : IAgentServiceAdapter
    {
        private readonly IRouter _router;
        private readonly IAgendaAgent _agendaAgent;

        public AgentServiceAdapter(IRouter router, IAgendaAgent agendaAgent)
        {
            _router = router;
            _agendaAgent = agendaAgent;
        }

        public async Task<string> HandleMessageAsync(Message message)
        {
            var targetAgent = await _router.RouteAsync(message.Content);

            if (targetAgent.Contains("agenda", StringComparison.InvariantCultureIgnoreCase))
            {
                return await _agendaAgent.HandleAsync(message.Content);
            }
            // if (targetAgent.Contains("recipes"))
            //     return await _recipeAgent.HandleAsync(message);
            // if (targetAgent.Contains("weather"))
            //     return await _weatherAgent.HandleAsync(message);
            // if (targetAgent.Contains("content"))
            //     return await _contentAgent.HandleAsync(message);

            return "Sorry, I don't know how to handle that.";
        }
    }
}
