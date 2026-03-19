using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace TheAssistant.Notion.ServiceAdapter.UnitTests
{
    public class NotionServiceAdapterTests
    {
        private readonly Mock<NotionMcpServer> mockMcpServer;
        private readonly Mock<ILogger<NotionServiceAdapter>> mockLogger;
        private readonly NotionServiceAdapter adapter;

        public NotionServiceAdapterTests()
        {
            mockMcpServer = new Mock<NotionMcpServer>(MockBehavior.Strict, null!, null!);
            mockLogger = new Mock<ILogger<NotionServiceAdapter>>();
            adapter = new NotionServiceAdapter(mockMcpServer.Object, mockLogger.Object);
        }

        [Fact]
        public async Task SearchPagesAsyncShouldCallSearchTool()
        {
            mockMcpServer.Setup(x => x.CallToolAsync(
                    "search",
                    It.Is<Dictionary<string, object>>(d => d.ContainsKey("query"))))
                .ReturnsAsync("{\"results\": []}");

            var result = await adapter.SearchPagesAsync("test query");

            result.Should().NotBeNullOrEmpty();
            mockMcpServer.Verify(x => x.CallToolAsync("search", It.IsAny<Dictionary<string, object>>()), Times.Once);
        }

        [Fact]
        public async Task SearchPagesAsyncShouldPassQueryParameter()
        {
            mockMcpServer.Setup(x => x.CallToolAsync(
                    "search",
                    It.Is<Dictionary<string, object>>(d => d["query"].ToString() == "my search")))
                .ReturnsAsync("{\"results\": []}");

            await adapter.SearchPagesAsync("my search");

            mockMcpServer.Verify(x => x.CallToolAsync(
                "search",
                It.Is<Dictionary<string, object>>(d => d["query"].ToString() == "my search")), Times.Once);
        }

        [Fact]
        public async Task SearchPagesAsyncShouldReturnErrorOnException()
        {
            mockMcpServer.Setup(x => x.CallToolAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>()))
                .ThrowsAsync(new Exception("Test error"));

            var result = await adapter.SearchPagesAsync("test");

            result.Should().Contain("error");
            result.Should().Contain("Test error");
        }

        [Fact]
        public async Task GetDatabaseAsyncShouldCallGetDatabaseTool()
        {
            mockMcpServer.Setup(x => x.CallToolAsync(
                    "get-database",
                    It.Is<Dictionary<string, object>>(d => d.ContainsKey("database_id"))))
                .ReturnsAsync("{\"id\": \"db-123\"}");

            var result = await adapter.GetDatabaseAsync("db-123");

            result.Should().Contain("db-123");
            mockMcpServer.Verify(x => x.CallToolAsync("get-database", It.IsAny<Dictionary<string, object>>()), Times.Once);
        }

        [Fact]
        public async Task GetDatabaseAsyncShouldPassDatabaseId()
        {
            mockMcpServer.Setup(x => x.CallToolAsync(
                    "get-database",
                    It.Is<Dictionary<string, object>>(d => d["database_id"].ToString() == "db-456")))
                .ReturnsAsync("{\"id\": \"db-456\"}");

            await adapter.GetDatabaseAsync("db-456");

            mockMcpServer.Verify(x => x.CallToolAsync(
                "get-database",
                It.Is<Dictionary<string, object>>(d => d["database_id"].ToString() == "db-456")), Times.Once);
        }

        [Fact]
        public async Task QueryDatabaseAsyncShouldCallQueryDatabaseTool()
        {
            mockMcpServer.Setup(x => x.CallToolAsync(
                    "query-database",
                    It.Is<Dictionary<string, object>>(d => d.ContainsKey("database_id"))))
                .ReturnsAsync("{\"results\": []}");

            var result = await adapter.QueryDatabaseAsync("db-123", null);

            result.Should().NotBeNullOrEmpty();
            mockMcpServer.Verify(x => x.CallToolAsync("query-database", It.IsAny<Dictionary<string, object>>()), Times.Once);
        }

        [Fact]
        public async Task QueryDatabaseAsyncWithFilterShouldPassFilter()
        {
            mockMcpServer.Setup(x => x.CallToolAsync(
                    "query-database",
                    It.Is<Dictionary<string, object>>(d => d.ContainsKey("filter"))))
                .ReturnsAsync("{\"results\": []}");

            await adapter.QueryDatabaseAsync("db-123", "{\"property\": \"Status\"}");

            mockMcpServer.Verify(x => x.CallToolAsync(
                "query-database",
                It.Is<Dictionary<string, object>>(d => d.ContainsKey("filter"))), Times.Once);
        }

        [Fact]
        public async Task QueryDatabaseAsyncWithoutFilterShouldNotPassFilter()
        {
            mockMcpServer.Setup(x => x.CallToolAsync(
                    "query-database",
                    It.Is<Dictionary<string, object>>(d => !d.ContainsKey("filter"))))
                .ReturnsAsync("{\"results\": []}");

            await adapter.QueryDatabaseAsync("db-123", null);

            mockMcpServer.Verify(x => x.CallToolAsync(
                "query-database",
                It.Is<Dictionary<string, object>>(d => !d.ContainsKey("filter"))), Times.Once);
        }

        [Fact]
        public async Task CreatePageAsyncShouldCallCreatePageTool()
        {
            mockMcpServer.Setup(x => x.CallToolAsync(
                    "create-page",
                    It.Is<Dictionary<string, object>>(d => 
                        d.ContainsKey("database_id") && 
                        d.ContainsKey("title"))))
                .ReturnsAsync("{\"id\": \"page-123\"}");

            var result = await adapter.CreatePageAsync("db-123", "New Page", null);

            result.Should().Contain("page-123");
            mockMcpServer.Verify(x => x.CallToolAsync("create-page", It.IsAny<Dictionary<string, object>>()), Times.Once);
        }

        [Fact]
        public async Task CreatePageAsyncShouldPassTitle()
        {
            mockMcpServer.Setup(x => x.CallToolAsync(
                    "create-page",
                    It.Is<Dictionary<string, object>>(d => d["title"].ToString() == "Test Title")))
                .ReturnsAsync("{\"id\": \"page-123\"}");

            await adapter.CreatePageAsync("db-123", "Test Title", null);

            mockMcpServer.Verify(x => x.CallToolAsync(
                "create-page",
                It.Is<Dictionary<string, object>>(d => d["title"].ToString() == "Test Title")), Times.Once);
        }

        [Fact]
        public async Task CreatePageAsyncWithPropertiesShouldPassProperties()
        {
            var properties = new Dictionary<string, object>
            {
                { "Status", "Active" }
            };

            mockMcpServer.Setup(x => x.CallToolAsync(
                    "create-page",
                    It.Is<Dictionary<string, object>>(d => d.ContainsKey("Status"))))
                .ReturnsAsync("{\"id\": \"page-123\"}");

            await adapter.CreatePageAsync("db-123", "Title", properties);

            mockMcpServer.Verify(x => x.CallToolAsync(
                "create-page",
                It.Is<Dictionary<string, object>>(d => d.ContainsKey("Status"))), Times.Once);
        }

        [Fact]
        public async Task UpdatePageAsyncShouldCallUpdatePageTool()
        {
            var properties = new Dictionary<string, object>
            {
                { "Status", "Done" }
            };

            mockMcpServer.Setup(x => x.CallToolAsync(
                    "update-page",
                    It.Is<Dictionary<string, object>>(d => 
                        d.ContainsKey("page_id") && 
                        d.ContainsKey("properties"))))
                .ReturnsAsync("{\"id\": \"page-123\"}");

            var result = await adapter.UpdatePageAsync("page-123", properties);

            result.Should().Contain("page-123");
            mockMcpServer.Verify(x => x.CallToolAsync("update-page", It.IsAny<Dictionary<string, object>>()), Times.Once);
        }

        [Fact]
        public async Task UpdatePageAsyncShouldPassPageId()
        {
            var properties = new Dictionary<string, object>();

            mockMcpServer.Setup(x => x.CallToolAsync(
                    "update-page",
                    It.Is<Dictionary<string, object>>(d => d["page_id"].ToString() == "page-456")))
                .ReturnsAsync("{\"id\": \"page-456\"}");

            await adapter.UpdatePageAsync("page-456", properties);

            mockMcpServer.Verify(x => x.CallToolAsync(
                "update-page",
                It.Is<Dictionary<string, object>>(d => d["page_id"].ToString() == "page-456")), Times.Once);
        }

        [Fact]
        public async Task GetPageContentAsyncShouldCallGetPageContentTool()
        {
            mockMcpServer.Setup(x => x.CallToolAsync(
                    "get-page-content",
                    It.Is<Dictionary<string, object>>(d => d.ContainsKey("page_id"))))
                .ReturnsAsync("{\"results\": []}");

            var result = await adapter.GetPageContentAsync("page-123");

            result.Should().NotBeNullOrEmpty();
            mockMcpServer.Verify(x => x.CallToolAsync("get-page-content", It.IsAny<Dictionary<string, object>>()), Times.Once);
        }

        [Fact]
        public async Task GetPageContentAsyncShouldPassPageId()
        {
            mockMcpServer.Setup(x => x.CallToolAsync(
                    "get-page-content",
                    It.Is<Dictionary<string, object>>(d => d["page_id"].ToString() == "page-789")))
                .ReturnsAsync("{\"results\": []}");

            await adapter.GetPageContentAsync("page-789");

            mockMcpServer.Verify(x => x.CallToolAsync(
                "get-page-content",
                It.Is<Dictionary<string, object>>(d => d["page_id"].ToString() == "page-789")), Times.Once);
        }

        [Fact]
        public async Task AllMethodsShouldReturnErrorOnException()
        {
            mockMcpServer.Setup(x => x.CallToolAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>()))
                .ThrowsAsync(new Exception("Error"));

            var searchResult = await adapter.SearchPagesAsync("test");
            var getDbResult = await adapter.GetDatabaseAsync("db");
            var queryResult = await adapter.QueryDatabaseAsync("db", null);
            var createResult = await adapter.CreatePageAsync("db", "title", null);
            var updateResult = await adapter.UpdatePageAsync("page", new Dictionary<string, object>());
            var contentResult = await adapter.GetPageContentAsync("page");

            searchResult.Should().Contain("error");
            getDbResult.Should().Contain("error");
            queryResult.Should().Contain("error");
            createResult.Should().Contain("error");
            updateResult.Should().Contain("error");
            contentResult.Should().Contain("error");
        }

        [Fact]
        public void SearchPagesAsyncShouldThrowOnNullQuery()
        {
            var act = () => adapter.SearchPagesAsync(null!);
            act.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public void SearchPagesAsyncShouldThrowOnEmptyQuery()
        {
            var act = () => adapter.SearchPagesAsync("");
            act.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public void GetDatabaseAsyncShouldThrowOnNullDatabaseId()
        {
            var act = () => adapter.GetDatabaseAsync(null!);
            act.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public void GetDatabaseAsyncShouldThrowOnEmptyDatabaseId()
        {
            var act = () => adapter.GetDatabaseAsync("");
            act.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public void QueryDatabaseAsyncShouldThrowOnNullDatabaseId()
        {
            var act = () => adapter.QueryDatabaseAsync(null!, null);
            act.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public void CreatePageAsyncShouldThrowOnNullDatabaseId()
        {
            var act = () => adapter.CreatePageAsync(null!, "title", null);
            act.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public void CreatePageAsyncShouldThrowOnNullTitle()
        {
            var act = () => adapter.CreatePageAsync("db-123", null!, null);
            act.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public void UpdatePageAsyncShouldThrowOnNullPageId()
        {
            var act = () => adapter.UpdatePageAsync(null!, new Dictionary<string, object>());
            act.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public void UpdatePageAsyncShouldThrowOnNullProperties()
        {
            var act = () => adapter.UpdatePageAsync("page-123", null!);
            act.Should().ThrowAsync<ArgumentNullException>();
        }

        [Fact]
        public void GetPageContentAsyncShouldThrowOnNullPageId()
        {
            var act = () => adapter.GetPageContentAsync(null!);
            act.Should().ThrowAsync<ArgumentException>();
        }
    }
}
