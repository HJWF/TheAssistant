using FluentAssertions;
using Moq;
using TheAssistant.Core.State.InvalidateStateToken;

namespace TheAssistant.Core.UnitTests.State;

public class InvalidateStateTokenCommandHandlerTests
{
    private readonly Mock<IOneTimeTokenStoreServiceAdapter> _storeMock;
    private readonly InvalidateStateTokenCommandHandler _handler;

    public InvalidateStateTokenCommandHandlerTests()
    {
        _storeMock = new Mock<IOneTimeTokenStoreServiceAdapter>(MockBehavior.Strict);
        _handler = new InvalidateStateTokenCommandHandler(_storeMock.Object);
    }

    [Fact]
    public async Task HandleShouldCallInvalidateTokenWithState()
    {
        _storeMock.Setup(x => x.InvalidateToken("state-abc")).Verifiable();

        await _handler.Handle(new InvalidateStateTokenCommand("state-abc"));

        _storeMock.Verify(x => x.InvalidateToken("state-abc"), Times.Once);
    }

    [Fact]
    public async Task HandleShouldCompleteSuccessfully()
    {
        _storeMock.Setup(x => x.InvalidateToken(It.IsAny<string>()));

        var act = async () => await _handler.Handle(new InvalidateStateTokenCommand("state-xyz"));

        await act.Should().NotThrowAsync();
    }
}
