namespace TheAssistant.Core.Agents
{
    public class AgentMessage
    {
        public string UserId { get; set; }  
        public string Sender { get; set; }  
        public string Receiver { get; set; } 
        public string Role { get; set; }
        public string Content { get; set; }
        public Dictionary<string, string>? Metadata { get; set; }

        public AgentMessage(string userId, string sender, string receiver, string role, string content, Dictionary<string, string>? metadata)
        {
            UserId = userId;
            Sender = sender;
            Receiver = receiver;
            Role = role;
            Content = content;
            Metadata = metadata;
        }
    }

}
