namespace TheAssistant.Core.Messaging
{
    public class SentMessage
    {
        public string Destination { get; set; } = string.Empty;
        public string DestinationNumber { get; set; } = string.Empty;
        public string DestinationUuid { get; set; } = string.Empty;
        public long Timestamp { get; set; }
        public string Message { get; set; } = string.Empty;
        public int ExpiresInSeconds { get; set; }
        public bool ViewOnce { get; set; }
    }
}
