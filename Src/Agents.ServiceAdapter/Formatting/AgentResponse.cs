namespace TheAssistant.Agents.ServiceAdapter.Formatting
{
    public class AgentResponse
    {
        public string AgentName { get; set; }
        public string Content { get; set; }

        public AgentResponse(string agentName, string content)
        {
            AgentName = agentName;
            Content = content;
        }
    }
}
