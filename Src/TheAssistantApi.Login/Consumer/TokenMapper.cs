namespace TheAssistant.TheAssistantApi.Login.Consumer
{
    public static class TokenMapper
    {
        public static Core.Authentication.Token ToModel(this AzureTokenResponse source) => new(source.AccessToken, source.RefreshToken, DateTime.Now.AddSeconds(source.ExpiresIn));
    }
}
