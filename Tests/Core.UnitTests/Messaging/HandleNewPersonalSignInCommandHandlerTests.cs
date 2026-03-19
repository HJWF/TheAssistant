using FluentAssertions;
using Moq;
using TheAssistant.Core.Authentication;
using TheAssistant.Core.Infrastructure;
using TheAssistant.Core.Messaging.HandleNewSignIn;

namespace TheAssistant.Core.UnitTests.Messaging
{
    public class HandleNewPersonalSignInCommandHandlerTests
    {
        private readonly Mock<ITokenStoreServiceAdapter> _tokenStoreMock;
        private readonly Mock<IServiceBusServiceAdapter> _serviceBusMock;
        private readonly HandleNewPersonalSignInCommandHandler _handler;
        private readonly UserDetails _user;
        private readonly Token _token;

        public HandleNewPersonalSignInCommandHandlerTests()
        {
            _tokenStoreMock = new Mock<ITokenStoreServiceAdapter>(MockBehavior.Strict);
            _serviceBusMock = new Mock<IServiceBusServiceAdapter>(MockBehavior.Strict);
            _handler = new HandleNewPersonalSignInCommandHandler(_tokenStoreMock.Object, _serviceBusMock.Object);
            _user = new UserDetails("+31600000000", "user@test.com", "work@test.com");
            _token = new Token("access", "refresh", DateTime.UtcNow.AddHours(1));
        }

        [Fact]
        public async Task HandleShouldStoreTokenWithCorrectArguments()
        {
            var command = new HandleNewPersonalSignInCommand(_token, _user, "user@test.com", "microsoftconsumer");

            _tokenStoreMock.Setup(x => x.StoreToken("user@test.com", _token, "microsoftconsumer"))
                .Returns(Task.CompletedTask)
                .Verifiable();
            _serviceBusMock.Setup(x => x.SendMessageAsync(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            await _handler.Handle(command);

            _tokenStoreMock.Verify(x => x.StoreToken("user@test.com", _token, "microsoftconsumer"), Times.Once);
        }

        [Fact]
        public async Task HandleShouldSendSuccessMessageToServiceBusAfterStoringToken()
        {
            var command = new HandleNewPersonalSignInCommand(_token, _user, "user@test.com", "microsoftconsumer");
            var callOrder = new List<string>();

            _tokenStoreMock.Setup(x => x.StoreToken(It.IsAny<string>(), It.IsAny<Token>(), It.IsAny<string>()))
                .Callback(() => callOrder.Add("store"))
                .Returns(Task.CompletedTask);
            _serviceBusMock.Setup(x => x.SendMessageAsync(It.IsAny<string>(), It.IsAny<string>()))
                .Callback<string, string>((_, _) => callOrder.Add("send"))
                .Returns(Task.CompletedTask);

            await _handler.Handle(command);

            callOrder.Should().ContainInOrder("store", "send");
            _serviceBusMock.Verify(x => x.SendMessageAsync("receivedmessages", It.Is<string>(s => s.Contains("Logged in"))), Times.Once);
        }
    }
}
