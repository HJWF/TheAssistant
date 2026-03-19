using FluentAssertions;
using Moq;
using TheAssistant.Core.State.StoreStateToken;

namespace TheAssistant.Core.UnitTests.State
{
    public class StoreStateTokenCommandHandlerTests
    {
        private readonly Mock<IOneTimeTokenStoreServiceAdapter> _storeMock;
        private readonly StoreStateTokenCommandHandler _handler;

        public StoreStateTokenCommandHandlerTests()
        {
            _storeMock = new Mock<IOneTimeTokenStoreServiceAdapter>(MockBehavior.Strict);
            _handler = new StoreStateTokenCommandHandler(_storeMock.Object);
        }

        [Fact]
        public async Task HandleShouldCallStoreTokenWithStateAndUserId()
        {
            var command = new StoreStateTokenCommand("state-abc", "user-123");
            _storeMock.Setup(x => x.StoreToken("state-abc", "user-123", It.IsAny<TimeSpan>())).Verifiable();

            await _handler.Handle(command);

            _storeMock.Verify(x => x.StoreToken("state-abc", "user-123", It.IsAny<TimeSpan>()), Times.Once);
        }

        [Fact]
        public async Task HandleShouldUse15MinuteTtl()
        {
            var command = new StoreStateTokenCommand("state-xyz", "user-456");
            TimeSpan capturedTtl = default;

            _storeMock.Setup(x => x.StoreToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>()))
                .Callback<string, string, TimeSpan>((_, _, ttl) => capturedTtl = ttl);

            await _handler.Handle(command);

            capturedTtl.Should().Be(TimeSpan.FromMinutes(15));
        }
    }
}
