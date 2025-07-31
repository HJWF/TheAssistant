using Azure.Core;
using Azure.Messaging.ServiceBus;

namespace TheAssistant.ServiceBus.ServiceAdapter
{
    public class ServiceBusClient : IServiceBusClient, IAsyncDisposable
    {
        private readonly Azure.Messaging.ServiceBus.ServiceBusClient _client;

        public ServiceBusClient(TokenCredential credential, string fullyQualifiedNamespace)
        {
            _client = new Azure.Messaging.ServiceBus.ServiceBusClient(fullyQualifiedNamespace, credential);
        }
        public async Task SendMessageAsync(string queueOrTopicName, string messageBody)
        {
            var sender = _client.CreateSender(queueOrTopicName);
            var message = new ServiceBusMessage(messageBody);
            await sender.SendMessageAsync(message);
        }

        public async Task<ServiceBusReceivedMessage?> ReceiveMessageAsync(string queueOrSubscriptionPath)
        {
            var receiver = _client.CreateReceiver(queueOrSubscriptionPath);
            var message = await receiver.ReceiveMessageAsync(TimeSpan.FromSeconds(10));
            return message;
        }

        public async Task CompleteMessageAsync(string queueOrSubscriptionPath, ServiceBusReceivedMessage message)
        {
            var receiver = _client.CreateReceiver(queueOrSubscriptionPath);
            await receiver.CompleteMessageAsync(message);
        }

        public async Task DeadLetterMessageAsync(string queueOrSubscriptionPath, ServiceBusReceivedMessage message, string reason)
        {
            var receiver = _client.CreateReceiver(queueOrSubscriptionPath);
            await receiver.DeadLetterMessageAsync(message, reason);
        }

        public async ValueTask DisposeAsync() => await _client.DisposeAsync();
    }
}
