using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using TheAssistant.Core.Infrastructure;
using TheAssistant.Core.Messaging.HandleQueuedMessage;

namespace TheAssistant.TheAssistantApi.Messaging.HandleQueuedMessage;

public class HandleQueuedMessage
{
    private readonly ILogger<HandleQueuedMessage> _logger;
    private readonly ICommandHandler<HandleQueuedMessageCommand> _handler;

    public HandleQueuedMessage(ILogger<HandleQueuedMessage> logger, ICommandHandler<HandleQueuedMessageCommand> commandHandler)
    {
        _logger = logger;
        _handler = commandHandler;
    }

    [Function(nameof(HandleQueuedMessage))]
    public async Task Run(
        [ServiceBusTrigger("receivedmessages", Connection = "ServiceBus")]
        ServiceBusReceivedMessage message,
        ServiceBusMessageActions messageActions)
    {
        _logger.LogInformation("Handeling queue message");

        await _handler.Handle(new HandleQueuedMessageCommand(message.Body.ToString()));
        
        await messageActions.CompleteMessageAsync(message);
    }
}