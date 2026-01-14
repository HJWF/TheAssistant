using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TheAssistant.Core.Infrastructure;
using TheAssistant.Core.Messaging.HandleQueuedMessage;
using TheAssistant.TheAssistantApi.Infrastructure;

namespace TheAssistant.TheAssistantApi.Messaging.HandleQueuedMessage;

public class HandleQueuedMessage
{
    private readonly ILogger<HandleQueuedMessage> _logger;
    private readonly ICommandHandler<HandleQueuedMessageCommand> _handler;
    private readonly UserDetailsSettings _userDetailsSettings;

    public HandleQueuedMessage(ILogger<HandleQueuedMessage> logger, ICommandHandler<HandleQueuedMessageCommand> commandHandler, IOptions<UserDetailsSettings> options)
    {
        _logger = logger;
        _handler = commandHandler;
        _userDetailsSettings = options.Value;
    }

    [Function(nameof(HandleQueuedMessage))]
    public async Task Run(
        [ServiceBusTrigger("receivedmessages", Connection = "ServiceBus")]
        ServiceBusReceivedMessage message)
    {
        _logger.LogInformation("Handling queue message: {MessageId}", message.MessageId);

        try
        {
            await _handler.Handle(new HandleQueuedMessageCommand(
                message.Body.ToString(), 
                new UserDetails(
                    _userDetailsSettings.PhoneNumber, 
                    _userDetailsSettings.PersonalMailTag, 
                    _userDetailsSettings.WorkMailTag)));

            _logger.LogInformation("Successfully handled queue message: {MessageId}", message.MessageId);           
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle queue message: {MessageId}", message.MessageId);
            throw;
        }
    }
}