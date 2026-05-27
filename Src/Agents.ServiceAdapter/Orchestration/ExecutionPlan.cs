using TheAssistant.Core.Agents;

namespace TheAssistant.Agents.ServiceAdapter.Orchestration;

public class ExecutionPlan
{
    public List<AgentMessage> ParallelTasks { get; set; } = new();
    public List<AgentMessage> SequentialTasks { get; set; } = new();
}
