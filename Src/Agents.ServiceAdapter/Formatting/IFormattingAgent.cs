namespace TheAssistant.Agents.ServiceAdapter.Formatting
{
    public interface IFormattingAgent
    {
        public Task<string> HandleAsync(List<AgentResponse> agentResponses);
    }
}
