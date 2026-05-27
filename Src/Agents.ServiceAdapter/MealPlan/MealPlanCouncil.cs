using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace TheAssistant.Agents.ServiceAdapter.MealPlan;

public class MealPlanCouncil
{
    private readonly IReadOnlyList<CouncilMember> _members;
    private readonly IChatClient _synthesizer;
    private readonly ILogger<MealPlanCouncil> _logger;
    private readonly ITokenUsageTracker _tokenUsageTracker;

    public MealPlanCouncil(
        IReadOnlyList<CouncilMember> members,
        IChatClient synthesizer,
        ILogger<MealPlanCouncil> logger,
        ITokenUsageTracker tokenUsageTracker)
    {
        _members = members;
        _synthesizer = synthesizer;
        _logger = logger;
        _tokenUsageTracker = tokenUsageTracker;
    }

    public async Task<string> DeliberateAsync(string topic, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Council deliberation started. Topic: {Topic}", topic);

        // Round 1 — each member proposes independently
        var proposals = new List<(string Role, string Contribution)>();
        foreach (var member in _members)
        {
            _logger.LogDebug("Council round 1: {Role} proposing", member.Role);
            var contribution = await member.ContributeAsync(topic, [], cancellationToken);
            proposals.Add((member.Role, contribution));
        }

        // Round 2 — each member reads all proposals and refines their position
        var refinements = new List<(string Role, string Contribution)>();
        foreach (var member in _members)
        {
            _logger.LogDebug("Council round 2: {Role} refining", member.Role);
            var contribution = await member.ContributeAsync(topic, proposals, cancellationToken);
            refinements.Add((member.Role, contribution));
        }

        _logger.LogInformation("Council deliberation complete. Synthesizing final plan");
        return await SynthesizeAsync(topic, refinements, cancellationToken);
    }

    private async Task<string> SynthesizeAsync(
        string topic,
        IReadOnlyList<(string Role, string Contribution)> refinements,
        CancellationToken cancellationToken)
    {
        var transcript = string.Join("\n\n", refinements
            .Select(r => $"[{r.Role}]:\n{r.Contribution}"));

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, """
                You are the synthesizer for a dinner planning council of Dutch experts.
                You receive the final positions of multiple expert council members
                and combine them into one cohesive, actionable week of dinners.

                Rules:
                - Resolve conflicts by finding the best practical middle ground
                - Respect all expert constraints (nutrition, fitness, family, taste)
                - Only include ingredients available at Dutch supermarkets
                - All quantities must use metric units (g, ml, kg, l). Never use cups, oz, lbs or tablespoons
                - Format output as a day-by-day plan: Monday through Sunday
                - Each day: one dinner (max one line)
                - Keep each line under 50 characters
                - No explanations, just the plan
                """),
            new(ChatRole.User, $"Topic: {topic}\n\nCouncil deliberation:\n\n{transcript}\n\nProduce the final dinner plan.")
        };

        var response = await _synthesizer.GetResponseAsync(messages, cancellationToken: cancellationToken);
        _tokenUsageTracker.Track(response.Usage);
        return !string.IsNullOrWhiteSpace(response.Text) ? response.Text : AgentConstants.SorryMessage;
    }
}
