namespace TheAssistant.Agents.ServiceAdapter.Routing
{
    public record AgentRoutingResult
    {
        public List<AgentRoute> Routes { get; init; } = new();
    }
}
