using TheAssistant.Core.Infrastructure;

namespace TheAssistant.Core.Agents
{
    public class AgentMessage
    {
        public UserDetails User { get; set; }  
        public string Sender { get; set; }  
        public string Receiver { get; set; } 
        public string Role { get; set; }
        public string Content { get; set; }
        public Dictionary<string, string>? Metadata { get; set; }

        public AgentMessage(UserDetails user, string sender, string receiver, string role, string content, Dictionary<string, string>? metadata)
        {
            User = user;
            Sender = sender;
            Receiver = receiver;
            Role = role;
            Content = content;
            Metadata = metadata;
        }
    }

}
