using Microsoft.Extensions.Logging;
using TheAssistant.Core.Infrastructure;

namespace TheAssistant.Core.Messaging.HandleReceiveMessages
{
    public class HandleReceiveMessagesCommandHandler : ICommandHandler<HandleReceiveMessagesCommand>
    {
        private readonly IMessageServiceAdapter _messageServiceAdapter;
        private readonly ILogger<HandleReceiveMessagesCommandHandler> _logger;
        private readonly IServiceBusServiceAdapter _serviceBusServiceAdapter;
        private const string ReceiveMessagesQueueName = "receivedmessages";

        public HandleReceiveMessagesCommandHandler(IMessageServiceAdapter messageServiceAdapter, ILogger<HandleReceiveMessagesCommandHandler> logger, IServiceBusServiceAdapter serviceBusServiceAdapter)
        {
            _messageServiceAdapter = messageServiceAdapter;
            _logger = logger;
            _serviceBusServiceAdapter = serviceBusServiceAdapter;
        }

        public async Task Handle(HandleReceiveMessagesCommand command)
        {
            _logger.LogInformation("Retreiving incoming messages.");
            var messages = await _messageServiceAdapter.ReceiveMessagesAsync();

            if (!messages.Any())
            {
                _logger.LogInformation("No messages found");
            }

            foreach (var message in messages)
            {
                _logger.LogInformation($"Processing message with timestamp: {message.Timestamp}");
                await _serviceBusServiceAdapter.SendMessageAsync(ReceiveMessagesQueueName, message.Message);
            }
        }
    }
}
