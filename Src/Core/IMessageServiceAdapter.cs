using TheAssistant.Core.Messaging;

namespace TheAssistant.Core
{
    public interface IMessageServiceAdapter
    {
        Task<IEnumerable<SentMessage>> ReceiveMessagesAsync();
        Task<string> SendMessageAsync(Message message);
    }
}
