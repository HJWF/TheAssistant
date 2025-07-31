using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Text.Json;
using TheAssistant.Agents.ServiceAdapter.Authentication;
using TheAssistant.Core;
using TheAssistant.Core.Agents;

namespace TheAssistant.Agents.ServiceAdapter.Agenda
{
    public class AgendaAgent : IAgendaAgent
    {
        private const string Prompt = """
                You are a helpful assistant formatting calendar events.

                Use the event data provided (not below) and format it **exactly** like this:

                08:30-09:30 Daily Standup - Teams (jan@company.com)  
                10:00-11:00 Project X - Amsterdam HQ (lisa@company.com)  
                14:00-15:30 Dev Review - Online (tom@company.com)

                Rules:
                - Do **not** include any explanation or extra text before or after the list.
                - If no events are found, respond with:  
                  No events found for today.
                - If any field is missing or empty, skip that part silently.
            """;
        private readonly IAgendaServiceAdapter _agendaServiceAdapter;
        private readonly ITokenStoreServiceAdapter _tokenStoreServiceAdapter;
        private readonly ILoginUrlProvider _loginUrlProvider;
        private readonly Kernel _kernel;

        public string Name => AgentConstants.Names.Agenda;

        public AgendaAgent(IAgendaServiceAdapter agendaServiceAdapter, Kernel kernel, ITokenStoreServiceAdapter tokenStoreServiceAdapter, ILoginUrlProvider loginUrlProvider)
        {
            _agendaServiceAdapter = agendaServiceAdapter;
            _kernel = kernel;
            _tokenStoreServiceAdapter = tokenStoreServiceAdapter;
            _loginUrlProvider = loginUrlProvider;
        }

        [KernelFunction]
        public async Task<IEnumerable<AgentMessage>> HandleAsync(AgentMessage message)
        {
            var token = await _tokenStoreServiceAdapter.GetToken(message.UserId);
            if (token == null || token.ExpiresAt <= DateTime.Now || string.IsNullOrWhiteSpace(token.AccessToken) || string.IsNullOrWhiteSpace(token.RefreshToken))
            {
                var loginUrl = _loginUrlProvider.GetLoginUrlForUser(message.UserId);
                return new List<AgentMessage> { 
                    new AgentMessage(message.UserId, Name, AgentConstants.Roles.User, AgentConstants.Roles.Agent, $"I need access to your calendar. Please log in: {loginUrl}", null) 
                };

            }

            var events = await _agendaServiceAdapter.GetTodayEvents(message.UserId, token);

            var formattedEvents = string.Join("\n", events.Select(e =>
            {
                var time = $"{e.Start:HH:mm}-{e.End:HH:mm}";
                var subject = e.Subject ?? "";
                var location = string.IsNullOrWhiteSpace(e.Location) ? "" : $" - {e.Location}";
                var organizer = string.IsNullOrWhiteSpace(e.Organizer) ? "" : $" ({e.Organizer})";
                return $"{time} {subject}{location}{organizer}";
            }));

            var chat = _kernel.GetRequiredService<IChatCompletionService>();
            var history = new ChatHistory();
            history.AddSystemMessage(Prompt);
            history.AddUserMessage(formattedEvents);

            var reply = await chat.GetChatMessageContentAsync(history);

            return new List<AgentMessage> {
                    new AgentMessage(message.UserId, Name, AgentConstants.Roles.User, AgentConstants.Roles.Agent, reply.Content ?? AgentConstants.SorryMessage, null)
            };
        }
    }
}
