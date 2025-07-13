using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Text.Json;
using TheAssistant.Core;

namespace TheAssistant.Agents.ServiceAdapter.Agenda
{
    public class AgendaAgent : IAgendaAgent
    {
        private const string Prompt = """
                You are a helpful assistant summarizing agendas.
                Use the following appointments to create a short daily overview in Dutch.
            """;
        private readonly IAgendaServiceAdapter _agendaServiceAdapter;
        private readonly Kernel _kernel;

        public AgendaAgent(IAgendaServiceAdapter agendaServiceAdapter, Kernel kernel)
        {
            _agendaServiceAdapter = agendaServiceAdapter;
            _kernel = kernel;
        }

        [KernelFunction]
        public async Task<string> HandleAsync(string input)
        {
            var weatherDetails = await _agendaServiceAdapter.GetMeetings(DateTime.Today);

            var chat = _kernel.GetRequiredService<IChatCompletionService>();
            var history = new ChatHistory();
            history.AddSystemMessage(Prompt);
            history.AddUserMessage(JsonSerializer.Serialize(weatherDetails));

            var reply = await chat.GetChatMessageContentAsync(history);
            return reply.Content ?? AgentConstants.SorryMessage;
        }
    }

}
