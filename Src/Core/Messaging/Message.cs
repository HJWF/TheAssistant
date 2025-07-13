namespace TheAssistant.Core.Messaging
{
    public class Message
    {
        public string To { get; set; }

        public string Content { get; set; }

        public Message(string content)
        {
            Content = content;
            To = string.Empty;
        }

        public Message(string content, string to)
        {
            Content = content;
            To = to;
        }
    }
}
