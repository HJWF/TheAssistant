using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using TheAssistant.Core.Agents;
using TheAssistant.Core.Infrastructure;
using TheAssistant.Agents.ServiceAdapter.AI;

namespace TheAssistant.Agents.ServiceAdapter.Routing
{
    public class RoutingAgent : IRoutingAgent
    {
        private readonly IChatCompletionService _chatCompletionService;
        private readonly ILogger<RoutingAgent> _logger;
        private const string Prompt = """
            You are an A2A router. Based on the user message and user context, decide which agent(s) the message should be routed to.

            Respond with a JSON object using this format:
            {
              "messages": [
                {
                  "user": {
                    "phoneNumber": "string",
                    "personalMailTag": "string",
                    "workMailTag": "string"
                  },
                  "sender": "router",
                  "receiver": "agent-name",
                  "role": "user",
                  "content": "message content"
                }
              ]
            }
            
            Requirements:

            Use only valid JSON.

            Use the provided user object in each message.

            Route to one or more of these known agents:
            agenda-agent, weather-agent, azurecost-agent, content-agent, recipe-agent, dailyupdate-agent.

            Choose agents based only on the message content.

            Never include explanation, comments, or extra text outside the JSON.

            Input:

            user: object with phoneNumber, personalMailTag, workMailTag.

            message: string from the user.

            Output:

            A messages array with one or more routed message objects.
            """;

        public RoutingAgent(IChatCompletionService chatCompletionService, ILogger<RoutingAgent> logger)
        {
            _chatCompletionService = chatCompletionService;
            _logger = logger;
        }

        public async Task<List<AgentMessage>> RouteAsync(string input, UserDetails user, CancellationToken cancellationToken = default)
        {
            var userJson = JsonConvert.SerializeObject(user);

            var history = new ChatHistory();
            history.AddSystemMessage(Prompt);
            history.AddSystemMessage($"User: {userJson}");
            history.AddUserMessage(input);

            var result = await _chatCompletionService.GetChatMessageContentAsync(history, cancellationToken: cancellationToken);

            if(result == null || string.IsNullOrWhiteSpace(result.Content))
            {
                _logger.LogWarning("LLM returned empty or null content for routing: {Input}", input);
                return [];
            }

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
