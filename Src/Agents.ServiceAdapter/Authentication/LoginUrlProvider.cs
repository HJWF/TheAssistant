using Microsoft.Extensions.Options;
using TheAssistant.Core;
using TheAssistant.Core.Infrastructure;

namespace TheAssistant.Agents.ServiceAdapter.Authentication
{
    public class LoginUrlProvider : ILoginUrlProvider
    {
        private readonly IOneTimeTokenStoreServiceAdapter _oneTimeTokenStoreServiceAdapter;
        private readonly LoginSettings _loginSettings;

        public LoginUrlProvider(IOptions<LoginSettings> LoginSettings, IOneTimeTokenStoreServiceAdapter oneTimeTokenStoreServiceAdapter)
        {
            _loginSettings = LoginSettings.Value;
            _oneTimeTokenStoreServiceAdapter = oneTimeTokenStoreServiceAdapter;
        }

        public string GetLoginUrlForUser(string userId)
        {
            var token = Guid.NewGuid().ToString("N");
            _oneTimeTokenStoreServiceAdapter.StoreToken(token, userId, TimeSpan.FromMinutes(15));
            return $"{_loginSettings.Consumer.StartUri}?token={token}";
        }
    }

}
