using FluentAssertions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Moq;
using TheAssistant.Agents.ServiceAdapter.Notion;
using TheAssistant.Core;
using TheAssistant.Core.Agents;
using TheAssistant.Core.Infrastructure;

namespace TheAssistant.Agents.ServiceAdapter.UnitTests.Notion;

public class NotionAgentTests
{
    private readonly Mock<INotionServiceAdapter> _mockNotionAdapter;
    private readonly Mock<IChatClient> _mockChatClient;
    private readonly Mock<ILogger<NotionAgent>> _mockLogger;
    private readonly Mock<ITokenUsageTracker> _mockTracker;
    private readonly NotionAgent _agent;
    private readonly UserDetails _testUser;

    public NotionAgentTests()
    {
        _mockNotionAdapter = new Mock<INotionServiceAdapter>();
        _mockChatClient = new Mock<IChatClient>();
        _mockLogger = new Mock<ILogger<NotionAgent>>();
        _mockTracker = new Mock<ITokenUsageTracker>();
        _agent = new NotionAgent(_mockNotionAdapter.Object, _mockChatClient.Object, _mockLogger.Object, _mockTracker.Object);
        _testUser = new UserDetails("test-user", "Test User", "test@example.com");
    }

    [Fact]
    public void NameShouldReturnNotionAgent()
    {
        _agent.Name.Should().Be(AgentConstants.Names.Notion);
    }

    [Fact]
    public async Task SearchPagesShouldCallNotionServiceAdapter()
    {
        var query = "test query";
        var expectedResult = "{\"results\": []}";
        _mockNotionAdapter.Setup(x => x.SearchPagesAsync(query))
            .ReturnsAsync(expectedResult);

        var result = await _agent.SearchPages(query);

        result.Should().Be(expectedResult);
        _mockNotionAdapter.Verify(x => x.SearchPagesAsync(query), Times.Once);
    }

    [Fact]
    public async Task SearchPagesShouldReturnErrorOnException()
    {
        var query = "test query";
        _mockNotionAdapter.Setup(x => x.SearchPagesAsync(query))
            .ThrowsAsync(new Exception("Test error"));

        var result = await _agent.SearchPages(query);

        result.Should().Contain("error");
        result.Should().Contain("Failed to search Notion pages");
    }

    [Fact]
    public async Task GetDatabaseShouldCallNotionServiceAdapter()
    {
        var databaseId = "db-123";
        var expectedResult = "{\"id\": \"db-123\"}";
        _mockNotionAdapter.Setup(x => x.GetDatabaseAsync(databaseId))
            .ReturnsAsync(expectedResult);

        var result = await _agent.GetDatabase(databaseId);

        result.Should().Be(expectedResult);
        _mockNotionAdapter.Verify(x => x.GetDatabaseAsync(databaseId), Times.Once);
    }

    [Fact]
    public async Task GetDatabaseShouldReturnErrorOnException()
    {
        var databaseId = "db-123";
        _mockNotionAdapter.Setup(x => x.GetDatabaseAsync(databaseId))
            .ThrowsAsync(new Exception("Test error"));

        var result = await _agent.GetDatabase(databaseId);

        result.Should().Contain("error");
        result.Should().Contain("Failed to get database");
    }

    [Fact]
    public async Task QueryDatabaseShouldCallNotionServiceAdapter()
    {
        var databaseId = "db-123";
        var filter = "{\"property\": \"Status\"}";
        var expectedResult = "{\"results\": []}";
        _mockNotionAdapter.Setup(x => x.QueryDatabaseAsync(databaseId, filter))
            .ReturnsAsync(expectedResult);

        var result = await _agent.QueryDatabase(databaseId, filter);

        result.Should().Be(expectedResult);
        _mockNotionAdapter.Verify(x => x.QueryDatabaseAsync(databaseId, filter), Times.Once);
    }

    [Fact]
    public async Task QueryDatabaseWithNullFilterShouldCallNotionServiceAdapter()
    {
        var databaseId = "db-123";
        var expectedResult = "{\"results\": []}";
        _mockNotionAdapter.Setup(x => x.QueryDatabaseAsync(databaseId, null))
            .ReturnsAsync(expectedResult);

        var result = await _agent.QueryDatabase(databaseId);

        result.Should().Be(expectedResult);
        _mockNotionAdapter.Verify(x => x.QueryDatabaseAsync(databaseId, null), Times.Once);
    }

    [Fact]
    public async Task CreatePageShouldCallNotionServiceAdapter()
    {
        var databaseId = "db-123";
        var title = "New Page";
        var expectedResult = "{\"id\": \"page-123\"}";
        _mockNotionAdapter.Setup(x => x.CreatePageAsync(databaseId, title, null))
            .ReturnsAsync(expectedResult);

        var result = await _agent.CreatePage(databaseId, title);

        result.Should().Be(expectedResult);
        _mockNotionAdapter.Verify(x => x.CreatePageAsync(databaseId, title, null), Times.Once);
    }

    [Fact]
    public async Task CreatePageWithPropertiesShouldDeserializeAndCallAdapter()
    {
        var databaseId = "db-123";
        var title = "New Page";
        var propertiesJson = "{\"Status\": \"Active\"}";
        var expectedResult = "{\"id\": \"page-123\"}";
        _mockNotionAdapter.Setup(x => x.CreatePageAsync(
                databaseId, 
                title, 
                It.IsAny<Dictionary<string, object>>()))
            .ReturnsAsync(expectedResult);

        var result = await _agent.CreatePage(databaseId, title, propertiesJson);

        result.Should().Be(expectedResult);
        _mockNotionAdapter.Verify(x => x.CreatePageAsync(
            databaseId, 
            title, 
            It.Is<Dictionary<string, object>>(d => d.ContainsKey("Status"))), Times.Once);
    }

    [Fact]
    public async Task UpdatePageShouldCallNotionServiceAdapter()
    {
        var pageId = "page-123";
        var propertiesJson = "{\"Status\": \"Done\"}";
        var expectedResult = "{\"id\": \"page-123\"}";
        _mockNotionAdapter.Setup(x => x.UpdatePageAsync(
                pageId, 
                It.IsAny<Dictionary<string, object>>()))
            .ReturnsAsync(expectedResult);

        var result = await _agent.UpdatePage(pageId, propertiesJson);

        result.Should().Be(expectedResult);
        _mockNotionAdapter.Verify(x => x.UpdatePageAsync(
            pageId, 
            It.Is<Dictionary<string, object>>(d => d.ContainsKey("Status"))), Times.Once);
    }

    [Fact]
    public async Task UpdatePageWithInvalidJsonShouldReturnError()
    {
        var pageId = "page-123";
        var invalidJson = "not valid json";

        var result = await _agent.UpdatePage(pageId, invalidJson);

        result.Should().Contain("error");
    }

    [Fact]
    public async Task GetPageContentShouldCallNotionServiceAdapter()
    {
        var pageId = "page-123";
        var expectedResult = "{\"results\": []}";
        _mockNotionAdapter.Setup(x => x.GetPageContentAsync(pageId))
            .ReturnsAsync(expectedResult);

        var result = await _agent.GetPageContent(pageId);

        result.Should().Be(expectedResult);
        _mockNotionAdapter.Verify(x => x.GetPageContentAsync(pageId), Times.Once);
    }

    [Fact]
    public async Task HandleAsyncShouldProcessMessageAndReturnResponse()
    {
        var inputMessage = new AgentMessage(
            _testUser,
            AgentConstants.Roles.User,
            AgentConstants.Names.Notion,
            AgentConstants.Roles.User,
            "Search for project pages",
            null);

        _mockChatClient.Setup(x => x.GetResponseAsync(
                It.IsAny<IEnumerable<ChatMessage>>(),
                It.IsAny<ChatOptions?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatResponse([new ChatMessage(ChatRole.Assistant, "I found 3 project pages in your workspace.")]));

        var responses = await _agent.HandleAsync(inputMessage);

        var response = responses.Should().ContainSingle().Subject;
        response.Sender.Should().Be(AgentConstants.Names.Notion);
        response.Role.Should().Be(AgentConstants.Roles.Agent);
        response.Content.Should().Be("I found 3 project pages in your workspace.");
    }

    [Fact]
    public async Task HandleAsyncOnExceptionShouldReturnErrorMessage()
    {
        var inputMessage = new AgentMessage(
            _testUser,
            AgentConstants.Roles.User,
            AgentConstants.Names.Notion,
            AgentConstants.Roles.User,
            "Search for pages",
            null);

        _mockChatClient.Setup(x => x.GetResponseAsync(
                It.IsAny<IEnumerable<ChatMessage>>(),
                It.IsAny<ChatOptions?>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Test error"));

        var responses = await _agent.HandleAsync(inputMessage);

        var response = responses.Should().ContainSingle().Subject;
        response.Content.Should().Contain("error");
        response.Content.Should().Contain("Notion request");
    }

    [Fact]
    public async Task HandleAsyncShouldIncludeAllToolsInChatOptions()
    {
        var inputMessage = new AgentMessage(
            _testUser,
            AgentConstants.Roles.User,
            AgentConstants.Names.Notion,
            AgentConstants.Roles.User,
            "Test message",
            null);

        ChatOptions? capturedOptions = null;
        _mockChatClient.Setup(x => x.GetResponseAsync(
                It.IsAny<IEnumerable<ChatMessage>>(),
                It.IsAny<ChatOptions?>(),
                It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<ChatMessage>, ChatOptions?, CancellationToken>((_, options, _) => capturedOptions = options)
            .ReturnsAsync(new ChatResponse([new ChatMessage(ChatRole.Assistant, "Response")]));

        await _agent.HandleAsync(inputMessage);

        capturedOptions.Should().NotBeNull();
        capturedOptions!.Tools.Should().HaveCount(6);
    }
}
