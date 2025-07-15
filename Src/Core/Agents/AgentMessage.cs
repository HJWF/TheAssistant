namespace TheAssistant.Core.Agents
{
    public class AgentMessage
    {
        public string Sender { get; set; }  
        public string Receiver { get; set; } 
        public string Role { get; set; }
        public string Content { get; set; }
        public Dictionary<string, string>? Metadata { get; set; }
    }

}
