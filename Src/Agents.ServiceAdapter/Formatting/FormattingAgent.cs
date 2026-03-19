using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace TheAssistant.Agents.ServiceAdapter.Formatting
{
    public class FormattingAgent : IFormattingAgent
    {
        private readonly IChatClient _chatClient;
        private readonly ITokenUsageTracker _tokenUsageTracker;
        private readonly TokenCostSettings _costSettings;

        public static string Name => AgentConstants.Names.Formatting;

        public FormattingAgent(IChatClient chatClient, ITokenUsageTracker tokenUsageTracker, IOptions<AgentsSettings> settings)
        {
            _chatClient = chatClient;
            _tokenUsageTracker = tokenUsageTracker;
            _costSettings = settings.Value.TokenCost;
        }

        public async Task<string> HandleAsync(List<AgentResponse> agentResponses, CancellationToken cancellationToken = default)
        {
            if (agentResponses == null || agentResponses.Count == 0)
            {
                return AgentConstants.SorryMessage;
            }

            string content;

            if (agentResponses.Count == 1)
            {
                content = agentResponses[0].Content ?? AgentConstants.SorryMessage;
            }
            else
            {
                var messages = new List<ChatMessage>
                {
                    new(ChatRole.System, """
                        You are a smart formatter.

                        You receive a list of messages from different assistant agents.

                        Your task is to combine these into one clean, user-friendly message.

                        Follow these rules:

                        1. Use clear section headers or emojis to group content by topic.
                        2. Do not repeat agent names in each sentence.
                        3. Only include relevant, useful content from the messages.
                        4. Keep line length under 50 characters.
                        5. Avoid using vertical bars (|), quotes, or bullets.
                        6. Use this format for calendar items:  
                           08:30–09:30 Standup – Teams (john@company.com)
                        7. Do not add any explanation, intros, or summaries.
                        8. Output must be concise, clean, and readable on a phone.
                    """),
                    new(ChatRole.User, JsonConvert.SerializeObject(agentResponses))
                };

                var response = await _chatClient.GetResponseAsync(messages, cancellationToken: cancellationToken);
                _tokenUsageTracker.Track(response.Usage);
                content = !string.IsNullOrWhiteSpace(response.Text) ? response.Text : AgentConstants.SorryMessage;
            }

            return AppendTokenSummary(content);
        }

        private string AppendTokenSummary(string content)
        {
            var (inputTokens, outputTokens) = _tokenUsageTracker.GetTotals();
            if (inputTokens == 0 && outputTokens == 0) return content;

            var costEur = CalculateCostEur(inputTokens, outputTokens);
            return $"{content}\n\n🪙 {inputTokens:N0} in / {outputTokens:N0} out ≈ €{costEur:F4}";
        }

        private decimal CalculateCostEur(long inputTokens, long outputTokens)
        {
            var inputCostUsd = (inputTokens / 1_000_000m) * _costSettings.InputPricePerMillionTokens;
            var outputCostUsd = (outputTokens / 1_000_000m) * _costSettings.OutputPricePerMillionTokens;
            return (inputCostUsd + outputCostUsd) * _costSettings.UsdToEurRate;
        }
    }
}
