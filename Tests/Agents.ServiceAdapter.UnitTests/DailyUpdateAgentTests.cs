using FluentAssertions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Moq;
using TheAssistant.Agents.ServiceAdapter.Agenda;
using TheAssistant.Agents.ServiceAdapter.DailyUpdate;
using TheAssistant.Agents.ServiceAdapter.Weather;
using TheAssistant.Core.Agents;
using TheAssistant.Core.Infrastructure;

namespace TheAssistant.Agents.ServiceAdapter.UnitTests;

public class DailyUpdateAgentTests
{
    private readonly Mock<IWeatherAgent> _mockWeatherAgent;
    private readonly Mock<IAgendaAgent> _mockAgendaAgent;
    private readonly Mock<IChatClient> _mockChatClient;
    private readonly Mock<ILogger<DailyUpdateAgent>> _mockLogger;
    private readonly Mock<ITokenUsageTracker> _mockTracker;
    private readonly DailyUpdateAgent _agent;
    private readonly UserDetails _testUser;
    private readonly AgentMessage _testMessage;

    public DailyUpdateAgentTests()
    {
        _mockWeatherAgent = new Mock<IWeatherAgent>();
        _mockAgendaAgent = new Mock<IAgendaAgent>();
        _mockChatClient = new Mock<IChatClient>();
        _mockLogger = new Mock<ILogger<DailyUpdateAgent>>();
        _mockTracker = new Mock<ITokenUsageTracker>();

        _agent = new DailyUpdateAgent(
            _mockWeatherAgent.Object,
            _mockAgendaAgent.Object,
            _mockChatClient.Object,
            _mockLogger.Object,
            _mockTracker.Object);

        _testUser = new UserDetails("+31630000000", "test@example.com", "work@example.com");
        _testMessage = new AgentMessage(_testUser, "user", AgentConstants.Names.DailyUpdate, "user", "Get a daily overview", null);
    }

    [Fact]
    public void NameShouldReturnDailyUpdateAgent()
    {
        _agent.Name.Should().Be(AgentConstants.Names.DailyUpdate);
    }

    [Fact]
    public async Task HandleAsyncShouldCallChatServiceWithTwoTools()
    {
        _mockChatClient
            .Setup(x => x.GetResponseAsync(
                It.IsAny<IEnumerable<ChatMessage>>(),
                It.IsAny<ChatOptions?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatResponse([new ChatMessage(ChatRole.Assistant, "Daily update")]));

        await _agent.HandleAsync(_testMessage);

        _mockChatClient.Verify(x => x.GetResponseAsync(
            It.Is<IEnumerable<ChatMessage>>(m => m.Count() >= 2),
            It.Is<ChatOptions?>(o => o != null && o.Tools != null && o.Tools.Count == 2),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsyncShouldReturnSingleMessageWithLlmContent()
    {
        _mockChatClient
            .Setup(x => x.GetResponseAsync(
                It.IsAny<IEnumerable<ChatMessage>>(),
                It.IsAny<ChatOptions?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatResponse([new ChatMessage(ChatRole.Assistant, "Here is your daily summary")]));

        var result = await _agent.HandleAsync(_testMessage);

        result.Should().ContainSingle();
        result.First().Content.Should().Be("Here is your daily summary");
    }

    [Fact]
    public async Task HandleAsyncWeatherToolShouldCallWeatherAgent()
    {
        ChatOptions? capturedOptions = null;
        _mockChatClient
            .Setup(x => x.GetResponseAsync(
                It.IsAny<IEnumerable<ChatMessage>>(),
                It.IsAny<ChatOptions?>(),
                It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<ChatMessage>, ChatOptions?, CancellationToken>((_, opts, _) => capturedOptions = opts)
            .ReturnsAsync(new ChatResponse([new ChatMessage(ChatRole.Assistant, "Summary")]));

        _mockWeatherAgent
            .Setup(x => x.HandleAsync(It.IsAny<AgentMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new AgentMessage(_testUser, AgentConstants.Names.Weather, "user", "agent", "Sunny, 22°C", null)]);

        await _agent.HandleAsync(_testMessage);

        var weatherTool = capturedOptions?.Tools?.OfType<AIFunction>()
            .FirstOrDefault(t => t.Name == "GetDailyWeatherSummary");
        weatherTool.Should().NotBeNull();

        await weatherTool!.InvokeAsync(new AIFunctionArguments(), CancellationToken.None);

        _mockWeatherAgent.Verify(x => x.HandleAsync(
            It.Is<AgentMessage>(m => m.Content == "What's the weather forecast for today?"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsyncCalendarToolShouldCallAgendaAgent()
    {
        ChatOptions? capturedOptions = null;
        _mockChatClient
            .Setup(x => x.GetResponseAsync(
                It.IsAny<IEnumerable<ChatMessage>>(),
                It.IsAny<ChatOptions?>(),
                It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<ChatMessage>, ChatOptions?, CancellationToken>((_, opts, _) => capturedOptions = opts)
            .ReturnsAsync(new ChatResponse([new ChatMessage(ChatRole.Assistant, "Summary")]));

        _mockAgendaAgent
            .Setup(x => x.HandleAsync(It.IsAny<AgentMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new AgentMessage(_testUser, AgentConstants.Names.Agenda, "user", "agent", "You have 2 meetings", null)]);

        await _agent.HandleAsync(_testMessage);

        var calendarTool = capturedOptions?.Tools?.OfType<AIFunction>()
            .FirstOrDefault(t => t.Name == "GetDailyCalendarSummary");
        calendarTool.Should().NotBeNull();

        await calendarTool!.InvokeAsync(new AIFunctionArguments(), CancellationToken.None);

        _mockAgendaAgent.Verify(x => x.HandleAsync(
            It.Is<AgentMessage>(m => m.Content == "What are today's meetings?"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsyncWhenExceptionOccursShouldReturnSorryMessage()
    {
        _mockChatClient
            .Setup(x => x.GetResponseAsync(
                It.IsAny<IEnumerable<ChatMessage>>(),
                It.IsAny<ChatOptions?>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("LLM service error"));

        var result = await _agent.HandleAsync(_testMessage);

        result.Should().ContainSingle();
        result.First().Content.Should().Be(AgentConstants.SorryMessage);
    }

    [Fact]
    public async Task HandleAsyncShouldInjectCurrentDateInSystemPrompt()
    {
        IEnumerable<ChatMessage>? capturedMessages = null;
        _mockChatClient
            .Setup(x => x.GetResponseAsync(
                It.IsAny<IEnumerable<ChatMessage>>(),
                It.IsAny<ChatOptions?>(),
                It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<ChatMessage>, ChatOptions?, CancellationToken>((m, _, _) => capturedMessages = m)
            .ReturnsAsync(new ChatResponse([new ChatMessage(ChatRole.Assistant, "Done")]));

        await _agent.HandleAsync(_testMessage);

        var systemMessage = capturedMessages?.FirstOrDefault(m => m.Role == ChatRole.System);
        systemMessage.Should().NotBeNull();
        systemMessage!.Text.Should().Contain(DateTime.UtcNow.ToString("yyyy-MM-dd"));
    }
}
