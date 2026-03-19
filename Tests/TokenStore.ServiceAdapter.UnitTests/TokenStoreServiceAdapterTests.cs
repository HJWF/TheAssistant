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

        [Fact]
        public async Task StoreTokenShouldSetSecretInKeyVault()
        {
            var client = new TestSecretClient();
            var logger = Mock.Of<ILogger<TokenStoreServiceAdapter>>();
            var adapter = new TokenStoreServiceAdapter(client, logger);
            var token = new Token("access", "refresh", DateTime.UtcNow.AddHours(1));

            await adapter.StoreToken("user1", token, "microsoftconsumer");

            client.LastSetSecret.Should().NotBeNull();
            client.LastSetSecret!.Name.Should().Be("token-user1-microsoftconsumer");
        }

        [Fact]
        public async Task StoreTokenShouldSerializeTokenAsJson()
        {
            var client = new TestSecretClient();
            var logger = Mock.Of<ILogger<TokenStoreServiceAdapter>>();
            var adapter = new TokenStoreServiceAdapter(client, logger);
            var token = new Token("my-access-token", "my-refresh-token", DateTime.UtcNow.AddHours(1));

            await adapter.StoreToken("user1", token, "type1");

            client.LastSetSecret!.Value.Should().Contain("my-access-token");
        }

        [Fact]
        public async Task ClearTokenShouldNotThrowWhenSecretNotFound()
        {
            var client = new TestSecretClient(404);
            var logger = Mock.Of<ILogger<TokenStoreServiceAdapter>>();
            var adapter = new TokenStoreServiceAdapter(client, logger);

            var act = async () => await adapter.ClearToken("user1", "type1");

            await act.Should().NotThrowAsync();
        }

        [Fact]
        public async Task ClearTokenShouldPropagateNonNotFoundExceptions()
        {
            var client = new TestSecretClient(500);
            var logger = Mock.Of<ILogger<TokenStoreServiceAdapter>>();
            var adapter = new TokenStoreServiceAdapter(client, logger);

            var act = async () => await adapter.ClearToken("user1", "type1");

            await act.Should().ThrowAsync<Azure.RequestFailedException>()
                .Where(ex => ex.Status == 500);
        }

        [Fact]
        public async Task ClearTokenShouldCompleteSuccessfullyWhenSecretExists()
        {
            var client = new TestSecretClient();
            var logger = Mock.Of<ILogger<TokenStoreServiceAdapter>>();
            var adapter = new TokenStoreServiceAdapter(client, logger);

            var act = async () => await adapter.ClearToken("user1", "type1");

            await act.Should().NotThrowAsync();
        }
    }
}
