using TheAssistant.Core.Infrastructure;

namespace TheAssistant.Core.State.StoreStateToken;

public class StoreStateTokenCommandHandler : ICommandHandler<StoreStateTokenCommand>
{
    private readonly IOneTimeTokenStoreServiceAdapter _oneTimeTokenStoreServiceAdapter;

    public StoreStateTokenCommandHandler(IOneTimeTokenStoreServiceAdapter oneTimeTokenStoreServiceAdapter)
    {
        _oneTimeTokenStoreServiceAdapter = oneTimeTokenStoreServiceAdapter;
    }

    public Task Handle(StoreStateTokenCommand command)
    {
        _oneTimeTokenStoreServiceAdapter.StoreToken(command.State, command.UserId, TimeSpan.FromMinutes(15));
    
        return Task.CompletedTask;
    }
}
