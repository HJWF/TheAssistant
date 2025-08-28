using Microsoft.Extensions.Options;
using TheAssistant.Core.Infrastructure;

namespace TheAssistant.Agents.ServiceAdapter.Authentication
{
    public class LoginUrlProvider : ILoginUrlProvider
    {
        private readonly IOneTimeTokenStore _oneTimeTokenStore;
        private readonly LoginSettings _loginSettings;
        //private readonly string _baseLoginUrl = "https://6bd30e14d632.ngrok-free.app/api/login/Consumer/start"; //TODO: This should be configured via appsettings or environment variable

        public LoginUrlProvider(IOneTimeTokenStore oneTimeTokenStore, IOptions<LoginSettings> LoginSettings)
        {
            _oneTimeTokenStore = oneTimeTokenStore;
            _loginSettings = LoginSettings.Value;
        }

        public string GetLoginUrlForUser(string userId)
        {
            var token = Guid.NewGuid().ToString("N");
            _oneTimeTokenStore.StoreToken(token, userId, TimeSpan.FromMinutes(15));
            return $"{_loginSettings.Consumer.StartUri}?token={token}";
        }
    }

}
