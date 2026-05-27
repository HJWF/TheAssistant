using FluentAssertions;

namespace TheAssistant.InMemoryOneTimeTokenStore.ServiceAdapter.UnitTests;

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

    [Fact]
    public void GetUserIdForTokenShouldReturnNullAfterTokenExpires()
    {
        var adapter = new InMemoryOneTimeTokenStoreServiceAdapter();

        adapter.StoreToken("t-expire", "user-expire", TimeSpan.FromMilliseconds(1));
        Thread.Sleep(10);

        var result = adapter.GetUserIdForToken("t-expire");

        result.Should().BeNull();
    }

    [Fact]
    public void InvalidateTokenShouldPreventFurtherRetrieval()
    {
        var adapter = new InMemoryOneTimeTokenStoreServiceAdapter();

        adapter.StoreToken("t-invalidate", "user-invalidate", TimeSpan.FromMinutes(5));
        adapter.InvalidateToken("t-invalidate");

        var result = adapter.GetUserIdForToken("t-invalidate");

        result.Should().BeNull();
    }

    [Fact]
    public void InvalidateTokenShouldNotThrowForUnknownToken()
    {
        var adapter = new InMemoryOneTimeTokenStoreServiceAdapter();

        var act = () => adapter.InvalidateToken("non-existent");

        act.Should().NotThrow();
    }
}
