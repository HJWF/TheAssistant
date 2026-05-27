namespace TheAssistant.Core.Agents;

public interface IAgent
{
    string Name { get; }
    string Description { get; }
    Task<IEnumerable<AgentMessage>> HandleAsync(AgentMessage message, CancellationToken cancellationToken = default);
}
