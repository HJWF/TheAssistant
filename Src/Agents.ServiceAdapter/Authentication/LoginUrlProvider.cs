namespace TheAssistant.Agents.ServiceAdapter.Authentication
{
    public class LoginUrlProvider : ILoginUrlProvider
    {
        private readonly IOneTimeTokenStore _oneTimeTokenStore;
        private readonly string _baseLoginUrl = "https://79c665ef9174.ngrok-free.app/api/login/Consumer/start"; //TODO: This should be configured via appsettings or environment variable

        public LoginUrlProvider(IOneTimeTokenStore oneTimeTokenStore)
        {
            _oneTimeTokenStore = oneTimeTokenStore;
        }

        public string GetLoginUrlForUser(string userId)
        {
            var token = Guid.NewGuid().ToString("N");
            _oneTimeTokenStore.StoreToken(token, userId, TimeSpan.FromMinutes(15));
            return $"{_baseLoginUrl}?token={token}";
        }
    }

}
