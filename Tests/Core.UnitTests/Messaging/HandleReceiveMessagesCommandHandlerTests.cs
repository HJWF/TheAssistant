using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using TheAssistant.Core.Messaging;
using TheAssistant.Core.Messaging.HandleReceiveMessages;

namespace TheAssistant.Core.UnitTests.Messaging
{
    public class HandleReceiveMessagesCommandHandlerTests
    {
        private readonly Mock<IMessageServiceAdapter> _messageMock;
        private readonly Mock<IServiceBusServiceAdapter> _serviceBusMock;
        private readonly HandleReceiveMessagesCommandHandler _handler;

        public HandleReceiveMessagesCommandHandlerTests()
        {
            _messageMock = new Mock<IMessageServiceAdapter>(MockBehavior.Strict);
            _serviceBusMock = new Mock<IServiceBusServiceAdapter>(MockBehavior.Strict);
            var logger = new Mock<ILogger<HandleReceiveMessagesCommandHandler>>().Object;
            _handler = new HandleReceiveMessagesCommandHandler(_messageMock.Object, logger, _serviceBusMock.Object);
        }

        [Fact]
        public async Task HandleShouldCallReceiveMessagesAsync()
        {
            _messageMock.Setup(x => x.ReceiveMessagesAsync()).ReturnsAsync(Enumerable.Empty<SentMessage>());

            await _handler.Handle(new HandleReceiveMessagesCommand());

            _messageMock.Verify(x => x.ReceiveMessagesAsync(), Times.Once);
        }

        [Fact]
        public async Task HandleShouldSendEachMessageToServiceBus()
        {
            var messages = new[]
            {
                new SentMessage { Message = "msg1" },
                new SentMessage { Message = "msg2" }
            };

            _messageMock.Setup(x => x.ReceiveMessagesAsync()).ReturnsAsync(messages);
            _serviceBusMock.Setup(x => x.SendMessageAsync("receivedmessages", It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            await _handler.Handle(new HandleReceiveMessagesCommand());

            _serviceBusMock.Verify(x => x.SendMessageAsync("receivedmessages", "msg1"), Times.Once);
            _serviceBusMock.Verify(x => x.SendMessageAsync("receivedmessages", "msg2"), Times.Once);
        }

        [Fact]
        public async Task HandleShouldNotSendToServiceBusWhenNoMessages()
        {
            _messageMock.Setup(x => x.ReceiveMessagesAsync()).ReturnsAsync(Enumerable.Empty<SentMessage>());

            await _handler.Handle(new HandleReceiveMessagesCommand());

            _serviceBusMock.Verify(x => x.SendMessageAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }
    }
}
