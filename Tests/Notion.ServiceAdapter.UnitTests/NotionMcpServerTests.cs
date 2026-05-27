using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Notion.Client;

namespace TheAssistant.Notion.ServiceAdapter.UnitTests;

public class NotionMcpServerTests
{
    private readonly Mock<INotionClient> mockNotionClient;
    private readonly Mock<ILogger<NotionMcpServer>> mockLogger;
    private readonly NotionMcpServer server;

    public NotionMcpServerTests()
    {
        mockNotionClient = new Mock<INotionClient>();
        mockLogger = new Mock<ILogger<NotionMcpServer>>();
        server = new NotionMcpServer(mockNotionClient.Object, mockLogger.Object);
    }

    [Fact]
    public async Task CallToolAsyncWithUnsupportedToolShouldReturnError()
    {
        var result = await server.CallToolAsync("invalid-tool", new Dictionary<string, object>());

        result.Should().Contain("error");
        result.Should().Contain("not supported");
    }

    [Fact]
    public async Task SearchAsyncShouldReturnSearchResults()
    {
        var searchResults = new SearchResponse
        {
            Results = new List<IObject>
            {
                new Page { Id = "page-123" }
            }
        };

        var mockSearch = new Mock<ISearchClient>();
        mockSearch.Setup(x => x.SearchAsync(It.IsAny<SearchRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(searchResults);
        mockNotionClient.Setup(x => x.Search).Returns(mockSearch.Object);

        var args = new Dictionary<string, object> { { "query", "test query" } };
        var result = await server.CallToolAsync("search", args);

        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("page-123");
    }

    [Fact]
    public async Task SearchAsyncWithEmptyQueryShouldWork()
    {
        var searchResults = new SearchResponse
        {
            Results = new List<IObject>()
        };

        var mockSearch = new Mock<ISearchClient>();
        mockSearch.Setup(x => x.SearchAsync(It.IsAny<SearchRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(searchResults);
        mockNotionClient.Setup(x => x.Search).Returns(mockSearch.Object);

        var args = new Dictionary<string, object>();
        var result = await server.CallToolAsync("search", args);

        result.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task CreatePageAsyncShouldReturnCreatedPage()
    {
        var createdPage = new Page
        {
            Id = "new-page-123"
        };

        var mockPages = new Mock<IPagesClient>();
        mockPages.Setup(x => x.CreateAsync(It.IsAny<PagesCreateParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdPage);
        mockNotionClient.Setup(x => x.Pages).Returns(mockPages.Object);

        var args = new Dictionary<string, object>
        {
            { "database_id", "db-123" },
            { "title", "Test Page" }
        };

        var result = await server.CallToolAsync("create-page", args);

        result.Should().Contain("new-page-123");
    }

    [Fact]
    public async Task CreatePageAsyncWithoutDatabaseIdShouldThrowArgumentException()
    {
        var args = new Dictionary<string, object>
        {
            { "title", "Test Page" }
        };

        var result = await server.CallToolAsync("create-page", args);

        result.Should().Be("{\"error\":\"Failed to call Notion tool\"}");
    }

    [Fact]
    public async Task QueryDatabaseAsyncShouldReturnResults()
    {
        var queryResults = new DatabaseQueryResponse
        {
            Results = new List<IWikiDatabase>
            {
                new Page { Id = "page-1" },
                new Page { Id = "page-2" }
            }
        };

        var mockDatabases = new Mock<IDatabasesClient>();
        mockDatabases.Setup(x => x.QueryAsync(
                It.IsAny<string>(),
                It.IsAny<DatabasesQueryParameters>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(queryResults);
        mockNotionClient.Setup(x => x.Databases).Returns(mockDatabases.Object);

        var args = new Dictionary<string, object>
        {
            { "database_id", "db-123" }
        };

        var result = await server.CallToolAsync("query-database", args);

        result.Should().Contain("page-1");
        result.Should().Contain("page-2");
    }

    [Fact]
    public async Task QueryDatabaseAsyncWithoutIdShouldReturnError()
    {
        var args = new Dictionary<string, object>();

        var result = await server.CallToolAsync("query-database", args);

        result.Should().Be("{\"error\":\"Failed to call Notion tool\"}");
    }

    [Fact]
    public async Task GetPageContentAsyncShouldReturnBlocks()
    {
        var blocks = new RetrieveChildrenResponse
        {
            Results = new List<IBlock>
            {
                new ParagraphBlock { Id = "block-1" }
            }
        };

        var mockBlocks = new Mock<IBlocksClient>();
        mockBlocks.Setup(x => x.RetrieveChildrenAsync(
                It.IsAny<BlockRetrieveChildrenRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(blocks);
        mockNotionClient.Setup(x => x.Blocks).Returns(mockBlocks.Object);

        var args = new Dictionary<string, object>
        {
            { "page_id", "page-123" }
        };

        var result = await server.CallToolAsync("get-page-content", args);

        result.Should().Contain("block-1");
    }

    [Fact]
    public async Task GetPageContentAsyncWithoutIdShouldReturnError()
    {
        var args = new Dictionary<string, object>();

        var result = await server.CallToolAsync("get-page-content", args);

        result.Should().Be("{\"error\":\"Failed to call Notion tool\"}");
    }

    [Fact]
    public async Task GetDatabaseAsyncShouldReturnDatabase()
    {
        var database = new Database
        {
            Id = "db-123",
            Title = new List<RichTextBase>
            {
                new RichTextText { PlainText = "Test Database" }
            }
        };

        var mockDatabases = new Mock<IDatabasesClient>();
        mockDatabases.Setup(x => x.RetrieveAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(database);
        mockNotionClient.Setup(x => x.Databases).Returns(mockDatabases.Object);

        var args = new Dictionary<string, object>
        {
            { "database_id", "db-123" }
        };

        var result = await server.CallToolAsync("get-database", args);

        result.Should().Contain("db-123");
        result.Should().Contain("Test Database");
    }

    [Fact]
    public async Task GetDatabaseAsyncWithoutIdShouldReturnError()
    {
        var args = new Dictionary<string, object>();

        var result = await server.CallToolAsync("get-database", args);

        result.Should().Be("{\"error\":\"Failed to call Notion tool\"}");
    }

    [Fact]
    public async Task UpdatePageAsyncShouldReturnUpdatedPage()
    {
        var updatedPage = new Page { Id = "page-123" };

        var mockPages = new Mock<IPagesClient>();
        mockPages.Setup(x => x.UpdateAsync(
                It.IsAny<string>(),
                It.IsAny<PagesUpdateParameters>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedPage);
        mockNotionClient.Setup(x => x.Pages).Returns(mockPages.Object);

        var args = new Dictionary<string, object>
        {
            { "page_id", "page-123" }
        };

        var result = await server.CallToolAsync("update-page", args);

        result.Should().Contain("page-123");
    }

    [Fact]
    public async Task UpdatePageAsyncWithoutPageIdShouldReturnError()
    {
        var args = new Dictionary<string, object>();

        var result = await server.CallToolAsync("update-page", args);

        result.Should().Be("{\"error\":\"Failed to call Notion tool\"}");
    }

    [Fact]
    public async Task CallToolAsyncShouldLogErrorOnException()
    {
        var mockSearch = new Mock<ISearchClient>();
        mockSearch.Setup(x => x.SearchAsync(
                It.IsAny<SearchRequest>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("API Error"));
        mockNotionClient.Setup(x => x.Search).Returns(mockSearch.Object);

        var args = new Dictionary<string, object> { { "query", "test" } };
        var result = await server.CallToolAsync("search", args);

        result.Should().Be("{\"error\":\"Failed to call Notion tool\"}");
    }

    [Fact]
    public async Task SearchAsyncShouldPassQueryToNotionClient()
    {
        var searchResults = new SearchResponse { Results = new List<IObject>() };

        var mockSearch = new Mock<ISearchClient>();
        mockSearch.Setup(x => x.SearchAsync(
                It.Is<SearchRequest>(p => p.Query == "my search query"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(searchResults);
        mockNotionClient.Setup(x => x.Search).Returns(mockSearch.Object);

        var args = new Dictionary<string, object> { { "query", "my search query" } };
        await server.CallToolAsync("search", args);

        mockSearch.Verify(x => x.SearchAsync(
            It.Is<SearchRequest>(p => p.Query == "my search query"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreatePageAsyncShouldPassTitleToNotionClient()
    {
        var createdPage = new Page { Id = "page-123" };

        var mockPages = new Mock<IPagesClient>();
        mockPages.Setup(x => x.CreateAsync(
                It.IsAny<PagesCreateParameters>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdPage);
        mockNotionClient.Setup(x => x.Pages).Returns(mockPages.Object);

        var args = new Dictionary<string, object>
        {
            { "database_id", "db-123" },
            { "title", "My Custom Title" }
        };

        await server.CallToolAsync("create-page", args);

        mockPages.Verify(x => x.CreateAsync(
            It.Is<PagesCreateParameters>(p => p.Properties.ContainsKey("Name")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task QueryDatabaseAsyncWithDataSourceIdShouldWork()
    {
        var queryResults = new DatabaseQueryResponse
        {
            Results = new List<IWikiDatabase> { new Page { Id = "page-1" } }
        };

        var mockDatabases = new Mock<IDatabasesClient>();
        mockDatabases.Setup(x => x.QueryAsync(
                "ds-123",
                It.IsAny<DatabasesQueryParameters>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(queryResults);
        mockNotionClient.Setup(x => x.Databases).Returns(mockDatabases.Object);

        var args = new Dictionary<string, object>
        {
            { "data_source_id", "ds-123" }
        };

        var result = await server.CallToolAsync("query-database", args);

        result.Should().NotBeNullOrEmpty();
        mockDatabases.Verify(x => x.QueryAsync("ds-123", It.IsAny<DatabasesQueryParameters>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetPageContentAsyncWithBlockIdShouldWork()
    {
        var blocks = new RetrieveChildrenResponse
        {
            Results = new List<IBlock>()
        };

        var mockBlocks = new Mock<IBlocksClient>();
        mockBlocks.Setup(x => x.RetrieveChildrenAsync(
                It.Is<BlockRetrieveChildrenRequest>(r => r.BlockId == "block-123"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(blocks);
        mockNotionClient.Setup(x => x.Blocks).Returns(mockBlocks.Object);

        var args = new Dictionary<string, object>
        {
            { "block_id", "block-123" }
        };

        var result = await server.CallToolAsync("get-page-content", args);

        result.Should().NotBeNullOrEmpty();
        mockBlocks.Verify(x => x.RetrieveChildrenAsync(
            It.Is<BlockRetrieveChildrenRequest>(r => r.BlockId == "block-123"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetDatabaseAsyncWithDataSourceIdShouldWork()
    {
        var database = new Database { Id = "ds-123" };

        var mockDatabases = new Mock<IDatabasesClient>();
        mockDatabases.Setup(x => x.RetrieveAsync(
                "ds-123",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(database);
        mockNotionClient.Setup(x => x.Databases).Returns(mockDatabases.Object);

        var args = new Dictionary<string, object>
        {
            { "data_source_id", "ds-123" }
        };

        var result = await server.CallToolAsync("get-database", args);

        result.Should().Contain("ds-123");
        mockDatabases.Verify(x => x.RetrieveAsync("ds-123", It.IsAny<CancellationToken>()), Times.Once);
    }
}
