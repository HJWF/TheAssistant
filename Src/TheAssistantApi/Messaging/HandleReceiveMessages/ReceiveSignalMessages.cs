using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using TheAssistant.Core.Infrastructure;
using TheAssistant.Core.Messaging.HandleReceiveMessages;

namespace TheAssistant.TheAssistantApi.Messaging.HandleReceiveMessages
{
    public class ReceiveSignalMessages
    {
        private readonly ILogger<ReceiveSignalMessages> _logger;
        private readonly ICommandHandler<HandleReceiveMessagesCommand> _handler;

        public ReceiveSignalMessages(ILogger<ReceiveSignalMessages> logger, ICommandHandler<HandleReceiveMessagesCommand> handler)
        {
            _logger = logger;
            _handler = handler;
        }

        [Function(nameof(ReceiveSignalMessages))]
        public async Task Run([TimerTrigger("*/30 * * * * *", RunOnStartup = true)] TimerInfo myTimer)
        {
            await _handler.Handle(new HandleReceiveMessagesCommand());

            if (myTimer.ScheduleStatus is not null)
            {
                _logger.LogDebug("Next timer schedule at: {Next}", myTimer.ScheduleStatus.Next);
            }
        }
    }
}
