using TheAssistant.Core.Infrastructure;

namespace TheAssistant.Core.State.ValidateStateToken
{
    public class ValidateStateTokenQueryHandler : IQueryHandler<ValidateStateTokenQuery, bool>
    {
        private readonly IOneTimeTokenStoreServiceAdapter _oneTimeTokenStoreServiceAdapter;

        public ValidateStateTokenQueryHandler(IOneTimeTokenStoreServiceAdapter oneTimeTokenStoreServiceAdapter)
        {
            _oneTimeTokenStoreServiceAdapter = oneTimeTokenStoreServiceAdapter;
        }

        public Task<bool> Handle(ValidateStateTokenQuery command)
        {
            var userId = _oneTimeTokenStoreServiceAdapter.GetUserIdForToken(command.State);

            return Task.FromResult(!string.IsNullOrEmpty(userId));
        }
    }
}
