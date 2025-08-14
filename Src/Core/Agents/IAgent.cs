namespace TheAssistant.Core.Agents
{
    public interface IAgent
    {
        string Name { get; }
        Task<IEnumerable<AgentMessage>> HandleAsync(AgentMessage message);
    }
}
