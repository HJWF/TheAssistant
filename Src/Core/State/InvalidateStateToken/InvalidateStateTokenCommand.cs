
using TheAssistant.Core.Infrastructure;

namespace TheAssistant.Core.State.InvalidateStateToken
{
    public record InvalidateStateTokenCommand(string State): ICommand;
}
