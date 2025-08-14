using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using TheAssistant.Agents.ServiceAdapter.Agenda.Events;
using TheAssistant.Agents.ServiceAdapter.Authentication;
using TheAssistant.Core;
using TheAssistant.Core.Agents;
using TheAssistant.Core.Authentication;

namespace TheAssistant.Agents.ServiceAdapter.Agenda
{
    public class AgendaAgent : IAgendaAgent
    {
        private readonly ITokenStoreServiceAdapter _tokenStoreServiceAdapter;
        private readonly ILoginUrlProvider _loginUrlProvider;
        private readonly Kernel _kernel;
        private readonly ILogger<AgendaAgent> _logger;
        private readonly IEventService _eventService;
        private readonly IChatCompletionService _chatCompletionService;

        public string Name => AgentConstants.Names.Agenda;

        public AgendaAgent(Kernel kernel,
            ITokenStoreServiceAdapter tokenStoreServiceAdapter,
            ILoginUrlProvider loginUrlProvider,
            ILogger<AgendaAgent> logger,
            IEventService eventService)
        {
            _kernel = kernel;
            _tokenStoreServiceAdapter = tokenStoreServiceAdapter;
            _loginUrlProvider = loginUrlProvider;
            _logger = logger;
            _eventService = eventService;
            _chatCompletionService = _kernel.GetRequiredService<IChatCompletionService>();
        }

        [KernelFunction]
        public async Task<IEnumerable<AgentMessage>> HandleAsync(AgentMessage message)
        {
            if (message.User == null)
            {
                return [new(message.User, Name, AgentConstants.Roles.User, AgentConstants.Roles.Agent, 
                    "User details are missing.", null)];
            }

            var (success, errorMessage, token) = await ValidateAndGetToken(message.User.PersonalMailTag);
            if (!success)
            {
                return [new(message.User, Name, AgentConstants.Roles.User, AgentConstants.Roles.Agent, 
                    errorMessage, null)];
            }

            try 
            {
                var events = await GetEvents(message, token);

                var formattedEvents = await PrepareEventsForUser(events);

                return [new(message.User, Name, AgentConstants.Roles.User, AgentConstants.Roles.Agent, 
                    formattedEvents ?? AgentConstants.SorryMessage, null)];
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in HandleAsync for user {UserId}", message.User.PersonalMailTag);
                return [new(message.User, Name, AgentConstants.Roles.User, AgentConstants.Roles.Agent, 
                    "Sorry, I encountered an error while fetching your calendar.", null)];
            }
        }

        private async Task<(bool success, string? errorMessage, Token? token)> ValidateAndGetToken(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return (false, "User ID is missing.", null);
            }

            var token = await _tokenStoreServiceAdapter.GetToken(userId);
            
            if (token == null || token.ExpiresAt <= DateTime.Now)
            {
                var loginUrl = _loginUrlProvider.GetLoginUrlForUser(userId);
                return (false, $"I need access to your calendar. Please log in: {loginUrl}", null);
            }

            return (true, null, token);
        }

        private async Task<string> GetEvents(AgentMessage message, Token token)
        {
            var today = DateTime.Today.ToString("yyyy-MM-dd");
            var intentHistory = new ChatHistory();
            intentHistory.AddSystemMessage(Prompts.IntentPrompt(today, message.Content));

            var intentReply = await _chatCompletionService.GetChatMessageContentAsync(intentHistory);

            var match = Regex.Match(intentReply.Content, @"\{.*\}");
            if (!match.Success)
            {
                throw new Exception("No valid JSON found in LLM response.");
            }

            var intent = JsonConvert.DeserializeObject<IntentInstruction>(match.Value);

            return intent.Action switch
            {
                "get_todays_meetings" => await _eventService.GetTodaysEvents(message.User.WorkMailTag, token),
                "get_meetings" => await _eventService.GetMeetings(intent.Date, token),
                "get_birthdays" => await _eventService.GetBirthdays(intent.Date, token),
                var _ => string.Empty
            };
        }

        private async Task<string?> PrepareEventsForUser(string events)
        {
            var history = new ChatHistory();
            history.AddSystemMessage(Prompts.FormatPrompt);
            history.AddUserMessage(events);

            var reply = await _chatCompletionService.GetChatMessageContentAsync(history);
            return reply.Content;
        }
    }
}
