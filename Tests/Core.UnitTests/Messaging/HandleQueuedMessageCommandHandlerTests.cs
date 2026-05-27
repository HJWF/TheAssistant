using FluentAssertions;
using Moq;
using TheAssistant.Core.Infrastructure;
using TheAssistant.Core.Messaging;
using TheAssistant.Core.Messaging.HandleQueuedMessage;

namespace TheAssistant.Core.UnitTests.Messaging;

public class HandleQueuedMessageCommandHandlerTests
{
    private readonly Mock<IAgentServiceAdapter> _agentMock;
    private readonly Mock<IMessageServiceAdapter> _messageMock;
    private readonly HandleQueuedMessageCommandHandler _handler;
    private readonly UserDetails _user;

    public HandleQueuedMessageCommandHandlerTests()
    {
        _agentMock = new Mock<IAgentServiceAdapter>(MockBehavior.Strict);
        _messageMock = new Mock<IMessageServiceAdapter>(MockBehavior.Strict);
        _handler = new HandleQueuedMessageCommandHandler(_agentMock.Object, _messageMock.Object);
        _user = new UserDetails("+31600000000", "user@test.com", "work@test.com");
    }

    [Fact]
    public async Task HandleShouldCallAgentWithCommandMessageAndUser()
    {
        var command = new HandleQueuedMessageCommand("hello agent", _user);

        _agentMock.Setup(x => x.HandleMessageAsync("hello agent", _user)).ReturnsAsync("response");
        _messageMock.Setup(x => x.SendMessageAsync(It.IsAny<Message>())).ReturnsAsync("sent");

        await _handler.Handle(command);

        _agentMock.Verify(x => x.HandleMessageAsync("hello agent", _user), Times.Once);
    }

    [Fact]
    public async Task HandleShouldSendAgentResponseAsMessage()
    {
        var command = new HandleQueuedMessageCommand("hello agent", _user);
        var agentResponse = "Here is your answer";

        _agentMock.Setup(x => x.HandleMessageAsync(It.IsAny<string>(), It.IsAny<UserDetails>()))
            .ReturnsAsync(agentResponse);
        _messageMock.Setup(x => x.SendMessageAsync(It.Is<Message>(m => m.Content == agentResponse)))
            .ReturnsAsync("sent")
            .Verifiable();

        await _handler.Handle(command);

        _messageMock.Verify(x => x.SendMessageAsync(It.Is<Message>(m => m.Content == agentResponse)), Times.Once);
    }
}
