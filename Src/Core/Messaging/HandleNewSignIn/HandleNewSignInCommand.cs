using TheAssistant.Core.Authentication;
using TheAssistant.Core.Infrastructure;

namespace TheAssistant.Core.Messaging.HandleNewSignIn
{
    public record class HandleNewSignInCommand(Token Token, string UserId) : ICommand;
}
