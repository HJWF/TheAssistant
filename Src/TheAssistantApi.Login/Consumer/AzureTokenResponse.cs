using Newtonsoft.Json;

namespace TheAssistant.TheAssistantApi.Login.Consumer
{
    public class AzureTokenResponse
    {
        [JsonProperty("access_token")]
        public string AccessToken { get; set; }

        [JsonProperty("token_type")]
        public string TokenType { get; set; }

        [JsonProperty("expires_in")]
        public int ExpiresIn { get; set; }

        [JsonProperty("scope")]
        public string Scope { get; set; }

        [JsonProperty("refresh_token")]
        public string RefreshToken { get; set; }

        [JsonProperty("id_token")]
        public string IdToken { get; set; }

        public AzureTokenResponse(string accessToken, string tokenType, int expiresIn, string scope, string refreshToken, string idToken)
        {
            AccessToken = accessToken;
            TokenType = tokenType;
            ExpiresIn = expiresIn;
            Scope = scope;
            RefreshToken = refreshToken;
            IdToken = idToken;
        }
    }

}
