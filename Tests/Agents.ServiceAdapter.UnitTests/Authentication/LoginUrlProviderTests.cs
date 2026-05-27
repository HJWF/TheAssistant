using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using TheAssistant.Agents.ServiceAdapter.Authentication;
using TheAssistant.Core;
using TheAssistant.Core.Infrastructure;

namespace TheAssistant.Agents.ServiceAdapter.UnitTests.Authentication;

public class LoginUrlProviderTests
{
    private readonly Mock<IOneTimeTokenStoreServiceAdapter> _tokenStoreMock;
    private readonly LoginUrlProvider _provider;
    private const string StartUri = "http://localhost/start";

    public LoginUrlProviderTests()
    {
        _tokenStoreMock = new Mock<IOneTimeTokenStoreServiceAdapter>(MockBehavior.Strict);

        var settings = Options.Create(new LoginSettings
        {
            Consumer = new ConsumerSettings
            {
                StartUri = StartUri,
                TenantId = "tenant",
                ClientId = "client",
                RedirectUri = "http://localhost",
                ClientSecret = "secret"
            }
        });

        _provider = new LoginUrlProvider(settings, _tokenStoreMock.Object);
    }

    [Fact]
    public void GetLoginUrlForUserShouldReturnUrlContainingStartUri()
    {
        _tokenStoreMock.Setup(x => x.StoreToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>()));

        var url = _provider.GetLoginUrlForUser("user@test.com");

        url.Should().StartWith(StartUri);
    }

    [Fact]
    public void GetLoginUrlForUserShouldContainTokenQueryParameter()
    {
        _tokenStoreMock.Setup(x => x.StoreToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>()));

        var url = _provider.GetLoginUrlForUser("user@test.com");

        url.Should().Contain("?token=");
    }

    [Fact]
    public void GetLoginUrlForUserShouldStoreTokenInStore()
    {
        string? capturedToken = null;
        string? capturedUserId = null;

        _tokenStoreMock.Setup(x => x.StoreToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>()))
            .Callback<string, string, TimeSpan>((token, userId, _) =>
            {
                capturedToken = token;
                capturedUserId = userId;
            });

        var url = _provider.GetLoginUrlForUser("user@test.com");

        capturedToken.Should().NotBeNullOrEmpty();
        capturedUserId.Should().Be("user@test.com");
        url.Should().Contain(capturedToken);
    }

    [Fact]
    public void GetLoginUrlForUserShouldUse15MinuteTtl()
    {
        TimeSpan capturedTtl = default;

        _tokenStoreMock.Setup(x => x.StoreToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>()))
            .Callback<string, string, TimeSpan>((_, _, ttl) => capturedTtl = ttl);

        _provider.GetLoginUrlForUser("user@test.com");

        capturedTtl.Should().Be(TimeSpan.FromMinutes(15));
    }

    [Fact]
    public void GetLoginUrlForUserShouldGenerateUniqueTokensForEachCall()
    {
        var tokens = new List<string>();

        _tokenStoreMock.Setup(x => x.StoreToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>()))
            .Callback<string, string, TimeSpan>((token, _, _) => tokens.Add(token));

        _provider.GetLoginUrlForUser("user@test.com");
        _provider.GetLoginUrlForUser("user@test.com");

        tokens.Should().HaveCount(2);
        tokens[0].Should().NotBe(tokens[1]);
    }
}
