using FluentAssertions;
using InMemoryOneTimeTokenStore.ServiceAdapter;

namespace TheAssistant.InMemoryOneTimeTokenStore.ServiceAdapter.UnitTests
{
    public class InMemoryOneTimeTokenStoreTests
    {
        [Fact]
        public void StoreTokenShouldAllowRetrievalWithinTtl()
        {
            var adapter = new InMemoryOneTimeTokenStoreServiceAdapter();

            adapter.StoreToken("t1", "user1", TimeSpan.FromMinutes(5));
            var userId = adapter.GetUserIdForToken("t1");

            userId.Should().Be("user1");
        }

        [Fact]
        public void GetUserIdForTokenShouldReturnNullForUnknownOrExpiredToken()
        {
            var adapter = new InMemoryOneTimeTokenStoreServiceAdapter();

            var result = adapter.GetUserIdForToken("unknown");

            result.Should().BeNull();
        }
    }
}
