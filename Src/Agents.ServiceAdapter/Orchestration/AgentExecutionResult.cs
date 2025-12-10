using TheAssistant.Core.Agents;

namespace TheAssistant.Agents.ServiceAdapter.Orchestration
{
    public class AgentExecutionResult
    {
        public string AgentName { get; set; } = string.Empty;
        public bool Success { get; set; }
        public List<AgentMessage> Messages { get; set; } = new();
        public string? ErrorMessage { get; set; }
        public bool IsCritical { get; set; } = false;
    }
}
