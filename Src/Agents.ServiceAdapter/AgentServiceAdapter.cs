using Microsoft.Extensions.Logging;
using TheAssistant.Agents.ServiceAdapter.Orchestration;
using TheAssistant.Core;
using TheAssistant.Core.Infrastructure;

namespace TheAssistant.Agents.ServiceAdapter;

public class AgentServiceAdapter : IAgentServiceAdapter
{
    private readonly AgentOrchestrator _orchestrator;
    private readonly ILogger<AgentServiceAdapter> _logger;
    private readonly ITokenUsageTracker _tokenUsageTracker;

    public AgentServiceAdapter(
        AgentOrchestrator orchestrator,
        ITokenUsageTracker tokenUsageTracker,
        ILogger<AgentServiceAdapter> logger)
    {
        _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
        _tokenUsageTracker = tokenUsageTracker ?? throw new ArgumentNullException(nameof(tokenUsageTracker));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<string> HandleMessageAsync(string userInput, UserDetails user)
    {
        if (user == null)
        {
            _logger.LogWarning("HandleMessageAsync called with null user");
            throw new ArgumentNullException(nameof(user), "User cannot be null.");
        }

        if (string.IsNullOrWhiteSpace(userInput))
        {
            _logger.LogWarning("HandleMessageAsync called with empty input for user {UserId}", user.PersonalMailTag);
            throw new ArgumentException("Input message cannot be null or empty.", nameof(userInput));
        }

        try
        {
            _tokenUsageTracker.Initialize();

            _logger.LogInformation(
                "Handling message for user {UserId}: {MessagePreview}",
                user.PersonalMailTag,
                userInput.Length > 50 ? userInput.Substring(0, 50) + "..." : userInput);

            var response = await _orchestrator.ExecuteAsync(
                userInput,
                user,
                CancellationToken.None);

            _logger.LogInformation("Successfully handled message for user {UserId}", user.PersonalMailTag);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle message for user {UserId}", user.PersonalMailTag);
            return AgentConstants.SorryMessage;
        }
    }
}
