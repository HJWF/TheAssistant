using Microsoft.Extensions.Logging;
using TheAssistant.Core.Infrastructure;
using TheAssistant.Core.Messaging.HandleReceiveMessages;

namespace TheAssistant.Core.Messaging.HandleDailyOverview
{
    public class HandleDailyOverviewCommandHandler : ICommandHandler<HandleDailyOverviewCommand>
    {
        private readonly ILogger<HandleReceiveMessagesCommandHandler> _logger;
        private readonly IServiceBusServiceAdapter _serviceBusServiceAdapter;
        private const string ReceiveMessagesQueueName = "receivedmessages";
        private const string DailyOverviewQueueName = "Get an overview of all personal and work meetings. Also get me a overview of the weather separated by part of the day, also include perceived temperature.";

        public HandleDailyOverviewCommandHandler(ILogger<HandleReceiveMessagesCommandHandler> logger, IServiceBusServiceAdapter serviceBusServiceAdapter)
        {
            _logger = logger;
            _serviceBusServiceAdapter = serviceBusServiceAdapter;
        }

        public async Task Handle(HandleDailyOverviewCommand command)
        {
            _logger.LogInformation($"Processing message with timestamp: {command.TriggerTime}");
            await _serviceBusServiceAdapter.SendMessageAsync(ReceiveMessagesQueueName, DailyOverviewQueueName);
        }
    }
}
