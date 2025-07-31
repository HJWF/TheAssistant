namespace TheAssistant.Agents.ServiceAdapter.Authentication
{
    public class LoginUrlProvider : ILoginUrlProvider
    {
        private readonly IOneTimeTokenStore _oneTimeTokenStore;
        private readonly string _baseLoginUrl = "https://a1515cd0e6b6.ngrok-free.app/api/login/Consumer/start";

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
