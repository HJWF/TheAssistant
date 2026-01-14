using System.ComponentModel;
using System.Text.Json;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using TheAssistant.Agents.ServiceAdapter.AI;
using TheAssistant.Core;
using TheAssistant.Core.Agents;

namespace TheAssistant.Agents.ServiceAdapter.AzureCosts
{
    public class AzureCostAgent : IAzureCostAgent
    {
        private const string SystemPrompt = """
            You are an Azure cost assistant with access to tools for retrieving cost data.
            
            IMPORTANT: 
            - You MUST use the available tools to get cost data. Do not make up numbers or provide information without calling the tools first.
            - Current date context: Today is {CurrentDate}. When users ask for "this year", "Q4", "this month", etc., use the current year {CurrentYear}.
            - Q1 = Jan-Mar, Q2 = Apr-Jun, Q3 = Jul-Sep, Q4 = Oct-Dec of the CURRENT year unless explicitly stated otherwise.
            
            Available tools:
            - GetCurrentMonthCosts: Use for "this month" or "current costs"
            - GetPreviousMonthCosts: Use for "last month" or "previous month"  
            - GetCostsForPeriod: Use for specific date ranges or quarters
            
            Process:
            1. Call the appropriate tool based on the user's question
            2. Wait for the tool result
            3. Format the cost data clearly with currency and 2 decimal places
            4. List top services with costs and percentages
            5. Be concise
            """;
            
        private readonly IAzureCostServiceAdapter _azureCostServiceAdapter;
        private readonly IChatCompletionService _chat;
        private readonly ILogger<AzureCostAgent> _logger;

        public string Name => AgentConstants.Names.AzureCost;

        public AzureCostAgent(
            IAzureCostServiceAdapter azureCostServiceAdapter, 
            IChatCompletionService chat,
            ILogger<AzureCostAgent> logger)
        {
            _azureCostServiceAdapter = azureCostServiceAdapter;
            _chat = chat;
            _logger = logger;
        }

        [Description("Gets Azure costs for the current month (month-to-date)")]
        public async Task<string> GetCurrentMonthCosts()
        {
            var costs = await _azureCostServiceAdapter.GetCurrentMonthCosts();
            return JsonSerializer.Serialize(costs);
        }

        [Description("Gets Azure costs for the previous month")]
        public async Task<string> GetPreviousMonthCosts()
        {
            var costs = await _azureCostServiceAdapter.GetPreviousMonthCosts();
            return JsonSerializer.Serialize(costs);
        }

        [Description("Gets Azure costs for a specific date range")]
        public async Task<string> GetCostsForPeriod(
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

            var twoYearsAgo = DateTime.UtcNow.AddYears(-2);
            if (end < twoYearsAgo)
            {
                return JsonSerializer.Serialize(new 
                { 
                    warning = $"Requested dates are from {start:yyyy-MM-dd} to {end:yyyy-MM-dd}, which is more than 2 years ago. Current year is {DateTime.UtcNow.Year}. Did you mean a more recent period?",
                    totalCost = 0,
                    currency = "EUR"
                });
            }

            var costs = await _azureCostServiceAdapter.GetCostsForPeriod(start, end);
            return JsonSerializer.Serialize(costs);
        }

        public async Task<IEnumerable<AgentMessage>> HandleAsync(
            AgentMessage message, 
            CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;
            var systemPrompt = SystemPrompt
                .Replace("{CurrentDate}", now.ToString("yyyy-MM-dd"))
                .Replace("{CurrentYear}", now.Year.ToString());

            var history = new ChatHistory();
            history.AddSystemMessage(systemPrompt);
            history.AddUserMessage(message.Content);

            var tools = new List<AITool>
            {
                AIFunctionFactory.Create(GetCurrentMonthCosts),
                AIFunctionFactory.Create(GetPreviousMonthCosts),
                AIFunctionFactory.Create(GetCostsForPeriod)
            };

            var reply = await _chat.GetChatMessageContentAsync(
                history, 
                new ChatOptions 
                { 
                    Tools = tools
                },
                cancellationToken);

            return new List<AgentMessage> {
                new AgentMessage(
                    message.User,
                    Name,
                    AgentConstants.Roles.User,
                    AgentConstants.Roles.Agent,
                    reply.Content ?? AgentConstants.SorryMessage,
                    null)
            };
        }
    }
}
