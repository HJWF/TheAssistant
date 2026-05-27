using TheAssistant.Core.Infrastructure;

namespace TheAssistant.Core.State.StoreStateToken;

public record StoreStateTokenCommand(string State, string UserId) : ICommand;
