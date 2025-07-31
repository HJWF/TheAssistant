namespace TheAssistant.Core.Agents
{
    public interface IAgent
    {
        //Task<string> HandleAsync(string input);

        string Name { get; }
        Task<IEnumerable<AgentMessage>> HandleAsync(AgentMessage message);
    }
}
