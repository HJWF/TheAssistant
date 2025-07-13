namespace TheAssistant.Agents.ServiceAdapter.Routing
{
    public interface IRouter
    {
        Task<List<AgentRoute>> RouteAsync(string message);
    }

}
