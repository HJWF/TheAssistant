using Microsoft.Extensions.AI;

namespace TheAssistant.Agents.ServiceAdapter.MealPlan
{
    public class CouncilMember
    {
        private readonly IChatClient _chatClient;
        private readonly ITokenUsageTracker _tokenUsageTracker;
        private readonly string _systemPrompt;

        public string Role { get; }

        public CouncilMember(string role, string systemPrompt, IChatClient chatClient, ITokenUsageTracker tokenUsageTracker)
        {
            Role = role;
            _systemPrompt = systemPrompt;
            _chatClient = chatClient;
            _tokenUsageTracker = tokenUsageTracker;
        }

        public async Task<string> ContributeAsync(
            string topic,
            IReadOnlyList<(string Role, string Contribution)> priorContributions,
            CancellationToken cancellationToken)
        {
            var messages = new List<ChatMessage>
            {
                new(ChatRole.System, _systemPrompt)
            };

            if (priorContributions.Count > 0)
            {
                var transcript = string.Join("\n\n", priorContributions
                    .Select(c => $"[{c.Role}]:\n{c.Contribution}"));

                messages.Add(new(ChatRole.User,
                    $"Topic: {topic}\n\n" +
                    $"The other council members have shared their perspectives:\n\n{transcript}\n\n" +
                    $"Review their input and refine your position. You may build on their ideas or " +
                    $"respectfully challenge points that conflict with your expertise."));
            }
            else
            {
                messages.Add(new(ChatRole.User,
                    $"Topic: {topic}\n\n" +
                    $"You are the first to contribute. Share your detailed expert perspective."));
            }

            var response = await _chatClient.GetResponseAsync(messages, cancellationToken: cancellationToken);
            _tokenUsageTracker.Track(response.Usage);
            return response.Text ?? string.Empty;
        }
    }
}
