using TheAssistant.Core;

namespace TheAssistant.ServiceBus.ServiceAdapter
{
    public class ServiceBusServiceAdapter : IServiceBusServiceAdapter
    {
        private readonly IServiceBusClient _client;

        public ServiceBusServiceAdapter(IServiceBusClient client)
        {
            _client = client;
        }

        public async Task SendMessageAsync(string queueOrTopicName, string messageBody) => await _client.SendMessageAsync(queueOrTopicName, messageBody);
    }
}
