namespace TheAssistant.Messaging.ServiceAdapter
{
    public static class MessageMapper
    {
        public static IEnumerable<Core.Messaging.SentMessage> ToModel(this IEnumerable<Models.SentMessage> sentMessages) => sentMessages.Where(m => m != null).Select(ToModel);

        public static Core.Messaging.SentMessage ToModel(this Models.SentMessage sentMessage) => new()
        {
            Destination = sentMessage.Destination,
            DestinationNumber = sentMessage.DestinationNumber,
            DestinationUuid = sentMessage.DestinationUuid,
            Timestamp = sentMessage.Timestamp,
            Message = sentMessage.Message,
            ExpiresInSeconds = sentMessage.ExpiresInSeconds,
            ViewOnce = sentMessage.ViewOnce
        };
    }
}
