using Microsoft.Extensions.Logging;
using TheAssistant.Core.Agents;

namespace TheAssistant.Agents.ServiceAdapter.MealPlan;

public class MealPlanAgent : IMealPlanAgent
{
    private readonly MealPlanCouncil _council;
    private readonly ILogger<MealPlanAgent> _logger;

    public string Name => AgentConstants.Names.MealPlan;
    public string Description => "For creating a weekly dinner plan via a council of nutrition, fitness, family, and culinary experts.";

    public MealPlanAgent(MealPlanCouncil council, ILogger<MealPlanAgent> logger)
    {
        _council = council;
        _logger = logger;
    }

    public async Task<IEnumerable<AgentMessage>> HandleAsync(
        AgentMessage message,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("MealPlanAgent received request from {Sender}", message.Sender);

        var result = await _council.DeliberateAsync(message.Content, cancellationToken);

        return
        [
            new AgentMessage(
                message.User,
                Name,
                message.Sender,
                AgentConstants.Roles.Agent,
                result,
                null)
        ];
    }
}
