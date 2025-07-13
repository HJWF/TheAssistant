
namespace TheAssistant.Core
{
    public interface IServiceBusServiceAdapter
    {
        Task SendMessageAsync(string queueOrTopicName, string messageBody);
    }
}
