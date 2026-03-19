using Microsoft.Extensions.Logging;
using System.ComponentModel;
using System.Text.Json;
using Microsoft.Extensions.AI;
using TheAssistant.Agents.ServiceAdapter.Agenda.Events;
using TheAssistant.Agents.ServiceAdapter.Authentication;
using TheAssistant.Core;
using TheAssistant.Core.Agenda;
using TheAssistant.Core.Agents;
using TheAssistant.Core.Authentication;
using TheAssistant.Core.Infrastructure;

namespace TheAssistant.Agents.ServiceAdapter.Agenda
{
    public class AgendaAgent : IAgendaAgent
    {
        private const string SystemPrompt = """
            You are a calendar assistant with access to tools for retrieving calendar events and birthdays.
            
            IMPORTANT:
            - You MUST use the available tools to get calendar data. Do not make up events or information.
            - Current date context: Today is {CurrentDate}. When users ask for "today", "tomorrow", "this week", use the current date {CurrentYear}-{CurrentMonth}-{CurrentDay}.
            - When formatting events, include: time, subject, location (if available), and duration
            - For birthdays, include the person's name and age if available
            - Be concise and friendly
            
            Available tools:
            - GetTodaysEvents: Use for "today's meetings", "what's on my calendar today"
            - GetEventsForDate: Use for specific dates like "tomorrow", "next Monday", "December 25"
            - GetEventsForDateRange: Use for date ranges like "this week", "next week", "January 1-15"
            - GetBirthdaysForDate: Use for "birthdays today", "whose birthday is on [date]"
            
            Process:
            1. Call the appropriate tool based on the user's question
            2. Wait for the tool result
            3. Format the events/birthdays in a user-friendly way
            4. Include relevant details (time, subject, location)
            5. Be concise
            """;
        
        private readonly ITokenStoreServiceAdapter _tokenStoreServiceAdapter;
        private readonly ILoginUrlProvider _loginUrlProvider;
        private readonly ILogger<AgendaAgent> _logger;
        private readonly IEventService _eventService;
        private readonly IChatClient _chatClient;
        private readonly ITokenUsageTracker _tokenUsageTracker;

        private const string TokenType = "microsoftconsumer";
        public string Name => AgentConstants.Names.Agenda;
        public string Description => "For calendar events, meetings, scheduling, and birthdays.";
        
        private UserDetails? _currentUser;

        public AgendaAgent(
            IChatClient chatClient,
            ITokenStoreServiceAdapter tokenStoreServiceAdapter,
            ILoginUrlProvider loginUrlProvider,
            ILogger<AgendaAgent> logger,
            IEventService eventService,
            ITokenUsageTracker tokenUsageTracker)
        {
            _chatClient = chatClient;
            _tokenStoreServiceAdapter = tokenStoreServiceAdapter;
            _loginUrlProvider = loginUrlProvider;
            _logger = logger;
            _eventService = eventService;
            _tokenUsageTracker = tokenUsageTracker;
        }

        [Description("Gets calendar events for today")]
        public async Task<string> GetTodaysEvents()
        {
            var (success, errorMessage, token) = await ValidateAndGetToken();
            if (!success)
            {
                return JsonSerializer.Serialize(new { error = errorMessage });
            }

            try
            {
                var eventsJson = await _eventService.GetTodaysEvents(GetWorkEmail(), token!);
                return eventsJson;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching today's events");
                return JsonSerializer.Serialize(new { error = "Failed to fetch today's events" });
            }
        }

        [Description("Gets calendar events for a specific date")]
        public async Task<string> GetEventsForDate(
            [Description("Date in format YYYY-MM-DD")] string date)
        {
            if (!DateTime.TryParse(date, out var parsedDate))
            {
                return JsonSerializer.Serialize(new { error = "Invalid date format. Use YYYY-MM-DD" });
            }

            var (success, errorMessage, token) = await ValidateAndGetToken();
            if (!success)
            {
                return JsonSerializer.Serialize(new { error = errorMessage });
            }

            try
            {
                var eventsJson = await _eventService.GetMeetings(parsedDate.ToString("yyyy-MM-dd"), token!);
                return eventsJson;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching events for {Date}", date);
                return JsonSerializer.Serialize(new { error = $"Failed to fetch events for {date}" });
            }
        }

        [Description("Gets calendar events for a date range")]
        public async Task<string> GetEventsForDateRange(
            [Description("Start date in format YYYY-MM-DD")] string startDate,
            [Description("End date in format YYYY-MM-DD")] string endDate)
        {
            if (!DateTime.TryParse(startDate, out var start))
            {
                return JsonSerializer.Serialize(new { error = "Invalid start date format. Use YYYY-MM-DD" });
            }

            if (!DateTime.TryParse(endDate, out var end))
            {
                return JsonSerializer.Serialize(new { error = "Invalid end date format. Use YYYY-MM-DD" });
            }

            if (start > end)
            {
                return JsonSerializer.Serialize(new { error = "Start date must be before end date" });
            }

            var (success, errorMessage, token) = await ValidateAndGetToken();
            if (!success)
            {
                return JsonSerializer.Serialize(new { error = errorMessage });
            }

            try
            {
                var allEvents = new List<CalendarEvent>();
                var currentDate = start;

                while (currentDate <= end)
                {
                    var eventsJson = await _eventService.GetMeetings(currentDate.ToString("yyyy-MM-dd"), token!);
                    var events = JsonSerializer.Deserialize<IEnumerable<CalendarEvent>>(eventsJson);
                    if (events != null)
                    {
                        allEvents.AddRange(events);
                    }
                    currentDate = currentDate.AddDays(1);
                }

                return JsonSerializer.Serialize(allEvents);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching events for range {StartDate} to {EndDate}", startDate, endDate);
                return JsonSerializer.Serialize(new { error = $"Failed to fetch events for {startDate} to {endDate}" });
            }
        }

        [Description("Gets birthdays for a specific date")]
        public async Task<string> GetBirthdaysForDate(
            [Description("Date in format YYYY-MM-DD")] string date)
        {
            if (!DateTime.TryParse(date, out var parsedDate))
            {
                return JsonSerializer.Serialize(new { error = "Invalid date format. Use YYYY-MM-DD" });
            }

            var (success, errorMessage, token) = await ValidateAndGetToken();
            if (!success)
            {
                return JsonSerializer.Serialize(new { error = errorMessage });
            }

            try
            {
                var birthdaysJson = await _eventService.GetBirthdays(parsedDate.ToString("yyyy-MM-dd"), token!);
                return birthdaysJson;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching birthdays for {Date}", date);
                return JsonSerializer.Serialize(new { error = $"Failed to fetch birthdays for {date}" });
            }
        }

        public async Task<IEnumerable<AgentMessage>> HandleAsync(
            AgentMessage message, 
            CancellationToken cancellationToken = default)
        {
            if (message.User == null)
            {
                return [new(message.User!, Name, AgentConstants.Roles.User, AgentConstants.Roles.Agent, 
                    "User details are missing.", null)];
            }

            _currentUser = message.User;

            var now = DateTime.UtcNow;
            var systemPrompt = SystemPrompt
                .Replace("{CurrentDate}", now.ToString("yyyy-MM-dd"))
                .Replace("{CurrentYear}", now.Year.ToString())
                .Replace("{CurrentMonth}", now.Month.ToString("D2"))
                .Replace("{CurrentDay}", now.Day.ToString("D2"));

            var messages = new List<ChatMessage>
            {
                new(ChatRole.System, systemPrompt),
                new(ChatRole.User, message.Content)
            };

            var tools = new List<AITool>
            {
                AIFunctionFactory.Create(GetTodaysEvents),
                AIFunctionFactory.Create(GetEventsForDate),
                AIFunctionFactory.Create(GetEventsForDateRange),
                AIFunctionFactory.Create(GetBirthdaysForDate)
            };

            try
            {
                var response = await _chatClient.GetResponseAsync(
                    messages,
                    new ChatOptions { Tools = tools },
                    cancellationToken);

                _tokenUsageTracker.Track(response.Usage);

                return [new AgentMessage(
                    message.User,
                    Name,
                    AgentConstants.Roles.User,
                    AgentConstants.Roles.Agent,
                    !string.IsNullOrWhiteSpace(response.Text) ? response.Text : AgentConstants.SorryMessage,
                    null)];
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in HandleAsync for user {UserId}", message.User.PersonalMailTag);
                return [new(message.User, Name, AgentConstants.Roles.User, AgentConstants.Roles.Agent, 
                    "Sorry, I encountered an error while fetching your calendar.", null)];
            }
            finally
            {
                _currentUser = null;
            }
        }

        private async Task<(bool success, string? errorMessage, Token? token)> ValidateAndGetToken()
        {
            if (_currentUser is null)
            {
                return (false, "User context is missing.", null);
            }

            var userId = _currentUser.PersonalMailTag;
            if (string.IsNullOrEmpty(userId))
            {
                return (false, "User ID is missing.", null);
            }

            var token = await _tokenStoreServiceAdapter.GetToken(userId, TokenType);
            
            if (token == null || token.ExpiresAt <= DateTime.Now)
            {
                var loginUrl = _loginUrlProvider.GetLoginUrlForUser(userId);
                return (false, $"I need access to your calendar. Please log in: {loginUrl}", null);
            }

            return (true, null, token);
        }

        private string GetWorkEmail()
        {
            return _currentUser?.WorkMailTag ?? string.Empty;
        }
    }
}
