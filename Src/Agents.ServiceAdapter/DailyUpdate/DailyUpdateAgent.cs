using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using TheAssistant.Agents.ServiceAdapter.Agenda;
using TheAssistant.Agents.ServiceAdapter.Weather;
using TheAssistant.Core.Agents;

namespace TheAssistant.Agents.ServiceAdapter.DailyUpdate
{
    public class DailyUpdateAgent : IDailyUpdateAgent
    {
        private const string SystemPrompt = """
            You are a daily update assistant that provides a comprehensive daily summary.
            
            IMPORTANT:
            - You MUST use the available tools to gather daily update information
            - Current date context: Today is {CurrentDate}
            - Be concise and organized
            
            Available tools:
            - GetDailyCalendarSummary: Gets today's calendar events from the agenda agent
            - GetDailyWeatherSummary: Gets today's weather forecast from the weather agent
            
            Process:
            1. Call both tools to gather information
            2. Wait for results
            3. Present them in a clear, organized format
            """;

        private readonly IWeatherAgent _weatherAgent;
        private readonly IAgendaAgent _agendaAgent;
        private readonly IChatClient _chatClient;
        private readonly ILogger<DailyUpdateAgent> _logger;
        private readonly ITokenUsageTracker _tokenUsageTracker;

        public string Name => AgentConstants.Names.DailyUpdate;
        public string Description => "For a combined daily summary of calendar events and weather.";

        public DailyUpdateAgent(
            IWeatherAgent weatherAgent,
            IAgendaAgent agendaAgent,
            IChatClient chatClient,
            ILogger<DailyUpdateAgent> logger,
            ITokenUsageTracker tokenUsageTracker)
        {
            _weatherAgent = weatherAgent;
            _agendaAgent = agendaAgent;
            _chatClient = chatClient;
            _logger = logger;
            _tokenUsageTracker = tokenUsageTracker;
        }

        public async Task<IEnumerable<AgentMessage>> HandleAsync(
            AgentMessage message,
            CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;
            var systemPrompt = SystemPrompt.Replace("{CurrentDate}", now.ToString("yyyy-MM-dd"));

            var messages = new List<ChatMessage>
            {
                new(ChatRole.System, systemPrompt),
                new(ChatRole.User, message.Content)
            };

            var tools = new List<AITool>
            {
                AIFunctionFactory.Create(
                    async () => await GetDailyCalendarSummaryAsync(message, cancellationToken),
                    "GetDailyCalendarSummary",
                    "Gets today's calendar events from the agenda agent"),
                AIFunctionFactory.Create(
                    async () => await GetDailyWeatherSummaryAsync(message, cancellationToken),
                    "GetDailyWeatherSummary",
                    "Gets today's weather forecast from the weather agent"),
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
                _logger.LogError(ex, "Error handling daily update request");
                return [new AgentMessage(
                    message.User,
                    Name,
                    AgentConstants.Roles.User,
                    AgentConstants.Roles.Agent,
                    AgentConstants.SorryMessage,
                    null)];
            }
        }

        private async Task<string> GetDailyCalendarSummaryAsync(AgentMessage originalMessage, CancellationToken cancellationToken)
        {
            try
            {
                var agentMessage = new AgentMessage(
                    originalMessage.User, Name, AgentConstants.Names.Agenda,
                    AgentConstants.Roles.User, "What are today's meetings?", null);

                var responses = await _agendaAgent.HandleAsync(agentMessage, cancellationToken);
                return responses.FirstOrDefault()?.Content ?? "No calendar data available";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting calendar summary from agenda agent");
                return "Unable to retrieve calendar data";
            }
        }

        private async Task<string> GetDailyWeatherSummaryAsync(AgentMessage originalMessage, CancellationToken cancellationToken)
        {
            try
            {
                var agentMessage = new AgentMessage(
                    originalMessage.User, Name, AgentConstants.Names.Weather,
                    AgentConstants.Roles.User, "What's the weather forecast for today?", null);

                var responses = await _weatherAgent.HandleAsync(agentMessage, cancellationToken);
                return responses.FirstOrDefault()?.Content ?? "No weather data available";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting weather summary from weather agent");
                return "Unable to retrieve weather data";
            }
        }
    }
}