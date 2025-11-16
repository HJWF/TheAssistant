using Azure;
using Azure.Security.KeyVault.Secrets;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using TheAssistant.Core.Authentication;

namespace TheAssistant.TokenStore.ServiceAdapter.UnitTests
{
    public class TokenStoreServiceAdapterTests
    {

        [Fact]
        public async Task GetTokenShouldReturnTokenWhenSecretExists()
        {
            var secret = new KeyVaultSecret("token-user-type", JsonConvert.SerializeObject(new Token("a","b", DateTime.UtcNow.AddHours(1))));
            var response = Response.FromValue(secret, null);
            var client = new TestSecretClient(response);

            var logger = Mock.Of<ILogger<TokenStoreServiceAdapter>>();
            var adapter = new TokenStoreServiceAdapter(client, logger);

            var result = await adapter.GetToken("user","type");

            result.Should().NotBeNull();
        }

        [Fact]
        public async Task GetTokenShouldReturnNullWhenSecretNotFound()
        {
            var client = new TestSecretClient(true);

            var logger = Mock.Of<ILogger<TokenStoreServiceAdapter>>();
            var adapter = new TokenStoreServiceAdapter(client, logger);

            var result = await adapter.GetToken("user","type");

            result.Should().BeNull();
        }
    }
}
