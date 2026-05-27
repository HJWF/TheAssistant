using TheAssistant.Core.Authentication;
using TheAssistant.Core.Infrastructure;

namespace TheAssistant.Core.Messaging.HandleNewSignIn;

public record HandleNewPersonalSignInCommand(Token Token, UserDetails User, string UserId, string Type) : ICommand;
