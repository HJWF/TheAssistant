namespace TheAssistant.Agents.ServiceAdapter.Routing
{
    public record RoutingAgentResult
    {
        public List<AgentRoute> Routes { get; init; } = new();
    }
}
