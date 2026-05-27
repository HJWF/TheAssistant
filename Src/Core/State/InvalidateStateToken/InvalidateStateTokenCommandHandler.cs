using TheAssistant.Core.Infrastructure;

namespace TheAssistant.Core.State.InvalidateStateToken;

public class InvalidateStateTokenCommandHandler : ICommandHandler<InvalidateStateTokenCommand>
{
    private readonly IOneTimeTokenStoreServiceAdapter _oneTimeTokenStoreServiceAdapter;

    public InvalidateStateTokenCommandHandler(IOneTimeTokenStoreServiceAdapter oneTimeTokenStoreServiceAdapter)
    {
        _oneTimeTokenStoreServiceAdapter = oneTimeTokenStoreServiceAdapter;
    }

    public Task Handle(InvalidateStateTokenCommand command)
    {
        _oneTimeTokenStoreServiceAdapter.InvalidateToken(command.State);

        return Task.CompletedTask;
    }
}
