using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using TheAssistant.Agents.ServiceAdapter.Formatting;
using TheAssistant.Agents.ServiceAdapter.Orchestration;
using TheAssistant.Core.Agents;
using TheAssistant.Core.Infrastructure;

namespace TheAssistant.Agents.ServiceAdapter.UnitTests;

public class AgentServiceAdapterTests
{
    private readonly Mock<IRoutingAgent> _routerMock;
    private readonly Mock<IFormattingAgent> _formattingMock;
    private readonly Mock<ILogger<AgentOrchestrator>> _orchestratorLoggerMock;
    private readonly AgentServiceAdapter _adapter;
    private readonly UserDetails _user;

    public AgentServiceAdapterTests()
    {
        _routerMock = new Mock<IRoutingAgent>(MockBehavior.Strict);
        _formattingMock = new Mock<IFormattingAgent>(MockBehavior.Strict);
        _orchestratorLoggerMock = new Mock<ILogger<AgentOrchestrator>>();
        var adapterLogger = new Mock<ILogger<AgentServiceAdapter>>().Object;
        var trackerMock = new Mock<ITokenUsageTracker>();
        _user = new UserDetails("+31600000000", "user@test.com", "work@test.com");

        var orchestrator = new AgentOrchestrator(
            Enumerable.Empty<IAgent>(),
            _routerMock.Object,
            _formattingMock.Object,
            _orchestratorLoggerMock.Object);

        _adapter = new AgentServiceAdapter(orchestrator, trackerMock.Object, adapterLogger);
    }

    [Fact]
    public async Task HandleMessageAsyncShouldThrowArgumentNullExceptionForNullUser()
    {
        var act = async () => await _adapter.HandleMessageAsync("hello", null!);

        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("user");
    }

    [Fact]
    public async Task HandleMessageAsyncShouldThrowArgumentExceptionForEmptyInput()
    {
        var act = async () => await _adapter.HandleMessageAsync(string.Empty, _user);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("userInput");
    }

    [Fact]
    public async Task HandleMessageAsyncShouldThrowArgumentExceptionForWhitespaceInput()
    {
        var act = async () => await _adapter.HandleMessageAsync("   ", _user);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task HandleMessageAsyncShouldReturnSorryMessageWhenNoRoutesFound()
    {
        _routerMock.Setup(x => x.RouteAsync(It.IsAny<string>(), It.IsAny<UserDetails>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AgentMessage>());

        var result = await _adapter.HandleMessageAsync("What is the weather?", _user);

        result.Should().Be(AgentConstants.SorryMessage);
    }

    [Fact]
    public async Task HandleMessageAsyncShouldReturnSorryMessageWhenOrchestratorThrows()
    {
        _routerMock.Setup(x => x.RouteAsync(It.IsAny<string>(), It.IsAny<UserDetails>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("routing failed"));

        var result = await _adapter.HandleMessageAsync("hello", _user);

        result.Should().Be(AgentConstants.SorryMessage);
    }
}
