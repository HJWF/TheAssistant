using FluentAssertions;
using Moq;
using TheAssistant.Core.Messaging.HandleDailyOverview;
using TheAssistant.Core.Messaging.HandleReceiveMessages;

namespace TheAssistant.Core.UnitTests.Messaging;

public class HandleDailyOverviewCommandHandlerTests
{
    private readonly Mock<IServiceBusServiceAdapter> _serviceBusMock;
    private readonly HandleDailyOverviewCommandHandler _handler;

    public HandleDailyOverviewCommandHandlerTests()
    {
        _serviceBusMock = new Mock<IServiceBusServiceAdapter>(MockBehavior.Strict);
        var logger = new Mock<Microsoft.Extensions.Logging.ILogger<HandleReceiveMessagesCommandHandler>>().Object;
        _handler = new HandleDailyOverviewCommandHandler(logger, _serviceBusMock.Object);
    }

    [Fact]
    public async Task HandleShouldSendMessageToServiceBus()
    {
        _serviceBusMock
            .Setup(x => x.SendMessageAsync("receivedmessages", "Get a daily overview."))
            .Returns(Task.CompletedTask)
            .Verifiable();

        await _handler.Handle(new HandleDailyOverviewCommand(DateTime.UtcNow));

        _serviceBusMock.Verify(x => x.SendMessageAsync("receivedmessages", "Get a daily overview."), Times.Once);
    }

    [Fact]
    public async Task HandleShouldCompleteSuccessfully()
    {
        _serviceBusMock
            .Setup(x => x.SendMessageAsync(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var act = async () => await _handler.Handle(new HandleDailyOverviewCommand(DateTime.UtcNow));

        await act.Should().NotThrowAsync();
    }
}
