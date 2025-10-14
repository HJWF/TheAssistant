using TheAssistant.Core.Infrastructure;

namespace TheAssistant.Core.State.ValidateStateToken
{
    public record ValidateStateTokenQuery(string State) : IQuery<bool>;
}
