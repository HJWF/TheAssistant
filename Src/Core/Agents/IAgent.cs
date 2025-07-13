namespace TheAssistant.Core.Agents
{
    public interface IAgent
    {
        Task<string> HandleAsync(string input);
    }
}
