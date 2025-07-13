namespace TheAssistant.Agents.ServiceAdapter
{
    public interface IRouter
    {
        Task<string> RouteAsync(string message);
    }

}
