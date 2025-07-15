using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Text.Json;
using TheAssistant.Core;
using TheAssistant.Core.Agents;

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

        public string Name => "agenda-agent";

        public AgendaAgent(IAgendaServiceAdapter agendaServiceAdapter, Kernel kernel)
        {
            _agendaServiceAdapter = agendaServiceAdapter;
            _kernel = kernel;
        }

        //[KernelFunction]
        //public async Task<string> HandleAsync(string input)
        //{
        //    var agendaDetails = await _agendaServiceAdapter.GetMeetings(DateTime.Today);

        //    var chat = _kernel.GetRequiredService<IChatCompletionService>();
        //    var history = new ChatHistory();
        //    history.AddSystemMessage(Prompt);
        //    history.AddUserMessage(JsonSerializer.Serialize(agendaDetails));

        //    var reply = await chat.GetChatMessageContentAsync(history);
        //    return reply.Content ?? AgentConstants.SorryMessage;
        //}

        [KernelFunction]
        public async Task<AgentMessage> HandleAsync(AgentMessage message)
        {
            var agendaDetails = await _agendaServiceAdapter.GetMeetings(DateTime.Today);

            var chat = _kernel.GetRequiredService<IChatCompletionService>();
            var history = new ChatHistory();
            history.AddSystemMessage(Prompt);
            history.AddUserMessage(JsonSerializer.Serialize(agendaDetails));

            var reply = await chat.GetChatMessageContentAsync(history);

            return new AgentMessage
            {
                Sender = Name,
                Receiver = "user",
                Role = "agent",
                Content = reply.Content ?? AgentConstants.SorryMessage
            };
        }
    }

}
