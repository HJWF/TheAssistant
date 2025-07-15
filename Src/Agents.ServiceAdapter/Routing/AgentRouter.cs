using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Newtonsoft.Json;
using TheAssistant.Core.Agents;

namespace TheAssistant.Agents.ServiceAdapter.Routing
{
    public class AgentRouter : IAgentRouter
    {
        private readonly Kernel _kernel;
        private readonly ILogger<AgentRouter> _logger;
        private const string Prompt = """
            You are an A2A router. Given a user message, determine which agent(s) should receive it.
            Respond in valid JSON format with structured message objects:
            {
              "messages": [
                {
                  "sender": "router",
                  "receiver": "agenda-agent",
                  "role": "user",
                  "content": "..."
                }
              ]
            }
            Known agents: agenda-agent, weather-agent, content-agent, recipe-agent, dailyupdate-agent.
            """;

        public AgentRouter(Kernel kernel, ILogger<AgentRouter> logger)
        {
            _kernel = kernel;
            _logger = logger;
        }

        public async Task<List<AgentMessage>> RouteAsync(string input)
        {
            var chat = _kernel.GetRequiredService<IChatCompletionService>();

            var history = new ChatHistory();
            history.AddSystemMessage(Prompt);
            history.AddUserMessage(input);

            var result = await chat.GetChatMessageContentAsync(history);

            try
            {
                var routing = JsonConvert.DeserializeObject<AgentMessageResult>(result.Content);

                if (routing?.Messages is { Count: > 0 })
                {
                    return routing.Messages;
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
