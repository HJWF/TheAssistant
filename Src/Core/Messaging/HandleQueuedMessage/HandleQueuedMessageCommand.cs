using TheAssistant.Core.Infrastructure;

namespace TheAssistant.Core.Messaging.HandleQueuedMessage
{
    public record HandleQueuedMessageCommand(string Message, UserDetails User) : ICommand;
}
