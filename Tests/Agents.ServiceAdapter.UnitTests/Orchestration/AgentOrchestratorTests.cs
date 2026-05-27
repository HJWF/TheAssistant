using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using TheAssistant.Agents.ServiceAdapter.Formatting;
using TheAssistant.Agents.ServiceAdapter.Orchestration;
using TheAssistant.Core.Agents;
using TheAssistant.Core.Infrastructure;

namespace TheAssistant.Agents.ServiceAdapter.UnitTests.Orchestration;

public class AgentOrchestratorTests
{
    private readonly Mock<IRoutingAgent> _routerMock;
    private readonly Mock<IFormattingAgent> _formattingMock;
    private readonly Mock<ILogger<AgentOrchestrator>> _loggerMock;
    private readonly UserDetails _user;

    public AgentOrchestratorTests()
    {
        _routerMock = new Mock<IRoutingAgent>(MockBehavior.Strict);
        _formattingMock = new Mock<IFormattingAgent>(MockBehavior.Strict);
        _loggerMock = new Mock<ILogger<AgentOrchestrator>>();
        _user = new UserDetails("+31600000000", "user@test.com", "work@test.com");
    }

    [Fact]
    public async Task ExecuteAsyncShouldCallRouter()
    {
        _routerMock.Setup(x => x.RouteAsync("hello", _user, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AgentMessage>());

        var orchestrator = CreateOrchestrator();
        await orchestrator.ExecuteAsync("hello", _user);

        _routerMock.Verify(x => x.RouteAsync("hello", _user, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsyncShouldReturnSorryMessageWhenNoRoutesFound()
    {
        _routerMock.Setup(x => x.RouteAsync(It.IsAny<string>(), It.IsAny<UserDetails>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AgentMessage>());

        var orchestrator = CreateOrchestrator();
        var result = await orchestrator.ExecuteAsync("hello", _user);

        result.Should().Be(AgentConstants.SorryMessage);
    }

    [Fact]
    public async Task ExecuteAsyncShouldInvokeMatchingAgentAndReturnFormattedResponse()
    {
        var route = new AgentMessage(_user, "router", "weather-agent", "user", "Weather?", null);
        var agentReply = new AgentMessage(_user, "weather-agent", "user", "agent", "It is sunny.", null);

        var agentMock = new Mock<IAgent>(MockBehavior.Strict);
        agentMock.Setup(a => a.Name).Returns("weather-agent");
        agentMock.Setup(a => a.HandleAsync(route, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { agentReply });

        _routerMock.Setup(x => x.RouteAsync(It.IsAny<string>(), It.IsAny<UserDetails>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AgentMessage> { route });

        _formattingMock.Setup(f => f.HandleAsync(It.IsAny<List<AgentResponse>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("It is sunny.");

        var orchestrator = CreateOrchestrator(agentMock.Object);
        var result = await orchestrator.ExecuteAsync("Weather?", _user);

        result.Should().Be("It is sunny.");
        agentMock.Verify(a => a.HandleAsync(route, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsyncShouldReturnSorryMessageWhenAgentNotFound()
    {
        var route = new AgentMessage(_user, "router", "unknown-agent", "user", "Do something", null);

        _routerMock.Setup(x => x.RouteAsync(It.IsAny<string>(), It.IsAny<UserDetails>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AgentMessage> { route });

        var orchestrator = CreateOrchestrator();
        var result = await orchestrator.ExecuteAsync("Do something", _user);

        result.Should().Be(AgentConstants.SorryMessage);
    }

    [Fact]
    public async Task ExecuteAsyncShouldReturnSorryMessageWhenRouterThrows()
    {
        _routerMock.Setup(x => x.RouteAsync(It.IsAny<string>(), It.IsAny<UserDetails>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("router error"));

        var orchestrator = CreateOrchestrator();
        var result = await orchestrator.ExecuteAsync("hello", _user);

        result.Should().Be(AgentConstants.SorryMessage);
    }

    [Fact]
    public async Task ExecuteAsyncShouldReturnCancelledMessageOnCancellation()
    {
        using var cts = new CancellationTokenSource();

        _routerMock.Setup(x => x.RouteAsync(It.IsAny<string>(), It.IsAny<UserDetails>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        var orchestrator = CreateOrchestrator();
        var result = await orchestrator.ExecuteAsync("hello", _user, cts.Token);

        result.Should().Be("Request cancelled.");
    }

    [Fact]
    public async Task ExecuteAsyncShouldReturnSorryMessageWhenAllAgentResultsFailed()
    {
        var route = new AgentMessage(_user, "router", "weather-agent", "user", "Weather?", null);

        var agentMock = new Mock<IAgent>(MockBehavior.Strict);
        agentMock.Setup(a => a.Name).Returns("weather-agent");
        agentMock.Setup(a => a.HandleAsync(route, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("agent failure"));

        _routerMock.Setup(x => x.RouteAsync(It.IsAny<string>(), It.IsAny<UserDetails>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AgentMessage> { route });

        var orchestrator = CreateOrchestrator(agentMock.Object);
        var result = await orchestrator.ExecuteAsync("Weather?", _user);

        result.Should().Be(AgentConstants.SorryMessage);
    }

    [Fact]
    public void ConstructorShouldThrowForNullAgents()
    {
        var act = () => new AgentOrchestrator(null!, _routerMock.Object, _formattingMock.Object, _loggerMock.Object);
        act.Should().Throw<ArgumentNullException>().WithParameterName("agents");
    }

    [Fact]
    public void ConstructorShouldThrowForNullRouter()
    {
        var act = () => new AgentOrchestrator(Enumerable.Empty<IAgent>(), null!, _formattingMock.Object, _loggerMock.Object);
        act.Should().Throw<ArgumentNullException>().WithParameterName("router");
    }

    [Fact]
    public void ConstructorShouldThrowForNullFormattingAgent()
    {
        var act = () => new AgentOrchestrator(Enumerable.Empty<IAgent>(), _routerMock.Object, null!, _loggerMock.Object);
        act.Should().Throw<ArgumentNullException>().WithParameterName("formattingAgent");
    }

    private AgentOrchestrator CreateOrchestrator(params IAgent[] agents) =>
        new(agents, _routerMock.Object, _formattingMock.Object, _loggerMock.Object);
}
