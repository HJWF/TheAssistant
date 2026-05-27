using FluentAssertions;
using Moq;
using TheAssistant.Core.State.ValidateStateToken;

namespace TheAssistant.Core.UnitTests.State;

public class ValidateStateTokenQueryHandlerTests
{
    private readonly Mock<IOneTimeTokenStoreServiceAdapter> _storeMock;
    private readonly ValidateStateTokenQueryHandler _handler;

    public ValidateStateTokenQueryHandlerTests()
    {
        _storeMock = new Mock<IOneTimeTokenStoreServiceAdapter>(MockBehavior.Strict);
        _handler = new ValidateStateTokenQueryHandler(_storeMock.Object);
    }

    [Fact]
    public async Task HandleShouldReturnTrueWhenTokenIsValid()
    {
        _storeMock.Setup(x => x.GetUserIdForToken("valid-state")).Returns("user-123");

        var result = await _handler.Handle(new ValidateStateTokenQuery("valid-state"));

        result.Should().BeTrue();
    }

    [Fact]
    public async Task HandleShouldReturnFalseWhenTokenIsUnknown()
    {
        _storeMock.Setup(x => x.GetUserIdForToken("unknown-state")).Returns((string?)null);

        var result = await _handler.Handle(new ValidateStateTokenQuery("unknown-state"));

        result.Should().BeFalse();
    }

    [Fact]
    public async Task HandleShouldReturnFalseWhenTokenReturnsEmptyString()
    {
        _storeMock.Setup(x => x.GetUserIdForToken("empty-state")).Returns(string.Empty);

        var result = await _handler.Handle(new ValidateStateTokenQuery("empty-state"));

        result.Should().BeFalse();
    }
}
