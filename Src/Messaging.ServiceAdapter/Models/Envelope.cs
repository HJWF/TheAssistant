namespace TheAssistant.Messaging.ServiceAdapter.Models
{
    public class Envelope
    {
        public string Source { get; set; } = string.Empty;
        public string SourceNumber { get; set; } = string.Empty;
        public string SourceUuid { get; set; } = string.Empty;
        public string SourceName { get; set; } = string.Empty;
        public int SourceDevice { get; set; }
        public long Timestamp { get; set; }
        public long ServerReceivedTimestamp { get; set; }
        public long ServerDeliveredTimestamp { get; set; }
        public SyncMessage SyncMessage { get; set; } = new();
    }
}
