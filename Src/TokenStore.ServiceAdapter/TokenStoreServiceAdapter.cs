using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using TheAssistant.Core;
using TheAssistant.Core.Authentication;

namespace TheAssistant.TokenStore.ServiceAdapter
{
    public class TokenStoreServiceAdapter : ITokenStoreServiceAdapter
    {
        private readonly ILogger<TokenStoreServiceAdapter> _logger;
        private readonly SecretClient _secretClient;

        public TokenStoreServiceAdapter(SecretClient secretClient, ILogger<TokenStoreServiceAdapter> logger)
        {
            _secretClient = secretClient ?? throw new ArgumentNullException(nameof(secretClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task ClearToken(string userId)
        {
            var name = GetSecretName(userId);

            try
            {
                await _secretClient.StartDeleteSecretAsync(name);
                _logger.LogInformation("Cleared tokens for user {UserId}", userId);
            }
            catch (Azure.RequestFailedException ex) when (ex.Status == 404)
            {
                _logger.LogWarning("Tokens for user {UserId} not found to clear", userId);
            }
        }

        public async Task<Token?> GetToken(string userId)
        {
            try
            {
                var name = GetSecretName(userId);
                var secret = await _secretClient.GetSecretAsync(name);

                return JsonConvert.DeserializeObject<Token>(secret.Value.Value);
            }
            catch (Azure.RequestFailedException ex) when (ex.Status == 404)
            {
                _logger.LogWarning("Tokens for user {UserId} not found", userId);
                return null;
            }
        }

        public async Task StoreToken(string userId, Token token)
        {
            var json = JsonConvert.SerializeObject(token);
            var name = GetSecretName(userId);

            var secret = new KeyVaultSecret(name, json)
            {
                Properties = { ContentType = "application/json", ExpiresOn = token.ExpiresAt }
            };

            await _secretClient.SetSecretAsync(secret);
            _logger.LogInformation("Stored tokens for user {UserId}", userId);
        }


        private static string GetSecretName(string userId) => $"token-{userId}";
    }
}
