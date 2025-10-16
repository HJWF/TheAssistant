using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using TheAssistant.Agents.ServiceAdapter.Agenda.Events;
using TheAssistant.Agents.ServiceAdapter.Authentication;
using TheAssistant.Core;
using TheAssistant.Core.Agenda;
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

        private const string TokenType = "microsoftconsumer";
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
                return [new(message.User!, Name, AgentConstants.Roles.User, AgentConstants.Roles.Agent, 
                    "User details are missing.", null)];
            }

            var (success, errorMessage, token) = await ValidateAndGetToken(message.User.PersonalMailTag, TokenType);
            if (!success)
            {
                return [new(message.User, Name, AgentConstants.Roles.User, AgentConstants.Roles.Agent, 
                    errorMessage ?? AgentConstants.SorryMessage, null)];
            }

            try 
            {
                // 1. Fetch events as structured JSON
                var eventsJson = await GetEvents(message, token!);
                var events = JsonConvert.DeserializeObject<IEnumerable<CalendarEvent>>(eventsJson);

                // 2. Extract dynamic intent from question
                var intent = await ExtractIntentAsync(message.Content);

                // 3. Filter events based on intent (time ranges, day, etc.)
                var filteredEvents = FilterEventsByIntent(events, intent);

                // 4. Format the filtered events for user
                var formattedAnswer = await PrepareEventsForUser(filteredEvents);

                // 5. Evaluate answer quality
                var (score, reason) = await EvaluateAnswerQualityAsync(message.Content, filteredEvents, formattedAnswer);
                _logger.LogInformation("Answer quality score: {Score}, reason: {Reason}", score, reason);

                // 6. Optionally refine answer if score is too low
                if (score < 0.7)
                {
                    formattedAnswer = await RefineAnswerAsync(message.Content, filteredEvents, formattedAnswer);
                }

                return [new(message.User, Name, AgentConstants.Roles.User, AgentConstants.Roles.Agent,
                    formattedAnswer ?? AgentConstants.SorryMessage, null)];
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in HandleAsync for user {UserId}", message.User.PersonalMailTag);
                return [new(message.User, Name, AgentConstants.Roles.User, AgentConstants.Roles.Agent, 
                    "Sorry, I encountered an error while fetching your calendar.", null)];
            }
        }

        private async Task<(bool success, string? errorMessage, Token? token)> ValidateAndGetToken(string userId, string type)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return (false, "User ID is missing.", null);
            }

            var token = await _tokenStoreServiceAdapter.GetToken(userId, type);
            
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

        //private async Task<string?> PrepareEventsForUser(string events)
        //{
        //    var history = new ChatHistory();
        //    history.AddSystemMessage(Prompts.FormatPrompt);
        //    history.AddUserMessage(events);

        //    var reply = await _chatCompletionService.GetChatMessageContentAsync(history);
        //    return reply.Content;
        //}

        private async Task<EventQueryIntent> ExtractIntentAsync(string question)
        {
            var history = new ChatHistory();
            history.AddSystemMessage(Prompts.ExtractQuestionIntentPrompt(question));

            var reply = await _chatCompletionService.GetChatMessageContentAsync(history);
            var json = ExtractJson(reply.Content);

            try
            {
                return JsonConvert.DeserializeObject<EventQueryIntent>(json) ?? new(null, null, string.Empty);
            }
            catch
            {
                return new EventQueryIntent(null, null, string.Empty);
            }
        }

        private static string ExtractJson(string content)
        {
            var start = content.IndexOf('{');
            var end = content.LastIndexOf('}');
            if (start < 0 || end <= start)
            {
                return "{}";
            }

            return content.Substring(start, end - start + 1);
        }

        private static List<CalendarEvent> FilterEventsByIntent(IEnumerable<CalendarEvent> events, EventQueryIntent intent)
        {
            var filtered = events.AsEnumerable();

            if (intent.Start.HasValue)
            {
                filtered = filtered.Where(e => e.Start.TimeOfDay >= intent.Start.Value);
            }

            if (intent.End.HasValue)
            {
                filtered = filtered.Where(e => e.Start.TimeOfDay <= intent.End.Value);
            }

            return filtered.ToList();
        }

        private async Task<string?> PrepareEventsForUser(List<CalendarEvent> events)
        {
            var history = new ChatHistory();
            history.AddSystemMessage(Prompts.FormatPrompt);

            var json = JsonConvert.SerializeObject(events);
            history.AddUserMessage($"Here is the JSON event data:\n{json}");

            var reply = await _chatCompletionService.GetChatMessageContentAsync(history);
            return reply.Content;
        }

        private async Task<(double score, string? reason)> EvaluateAnswerQualityAsync(string question, List<CalendarEvent> filteredEvents, string answer)
        {
            var history = new ChatHistory();
            history.AddSystemMessage(Prompts.EvaluationPrompt(question, answer, JsonConvert.SerializeObject(filteredEvents)));
            var evalReply = await _chatCompletionService.GetChatMessageContentAsync(history);
            var json = ExtractJson(evalReply.Content);

            try
            {
                var eval = JsonConvert.DeserializeObject<EvaluationResult>(json);
                return (eval?.Score ?? 0, eval?.Reason);
            }
            catch
            {
                return (0, "Failed to parse evaluator output");
            }
        }

        private async Task<string> RefineAnswerAsync(string question, List<CalendarEvent> filteredEvents, string answer)
        {
            var history = new ChatHistory();
            history.AddSystemMessage(Prompts.RefineAnswerPrompt(question, answer, JsonConvert.SerializeObject(filteredEvents)));
            var reply = await _chatCompletionService.GetChatMessageContentAsync(history);
            return reply.Content;
        }

    }
}
