using TheAssistant.Core.Authentication;
using TheAssistant.Core.Infrastructure;

namespace TheAssistant.Core.Messaging.HandleNewSignIn
{
    public record class HandleNewPersonalSignInCommand(Token Token, UserDetails User) : ICommand;
}
