using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Newtonsoft.Json;

namespace TheAssistant.Agents.ServiceAdapter.Routing
{
    public class LlmRouter : IRouter
    {
        private readonly Kernel _kernel;
        private readonly ILogger<LlmRouter> _logger;

        public LlmRouter(Kernel kernel, ILogger<LlmRouter> logger)
        {
            _kernel = kernel;
            _logger = logger;
        }

        public async Task<List<AgentRoute>> RouteAsync(string input)
        {
            var chat = _kernel.GetRequiredService<IChatCompletionService>();

            var chatHistory = new ChatHistory();
            chatHistory.AddSystemMessage($$"""
                You are an intelligent router that receives user messages and decides which specialized agents should handle which part of the message.

                Agents:
                - agenda: Handles questions about the calendar
                - weather: Provides weather forecasts
                - content: Assists with creative writing
                - recipes: Suggests or manages recipes

                Respond only with raw JSON in this format:

                {
                  "routes": [
                    { "name": "agenda", "input": "..." },
                    { "name": "weather", "input": "..." }
                  ]
                }

                If no agent matches, return: { "routes": [] }

                Do not add any explanation, comments, or formatting.           
                """);
            chatHistory.AddUserMessage(input);

            var result = await chat.GetChatMessageContentAsync(chatHistory);
            _logger.LogInformation("Routing result: {Result}", result.Content);

            try
            {
                var routing = JsonConvert.DeserializeObject<AgentRoutingResult>(result.Content);

                if (routing?.Routes is { Count: > 0 })
                {
                    return routing.Routes;
                }

                _logger.LogInformation("No routes found in LLM output: {Content}", result.Content);

                return [];
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse routing result: {Content}", result.Content);
                return [];
            }
        }
    }

}
