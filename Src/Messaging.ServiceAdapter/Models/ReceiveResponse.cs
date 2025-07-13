namespace TheAssistant.Messaging.ServiceAdapter.Models
{
    public class ReceiveResponse
    {
        public Envelope Envelope { get; set; } = new();
        public string Account { get; set; } = string.Empty;
    }
}
