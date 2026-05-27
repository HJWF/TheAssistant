using FluentAssertions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Moq;
using TheAssistant.Agents.ServiceAdapter.Routing;
using TheAssistant.Core.Agents;
using TheAssistant.Core.Infrastructure;

namespace TheAssistant.Agents.ServiceAdapter.UnitTests.Routing;

public class RoutingAgentTests
{
    private readonly Mock<IChatClient> _chatClientMock;
    private readonly RoutingAgent _agent;
    private readonly UserDetails _user;

    public RoutingAgentTests()
    {
        _chatClientMock = new Mock<IChatClient>(MockBehavior.Strict);
        var logger = new Mock<ILogger<RoutingAgent>>().Object;

        var agentMock = new Mock<IAgent>();
        agentMock.Setup(a => a.Name).Returns("weather-agent");
        agentMock.Setup(a => a.Description).Returns("For weather forecasts.");

        _agent = new RoutingAgent([agentMock.Object], _chatClientMock.Object, logger);
        _user = new UserDetails("+31600000000", "user@test.com", "work@test.com");
    }

    [Fact]
    public async Task RouteAsyncShouldReturnEmptyListWhenLlmReturnsEmptyContent()
    {
        _chatClientMock.Setup(x => x.GetResponseAsync(
                It.IsAny<IEnumerable<ChatMessage>>(),
                It.IsAny<ChatOptions?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatResponse([new ChatMessage(ChatRole.Assistant, string.Empty)]));

        var result = await _agent.RouteAsync("hello", _user);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task RouteAsyncShouldReturnRoutedMessagesForValidJson()
    {
        var json = """
            {
              "messages": [
                {
                  "user": { "phoneNumber": "+31600000000", "personalMailTag": "user@test.com", "workMailTag": "work@test.com" },
                  "sender": "router",
                  "receiver": "weather-agent",
                  "role": "user",
                  "content": "What is the weather?"
                }
              ]
            }
            """;

        _chatClientMock.Setup(x => x.GetResponseAsync(
                It.IsAny<IEnumerable<ChatMessage>>(),
                It.IsAny<ChatOptions?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatResponse([new ChatMessage(ChatRole.Assistant, json)]));

        var result = await _agent.RouteAsync("What is the weather?", _user);

        result.Should().HaveCount(1);
        result[0].Receiver.Should().Be("weather-agent");
        result[0].Content.Should().Be("What is the weather?");
    }

    [Fact]
    public async Task RouteAsyncShouldReturnEmptyListWhenJsonIsInvalid()
    {
        _chatClientMock.Setup(x => x.GetResponseAsync(
                It.IsAny<IEnumerable<ChatMessage>>(),
                It.IsAny<ChatOptions?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatResponse([new ChatMessage(ChatRole.Assistant, "not valid json {{ }")]));

        var result = await _agent.RouteAsync("hello", _user);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task RouteAsyncShouldReturnEmptyListWhenJsonHasEmptyMessagesArray()
    {
        _chatClientMock.Setup(x => x.GetResponseAsync(
                It.IsAny<IEnumerable<ChatMessage>>(),
                It.IsAny<ChatOptions?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatResponse([new ChatMessage(ChatRole.Assistant, "{\"messages\": []}")]));

        var result = await _agent.RouteAsync("hello", _user);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task RouteAsyncShouldPassUserInputToLlm()
    {
        IEnumerable<ChatMessage>? capturedMessages = null;

        _chatClientMock.Setup(x => x.GetResponseAsync(
                It.IsAny<IEnumerable<ChatMessage>>(),
                It.IsAny<ChatOptions?>(),
                It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<ChatMessage>, ChatOptions?, CancellationToken>((m, _, _) => capturedMessages = m)
            .ReturnsAsync(new ChatResponse([new ChatMessage(ChatRole.Assistant, "{\"messages\": []}")]));

        await _agent.RouteAsync("my specific question", _user);

        capturedMessages.Should().NotBeNull();
        capturedMessages!.Should().Contain(m => m.Role == ChatRole.User && m.Text == "my specific question");
    }

    [Fact]
    public async Task RouteAsyncShouldRouteToMultipleAgentsWhenJsonContainsMultipleMessages()
    {
        var json = """
            {
              "messages": [
                {
                  "user": { "phoneNumber": "+31600000000", "personalMailTag": "user@test.com", "workMailTag": "work@test.com" },
                  "sender": "router", "receiver": "weather-agent", "role": "user", "content": "Weather?"
                },
                {
                  "user": { "phoneNumber": "+31600000000", "personalMailTag": "user@test.com", "workMailTag": "work@test.com" },
                  "sender": "router", "receiver": "agenda-agent", "role": "user", "content": "Calendar?"
                }
              ]
            }
            """;

        _chatClientMock.Setup(x => x.GetResponseAsync(
                It.IsAny<IEnumerable<ChatMessage>>(),
                It.IsAny<ChatOptions?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatResponse([new ChatMessage(ChatRole.Assistant, json)]));

        var result = await _agent.RouteAsync("Give me a daily update", _user);

        result.Should().HaveCount(2);
        result.Select(r => r.Receiver).Should().Contain("weather-agent").And.Contain("agenda-agent");
    }
}
