using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Text.Json;
using TheAssistant.Core;

namespace TheAssistant.Agents.ServiceAdapter.Agenda
{
    public class AgendaAgent : IAgendaAgent
    {
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
            var appointments = await _agendaServiceAdapter.GetMeetings(DateTime.Today);

            var systemPrompt = """
                You are a helpful assistant summarizing agendas.
                Use the following appointments to create a short daily overview in Dutch.
            """;

            var chat = _kernel.GetRequiredService<IChatCompletionService>();
            var history = new ChatHistory();
            history.AddSystemMessage(systemPrompt);
            history.AddUserMessage(JsonSerializer.Serialize(appointments));

            var reply = await chat.GetChatMessageContentAsync(history);
            return reply.Content;
        }
    }

}
