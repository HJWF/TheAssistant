using Azure.Messaging.ServiceBus;

namespace TheAssistant.ServiceBus.ServiceAdapter
{
    public interface IServiceBusClient
    {
        Task SendMessageAsync(string queueOrTopicName, string messageBody);
        Task<ServiceBusReceivedMessage?> ReceiveMessageAsync(string queueOrSubscriptionPath);
        Task CompleteMessageAsync(string queueOrSubscriptionPath, ServiceBusReceivedMessage message);
        Task DeadLetterMessageAsync(string queueOrSubscriptionPath, ServiceBusReceivedMessage message, string reason);
    }
}