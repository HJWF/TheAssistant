using System.ComponentModel;
using System.Text.Json;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using TheAssistant.Agents.ServiceAdapter.AI;
using TheAssistant.Core.Agents;

namespace TheAssistant.Agents.ServiceAdapter.DailyUpdate
{
    public class DailyUpdateAgent : IDailyUpdateAgent
    {
        private const string SystemPrompt = """
            You are a daily update assistant that coordinates with other agents to provide a comprehensive daily summary.
            
            IMPORTANT:
            - You MUST use the available tools to gather daily update information
            - Current date context: Today is {CurrentDate}
            - Your job is to orchestrate getting calendar and weather information
            - Be concise and organized
            
            Available tools:
            - GetDailyCalendarSummary: Gets today's calendar events
            - GetDailyWeatherSummary: Gets today's weather forecast
            
            Process:
            1. Call both tools to gather information
            2. Wait for results
            3. Present them in a clear, organized format
            4. Include both calendar and weather in your response
            """;
        private readonly IChatCompletionService _chat;
        private readonly ILogger<DailyUpdateAgent> _logger;
        public string Name => AgentConstants.Names.DailyUpdate;
        public DailyUpdateAgent(
            IChatCompletionService chat,
            ILogger<DailyUpdateAgent> logger)
        {
            _chat = chat;
            _logger = logger;
        }
        [Description("Gets today's calendar events summary")]
        public Task<string> GetDailyCalendarSummary()
        {
            return Task.FromResult(JsonSerializer.Serialize(new
            {
                agentRequest = "agenda-agent",
                message = "What are today's meetings?",
                purpose = "Daily update calendar summary"
            }));
        }
        [Description("Gets today's weather forecast summary")]
        public Task<string> GetDailyWeatherSummary()
        {
            return Task.FromResult(JsonSerializer.Serialize(new
            {
                agentRequest = "weather-agent",
                message = "What's the weather forecast for today?",
                purpose = "Daily update weather summary"
            }));
        }
        public async Task<IEnumerable<AgentMessage>> HandleAsync(
            AgentMessage message, 
            CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;
            var systemPrompt = SystemPrompt
                .Replace("{CurrentDate}", now.ToString("yyyy-MM-dd"));
            var history = new ChatHistory();
            history.AddSystemMessage(systemPrompt);
            history.AddUserMessage(message.Content);
            var tools = new List<AITool>
            {
                AIFunctionFactory.Create(GetDailyCalendarSummary),
                AIFunctionFactory.Create(GetDailyWeatherSummary)
            };
            try
            {
                var reply = await _chat.GetChatMessageContentAsync(
                    history,
                    new ChatOptions
                    {
                        Tools = tools
                    },
                    cancellationToken);

                var agentMessages = new List<AgentMessage>();
                if (reply.Content?.Contains("agentRequest") == true)
                {
                    agentMessages.Add(new AgentMessage(
                        message.User,
                        Name,
                        AgentConstants.Names.Agenda,
                        AgentConstants.Roles.User,
                        "What are today's meetings?",
                        new Dictionary<string, string> { { "replyTo", Name } }));
                    agentMessages.Add(new AgentMessage(
                        message.User,
                        Name,
                        AgentConstants.Names.Weather,
                        AgentConstants.Roles.User,
                        "What's the weather forecast for today?",
                        new Dictionary<string, string> { { "replyTo", Name } }));
                }
                else
                {
                    agentMessages.Add(new AgentMessage(
                        message.User,
                        Name,
                        AgentConstants.Roles.User,
                        AgentConstants.Roles.Agent,
                        reply.Content ?? AgentConstants.SorryMessage,
                        null));
                }
                return agentMessages;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling daily update request");
                
                return new List<AgentMessage>
                {
                    new(message.User, Name, AgentConstants.Names.Agenda, AgentConstants.Roles.User, 
                        "What are today's meetings?", 
                        new Dictionary<string, string> { { "replyTo", Name } }),
                    new(message.User, Name, AgentConstants.Names.Weather, AgentConstants.Roles.User, 
                        "What's the weather forecast for today?", 
                        new Dictionary<string, string> { { "replyTo", Name } })
                };
            }
        }
    }
}