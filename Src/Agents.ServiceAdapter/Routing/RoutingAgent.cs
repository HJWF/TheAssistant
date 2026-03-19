using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using TheAssistant.Core.Agents;
using TheAssistant.Core.Infrastructure;

namespace TheAssistant.Agents.ServiceAdapter.Routing
{
    public class RoutingAgent : IRoutingAgent
    {
        private readonly IChatClient _chatClient;
        private readonly ILogger<RoutingAgent> _logger;
        private readonly string _prompt;

        public RoutingAgent(IEnumerable<IAgent> agents, IChatClient chatClient, ILogger<RoutingAgent> logger)
        {
            _chatClient = chatClient;
            _logger = logger;
            _prompt = BuildPrompt(agents);
        }

        public async Task<List<AgentMessage>> RouteAsync(string input, UserDetails user, CancellationToken cancellationToken = default)
        {
            var userJson = JsonConvert.SerializeObject(user);

            var messages = new List<ChatMessage>
            {
                new(ChatRole.System, _prompt),
                new(ChatRole.System, $"User: {userJson}"),
                new(ChatRole.User, input)
            };

            var result = await _chatClient.GetResponseAsync(messages, cancellationToken: cancellationToken);

            if(result == null || string.IsNullOrWhiteSpace(result.Text))
            {
                _logger.LogWarning("LLM returned empty or null content for routing: {Input}", input);
                return [];
            }

            try
            {
                var routing = JsonConvert.DeserializeObject<AgentMessageResult>(result.Text!);

                if (routing?.Messages is { Count: > 0 })
                {
                    return routing.Messages;
                }

                _logger.LogInformation("No routes found in LLM output: {Content}", result.Text);

                return [];
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse routing result: {Content}", result.Text);
                return [];
            }
        }

        private static string BuildPrompt(IEnumerable<IAgent> agents)
        {
            var agentList = string.Join("\n", agents.Select(a => $"- {a.Name}: {a.Description}"));

            return $$"""
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
                {{agentList}}

                Choose agents based only on the message content.

                Never include explanation, comments, or extra text outside the JSON.

                Input:

                user: object with phoneNumber, personalMailTag, workMailTag.

                message: string from the user.

                Output:

                A messages array with one or more routed message objects.
                """;
        }
    }

}
