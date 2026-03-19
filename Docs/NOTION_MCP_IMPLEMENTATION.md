# Notion MCP Integration - Implementation Plan

## Overview
Integrate **Official Notion MCP Server** to enable TheAssistant to interact with Notion databases, pages, and content management using the Model Context Protocol (MCP).

## Architecture
We'll use the **official Notion MCP server** (https://github.com/makenotion/notion-mcp-server) running in **SSE (Server-Sent Events) mode** as a bridge between TheAssistant and Notion API.

```
User ? TheAssistant (.NET) ? HTTP/SSE ? Notion MCP Server (Node.js) ? Notion API
                                         (localhost:3000)
```

## Why This Approach?
- ? **Official Implementation** - Maintained by Notion
- ? **Standard MCP Protocol** - Learn real MCP patterns
- ? **21 Built-in Tools** - Search, query, create, update pages/databases
- ? **OAuth Already Done** - Use existing authentication
- ? **.NET Compatible** - SSE works with HttpClient
- ? **Production Ready** - Can deploy MCP server as sidecar container

---

## Understanding MCP (Model Context Protocol)

### What is MCP?
MCP is a protocol created by Anthropic that enables AI applications to:
- Discover available tools dynamically
- Call tools with structured parameters
- Receive structured responses
- Maintain session state

### MCP Communication Patterns

**Standard MCP uses:**
- **Stdio** (standard input/output) - Process spawning
- **SSE** (Server-Sent Events) - HTTP-based streaming

**NOT standard HTTP REST** - This is key!

### Official Notion MCP Server Tools (21 total)

From the GitHub repo, available tools:
1. **search** - Search pages/databases
2. **retrieve-a-page** - Get page metadata
3. **retrieve-block-children** - Get page content
4. **append-block-children** - Add content to page
5. **create-a-page** - Create new page
6. **update-a-page** - Update page properties
7. **archive-a-page** - Archive page
8. **query-data-source** - Query database
9. **retrieve-a-data-source** - Get database schema
10. **create-a-data-source** - Create database
11. **update-a-data-source** - Update database
12. **create-comment** - Add comment
13. **retrieve-comments** - Get comments
14. And more...

### MCP Protocol Messages

**Request:**
```json
{
  "jsonrpc": "2.0",
  "method": "tools/call",
  "params": {
    "name": "search",
    "arguments": { "query": "recipes" }
  },
  "id": 1
}
```

**Response:**
```json
{
  "jsonrpc": "2.0",
  "result": {
    "content": [
      {
        "type": "text",
        "text": "{\"results\": [...]}"
      }
    ]
  },
  "id": 1
}
```

---

## Step 0: Deploy Notion MCP Server

### Local Development

**Terminal 1: Run Notion MCP Server**
```bash
# Install
npm install -g @notionhq/notion-mcp-server

# Run with SSE transport
NOTION_TOKEN=ntn_YOUR_TOKEN notion-mcp-server --transport http --port 3000 --auth-token "your-secret-token"
```

**Server will be available at:** `http://localhost:3000/mcp`

### Production Deployment

**Option 1: Docker Sidecar (Recommended)**
```yaml
# docker-compose.yml
services:
  notion-mcp:
    image: mcp/notion
    environment:
      - NOTION_TOKEN=${NOTION_TOKEN}
      - AUTH_TOKEN=${MCP_AUTH_TOKEN}
    command: ["--transport", "http", "--port", "3000"]
    ports:
      - "3000:3000"
    networks:
      - assistant-network
```

**Option 2: Azure Container Instances**
- Deploy MCP server as separate container
- Use private networking
- TheAssistantApi connects via container app environment

```
Src/Notion.ServiceAdapter/
??? NotionMcpClient.cs              # HTTP client for MCP endpoints
??? NotionServiceAdapter.cs         # Main service adapter
??? INotionServiceAdapter.cs        # Interface (in Core)
??? NotionSettings.cs               # Configuration (already exists in Core)
??? Module.cs                       # DI registration
??? Models/
    ??? NotionPage.cs               # Page model
    ??? NotionDatabase.cs           # Database model
    ??? NotionBlock.cs              # Block content model
    ??? NotionSearchResult.cs       # Search response
```

---

## Step 2: Core Interfaces & Models

### Add to Core project:

**File: `Src/Core/INotionServiceAdapter.cs`**
```csharp
namespace TheAssistant.Core
{
    public interface INotionServiceAdapter
    {
        Task<string> SearchPagesAsync(string query);
        Task<string> GetDatabaseAsync(string databaseId);
        Task<string> QueryDatabaseAsync(string databaseId, string filter);
        Task<string> CreatePageAsync(string databaseId, string title, Dictionary<string, object> properties);
        Task<string> UpdatePageAsync(string pageId, Dictionary<string, object> properties);
        Task<string> GetPageContentAsync(string pageId);
    }
}
```

**File: `Src/Core/Notion/` (new folder)**
- `NotionPage.cs`
- `NotionDatabase.cs`
- `NotionBlock.cs`

---

---

## Step 1: Notion.ServiceAdapter Project Structure

```
Src/Notion.ServiceAdapter/
??? McpClient.cs                    # MCP protocol client (SSE)
??? NotionMcpServiceAdapter.cs      # Adapter wrapping MCP calls
??? Module.cs                       # DI registration
??? Models/
    ??? McpRequest.cs               # MCP JSON-RPC request
    ??? McpResponse.cs              # MCP JSON-RPC response
    ??? McpToolCall.cs              # Tool invocation model
```

---

## Step 2: Create MCP Client (.NET)

### 2.1 McpClient.cs - MCP Protocol Implementation

```csharp
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace TheAssistant.Notion.ServiceAdapter
{
    public class McpClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<McpClient> _logger;
        private readonly string _mcpUrl;
        private readonly string _authToken;
        private string? _sessionId;

        public McpClient(
            HttpClient httpClient,
            ILogger<McpClient> logger,
            string mcpUrl,
            string authToken)
        {
            _httpClient = httpClient;
            _logger = logger;
            _mcpUrl = mcpUrl;
            _authToken = authToken;
        }

        public async Task InitializeAsync()
        {
            _sessionId = Guid.NewGuid().ToString();

            var request = new McpRequest
            {
                JsonRpc = "2.0",
                Method = "initialize",
                Params = new Dictionary<string, object>
                {
                    { "protocolVersion", "2024-11-05" },
                    { "capabilities", new { } },
                    { "clientInfo", new { name = "TheAssistant", version = "1.0.0" } }
                },
                Id = 1
            };

            await SendMcpRequestAsync<McpResponse>(request);
            _logger.LogInformation("MCP session initialized: {SessionId}", _sessionId);
        }

        public async Task<string> CallToolAsync(string toolName, Dictionary<string, object> arguments)
        {
            if (_sessionId == null)
                await InitializeAsync();

            var request = new McpRequest
            {
                JsonRpc = "2.0",
                Method = "tools/call",
                Params = new Dictionary<string, object>
                {
                    { "name", toolName },
                    { "arguments", arguments }
                },
                Id = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };

            var response = await SendMcpRequestAsync<McpToolResponse>(request);

            // Extract content from MCP response
            if (response?.Result?.Content != null && response.Result.Content.Count > 0)
            {
                var firstContent = response.Result.Content[0];
                return firstContent.Text ?? "{}";
            }

            return "{}";
        }

        public async Task<List<McpTool>> ListToolsAsync()
        {
            if (_sessionId == null)
                await InitializeAsync();

            var request = new McpRequest
            {
                JsonRpc = "2.0",
                Method = "tools/list",
                Params = new Dictionary<string, object>(),
                Id = 2
            };

            var response = await SendMcpRequestAsync<McpToolsListResponse>(request);
            return response?.Result?.Tools ?? new List<McpTool>();
        }

        private async Task<T?> SendMcpRequestAsync<T>(McpRequest request)
        {
            var httpRequest = new HttpRequestMessage(HttpMethod.Post, _mcpUrl)
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(request),
                    Encoding.UTF8,
                    "application/json")
            };

            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _authToken);
            
            if (_sessionId != null)
            {
                httpRequest.Headers.Add("mcp-session-id", _sessionId);
            }

            var httpResponse = await _httpClient.SendAsync(httpRequest);
            httpResponse.EnsureSuccessStatusCode();

            var content = await httpResponse.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<T>(content);
        }
    }

    // Models
    public class McpRequest
    {
        [JsonPropertyName("jsonrpc")]
        public string JsonRpc { get; set; } = "2.0";

        [JsonPropertyName("method")]
        public string Method { get; set; } = string.Empty;

        [JsonPropertyName("params")]
        public Dictionary<string, object> Params { get; set; } = new();

        [JsonPropertyName("id")]
        public long Id { get; set; }
    }

    public class McpResponse
    {
        [JsonPropertyName("jsonrpc")]
        public string JsonRpc { get; set; } = string.Empty;

        [JsonPropertyName("result")]
        public object? Result { get; set; }

        [JsonPropertyName("id")]
        public long Id { get; set; }
    }

    public class McpToolResponse
    {
        [JsonPropertyName("result")]
        public McpToolResult? Result { get; set; }
    }

    public class McpToolResult
    {
        [JsonPropertyName("content")]
        public List<McpContent> Content { get; set; } = new();
    }

    public class McpContent
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("text")]
        public string? Text { get; set; }
    }

    public class McpToolsListResponse
    {
        [JsonPropertyName("result")]
        public McpToolsList? Result { get; set; }
    }

    public class McpToolsList
    {
        [JsonPropertyName("tools")]
        public List<McpTool> Tools { get; set; } = new();
    }

    public class McpTool
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("inputSchema")]
        public Dictionary<string, object> InputSchema { get; set; } = new();
    }
}
```

### 2.2 NotionMcpServiceAdapter.cs

```csharp
using Microsoft.Extensions.Logging;
using System.Text.Json;
using TheAssistant.Core;

namespace TheAssistant.Notion.ServiceAdapter
{
    public class NotionMcpServiceAdapter : INotionServiceAdapter
    {
        private readonly McpClient _mcpClient;
        private readonly ILogger<NotionMcpServiceAdapter> _logger;

        public NotionMcpServiceAdapter(
            McpClient mcpClient,
            ILogger<NotionMcpServiceAdapter> logger)
        {
            _mcpClient = mcpClient;
            _logger = logger;
        }

        public async Task<string> SearchPagesAsync(string query)
        {
            try
            {
                var args = new Dictionary<string, object>
                {
                    { "query", query },
                    { "filter", new { property = "object", value = "page" } }
                };

                return await _mcpClient.CallToolAsync("search", args);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching Notion");
                return JsonSerializer.Serialize(new { error = ex.Message });
            }
        }

        public async Task<string> GetDatabaseAsync(string databaseId)
        {
            try
            {
                var args = new Dictionary<string, object>
                {
                    { "data_source_id", databaseId }
                };

                return await _mcpClient.CallToolAsync("retrieve-a-data-source", args);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting database");
                return JsonSerializer.Serialize(new { error = ex.Message });
            }
        }

        public async Task<string> QueryDatabaseAsync(string databaseId, string filter)
        {
            try
            {
                var args = new Dictionary<string, object>
                {
                    { "data_source_id", databaseId }
                };

                if (!string.IsNullOrEmpty(filter) && filter != "{}")
                {
                    var filterObj = JsonSerializer.Deserialize<Dictionary<string, object>>(filter);
                    if (filterObj != null)
                        args["filter"] = filterObj;
                }

                return await _mcpClient.CallToolAsync("query-data-source", args);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error querying database");
                return JsonSerializer.Serialize(new { error = ex.Message });
            }
        }

        public async Task<string> CreatePageAsync(string databaseId, string title, Dictionary<string, object> properties)
        {
            try
            {
                var parent = new Dictionary<string, object>
                {
                    { "type", "database_id" },
                    { "database_id", databaseId }
                };

                var pageProperties = new Dictionary<string, object>
                {
                    { "title", new { title = new[] { new { text = new { content = title } } } } }
                };

                // Merge custom properties
                foreach (var prop in properties)
                {
                    pageProperties[prop.Key] = prop.Value;
                }

                var args = new Dictionary<string, object>
                {
                    { "parent", parent },
                    { "properties", pageProperties }
                };

                return await _mcpClient.CallToolAsync("create-a-page", args);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating page");
                return JsonSerializer.Serialize(new { error = ex.Message });
            }
        }

        public async Task<string> UpdatePageAsync(string pageId, Dictionary<string, object> properties)
        {
            try
            {
                var args = new Dictionary<string, object>
                {
                    { "page_id", pageId },
                    { "properties", properties }
                };

                return await _mcpClient.CallToolAsync("update-a-page", args);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating page");
                return JsonSerializer.Serialize(new { error = ex.Message });
            }
        }

        public async Task<string> GetPageContentAsync(string pageId)
        {
            try
            {
                var args = new Dictionary<string, object>
                {
                    { "block_id", pageId }
                };

                return await _mcpClient.CallToolAsync("retrieve-block-children", args);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting page content");
                return JsonSerializer.Serialize(new { error = ex.Message });
            }
        }
    }
}
```

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TheAssistant.Core;
using TheAssistant.Core.Infrastructure;

namespace TheAssistant.Notion.ServiceAdapter
{
    public static class Module
    {
        public static IServiceCollection AddNotionServices(
            this IServiceCollection services,
            Action<NotionSettings> options)
        {
            services.AddOptions<NotionSettings>()
                .Configure(options)
                .ValidateDataAnnotations()
                .ValidateOnStart();

            services.AddHttpClient();

            services.AddSingleton<NotionMcpClient>(sp =>
            {
                var settings = sp.GetRequiredService<IOptions<NotionSettings>>().Value;
                var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient();
                var logger = sp.GetRequiredService<ILogger<NotionMcpClient>>();
                return new NotionMcpClient(httpClient, logger, settings.McpUrl);
            });

            services.AddSingleton<INotionServiceAdapter, NotionServiceAdapter>();

            return services;
        }
    }
}
```

---

## Step 4: NotionAgent in Agents.ServiceAdapter

### 4.1 Create NotionAgent.cs

**Location:** `Src/Agents.ServiceAdapter/Notion/NotionAgent.cs`

```csharp
using System.ComponentModel;
using System.Text.Json;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using TheAssistant.Agents.ServiceAdapter.AI;
using TheAssistant.Agents.ServiceAdapter.Authentication;
using TheAssistant.Core;
using TheAssistant.Core.Agents;
using TheAssistant.Core.Authentication;
using TheAssistant.Core.Infrastructure;

namespace TheAssistant.Agents.ServiceAdapter.Notion
{
    public class NotionAgent : INotionAgent
    {
        private const string SystemPrompt = """
            You are a Notion assistant with access to tools for managing Notion content.
            
            IMPORTANT:
            - You MUST use the available tools to interact with Notion. Do not make up information.
            - Current date context: Today is {CurrentDate}.
            - Be concise and helpful
            
            Available tools:
            - SearchPages: Search for pages in Notion
            - GetDatabase: Get database structure
            - QueryDatabase: Query database with filters
            - CreatePage: Create new page in database
            - UpdatePage: Update existing page
            - GetPageContent: Get full page content
            
            Process:
            1. Call the appropriate tool based on the user's question
            2. Wait for the tool result
            3. Format the response clearly
            4. Be concise
            """;

        private readonly INotionServiceAdapter _notionServiceAdapter;
        private readonly ITokenStoreServiceAdapter _tokenStoreServiceAdapter;
        private readonly ILoginUrlProvider _loginUrlProvider;
        private readonly IChatCompletionService _chatCompletionService;
        private readonly ILogger<NotionAgent> _logger;

        private const string TokenType = "notion";
        public string Name => AgentConstants.Names.Notion;

        private UserDetails? _currentUser;

        public NotionAgent(
            IChatCompletionService chatCompletionService,
            INotionServiceAdapter notionServiceAdapter,
            ITokenStoreServiceAdapter tokenStoreServiceAdapter,
            ILoginUrlProvider loginUrlProvider,
            ILogger<NotionAgent> logger)
        {
            _chatCompletionService = chatCompletionService;
            _notionServiceAdapter = notionServiceAdapter;
            _tokenStoreServiceAdapter = tokenStoreServiceAdapter;
            _loginUrlProvider = loginUrlProvider;
            _logger = logger;
        }

        [Description("Searches for pages in Notion by query")]
        public async Task<string> SearchPages(
            [Description("Search query string")] string query)
        {
            var (success, errorMessage, token) = await ValidateAndGetToken();
            if (!success)
                return JsonSerializer.Serialize(new { error = errorMessage });

            try
            {
                return await _notionServiceAdapter.SearchPagesAsync(query, token!.AccessToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching Notion pages");
                return JsonSerializer.Serialize(new { error = "Failed to search pages" });
            }
        }

        [Description("Gets a Notion database structure and schema")]
        public async Task<string> GetDatabase(
            [Description("Database ID")] string databaseId)
        {
            var (success, errorMessage, token) = await ValidateAndGetToken();
            if (!success)
                return JsonSerializer.Serialize(new { error = errorMessage });

            try
            {
                return await _notionServiceAdapter.GetDatabaseAsync(databaseId, token!.AccessToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting Notion database");
                return JsonSerializer.Serialize(new { error = "Failed to get database" });
            }
        }

        [Description("Queries a Notion database with filters")]
        public async Task<string> QueryDatabase(
            [Description("Database ID")] string databaseId,
            [Description("Optional filter criteria as JSON")] string? filter = null)
        {
            var (success, errorMessage, token) = await ValidateAndGetToken();
            if (!success)
                return JsonSerializer.Serialize(new { error = errorMessage });

            try
            {
                return await _notionServiceAdapter.QueryDatabaseAsync(
                    databaseId, filter ?? "{}", token!.AccessToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error querying Notion database");
                return JsonSerializer.Serialize(new { error = "Failed to query database" });
            }
        }

        [Description("Creates a new page in a Notion database")]
        public async Task<string> CreatePage(
            [Description("Database ID")] string databaseId,
            [Description("Page title")] string title,
            [Description("Page properties as JSON")] string propertiesJson)
        {
            var (success, errorMessage, token) = await ValidateAndGetToken();
            if (!success)
                return JsonSerializer.Serialize(new { error = errorMessage });

            try
            {
                var properties = JsonSerializer.Deserialize<Dictionary<string, object>>(propertiesJson);
                return await _notionServiceAdapter.CreatePageAsync(
                    databaseId, title, properties!, token!.AccessToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating Notion page");
                return JsonSerializer.Serialize(new { error = "Failed to create page" });
            }
        }

        [Description("Gets the full content of a Notion page")]
        public async Task<string> GetPageContent(
            [Description("Page ID")] string pageId)
        {
            var (success, errorMessage, token) = await ValidateAndGetToken();
            if (!success)
                return JsonSerializer.Serialize(new { error = errorMessage });

            try
            {
                return await _notionServiceAdapter.GetPageContentAsync(pageId, token!.AccessToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting Notion page content");
                return JsonSerializer.Serialize(new { error = "Failed to get page content" });
            }
        }

        public async Task<IEnumerable<AgentMessage>> HandleAsync(
            AgentMessage message,
            CancellationToken cancellationToken = default)
        {
            if (message.User == null)
            {
                return [new(message.User!, Name, AgentConstants.Roles.User,
                    AgentConstants.Roles.Agent, "User details are missing.", null)];
            }

            _currentUser = message.User;

            var now = DateTime.UtcNow;
            var systemPrompt = SystemPrompt
                .Replace("{CurrentDate}", now.ToString("yyyy-MM-dd"));

            var history = new ChatHistory();
            history.AddSystemMessage(systemPrompt);
            history.AddUserMessage(message.Content);

            var tools = new List<AITool>
            {
                AIFunctionFactory.Create(SearchPages),
                AIFunctionFactory.Create(GetDatabase),
                AIFunctionFactory.Create(QueryDatabase),
                AIFunctionFactory.Create(CreatePage),
                AIFunctionFactory.Create(GetPageContent)
            };

            try
            {
                var reply = await _chatCompletionService.GetChatMessageContentAsync(
                    history,
                    new ChatOptions { Tools = tools },
                    cancellationToken);

                return [new AgentMessage(
                    message.User,
                    Name,
                    AgentConstants.Roles.User,
                    AgentConstants.Roles.Agent,
                    reply.Content ?? AgentConstants.SorryMessage,
                    null)];
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Notion HandleAsync");
                return [new(message.User, Name, AgentConstants.Roles.User,
                    AgentConstants.Roles.Agent,
                    "Sorry, I encountered an error accessing Notion.", null)];
            }
            finally
            {
                _currentUser = null;
            }
        }

        private async Task<(bool success, string? errorMessage, Token? token)> ValidateAndGetToken()
        {
            if (_currentUser is null)
                return (false, "User context is missing.", null);

            var userId = _currentUser.PersonalMailTag;
            if (string.IsNullOrEmpty(userId))
                return (false, "User ID is missing.", null);

            var token = await _tokenStoreServiceAdapter.GetToken(userId, TokenType);

            if (token == null || token.ExpiresAt <= DateTime.Now)
            {
                var loginUrl = _loginUrlProvider.GetLoginUrlForUser(userId);
                return (false, $"I need access to your Notion. Please log in: {loginUrl}", null);
            }

            return (true, null, token);
        }
    }
}
```

### 4.2 Add INotionAgent interface

**Location:** `Src/Agents.ServiceAdapter/Notion/INotionAgent.cs`

```csharp
using TheAssistant.Core.Agents;

namespace TheAssistant.Agents.ServiceAdapter.Notion
{
    public interface INotionAgent : IAgent
    {
    }
}
```

---

## Step 5: Update AgentConstants

**File:** `Src/Agents.ServiceAdapter/AgentConstants.cs`

Add to Names class:
```csharp
public const string Notion = "notion-agent";
```

---

## Step 6: Register in DI Container

### 6.1 Update Agents.ServiceAdapter Module.cs

**File:** `Src/Agents.ServiceAdapter/Module.cs`

Add registration:
```csharp
services.AddSingleton<INotionAgent>(sp =>
{
    var chat = sp.GetRequiredService<IChatCompletionService>();
    var notionAdapter = sp.GetRequiredService<INotionServiceAdapter>();
    var tokenStore = sp.GetRequiredService<ITokenStoreServiceAdapter>();
    var loginUrlProvider = sp.GetRequiredService<ILoginUrlProvider>();
    var logger = sp.GetRequiredService<ILogger<NotionAgent>>();
    
    return new NotionAgent(chat, notionAdapter, tokenStore, loginUrlProvider, logger);
});
```

Add to AgentOrchestrator:
```csharp
var agents = new List<IAgent>
{
    sp.GetRequiredService<IAgendaAgent>(),
    sp.GetRequiredService<IWeatherAgent>(),
    sp.GetRequiredService<IAzureCostAgent>(),
    sp.GetRequiredService<IDailyUpdateAgent>(),
    sp.GetRequiredService<INotionAgent>()  // ? Add this
};
```

### 6.2 Update TheAssistantApi Program.cs

**File:** `Src/TheAssistantApi/Program.cs`

Add:
```csharp
using TheAssistant.Notion.ServiceAdapter;

// In ConfigureServices:
services.AddNotionServices(ns => 
    builder.Configuration.GetSection(Constants.SectionNames.Notion).Bind(ns));
```

---

## Step 7: Configuration

### 7.1 Update Constants.cs

**File:** `Src/TheAssistantApi/Infrastructure/Constants.cs`

Add:
```csharp
public const string Notion = "Notion";
```

### 7.2 Update appsettings.json

**File:** `Src/TheAssistantApi/appsettings.json`

Add section:
```json
"Notion": {
  "ClientId": "***",
  "ClientSecret": "***",
  "RedirectUri": "https://your-function-app.azurewebsites.net/api/notion/callback",
  "McpUrl": "https://your-mcp-server.com"
}
```

---

## Step 8: Update RoutingAgent

**File:** `Src/Agents.ServiceAdapter/Routing/RoutingAgent.cs`

Update prompt to include:
```csharp
Route to one or more of these known agents:
agenda-agent, weather-agent, azurecost-agent, notion-agent, dailyupdate-agent
```

---

## Step 9: Create Unit Tests

### 9.1 Create Test Project

```bash
dotnet new xunit -n Notion.ServiceAdapter.UnitTests -o Tests/Notion.ServiceAdapter.UnitTests
dotnet sln add Tests/Notion.ServiceAdapter.UnitTests
```

### 9.2 Test Files

**NotionServiceAdapterTests.cs**
```csharp
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using System.Net;

namespace TheAssistant.Notion.ServiceAdapter.UnitTests
{
    public class NotionServiceAdapterTests
    {
        private readonly Mock<ILogger<NotionServiceAdapter>> mockLogger;
        private readonly Mock<HttpMessageHandler> mockHttpHandler;
        private readonly NotionMcpClient mcpClient;

        public NotionServiceAdapterTests()
        {
            mockLogger = new Mock<ILogger<NotionServiceAdapter>>();
            mockHttpHandler = new Mock<HttpMessageHandler>();
            
            var httpClient = new HttpClient(mockHttpHandler.Object);
            mcpClient = new NotionMcpClient(
                httpClient,
                Mock.Of<ILogger<NotionMcpClient>>(),
                "https://test-mcp.com");
        }

        [Fact]
        public async Task SearchPagesAsyncShouldReturnResults()
        {
            SetupSuccessfulHttpResponse(@"{""results"": [{""id"": ""123""}]}");
            var adapter = new NotionServiceAdapter(mcpClient, mockLogger.Object);

            var result = await adapter.SearchPagesAsync("test query", "token");

            result.Should().NotBeNullOrEmpty();
            result.Should().Contain("123");
        }

        private void SetupSuccessfulHttpResponse(string content)
        {
            mockHttpHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(content)
                });
        }
    }
}
```

**NotionAgentTests.cs** - Follow same pattern as AgendaAgentTests

---

## Step 10: Documentation Updates

### 10.1 Update ARCHITECTURE.md

Add Notion section:
```markdown
### NotionAgent

- 5 tools: SearchPages, GetDatabase, QueryDatabase, CreatePage, GetPageContent
- OAuth via Notion MCP
- Full CRUD capabilities
- Search and query support
```

---

## Implementation Checklist

### Phase 1: Foundation (1-2 hours)
- [x] Create Notion.ServiceAdapter project
- [ ] Add Core interfaces (INotionServiceAdapter)
- [ ] Create NotionMcpClient
- [ ] Create NotionServiceAdapter
- [ ] Create Module.cs for DI
- [ ] Add project references

### Phase 2: Agent Implementation (2-3 hours)
- [ ] Create NotionAgent with 5 tools
- [ ] Add INotionAgent interface
- [ ] Update AgentConstants
- [ ] Add authentication flow

### Phase 3: Integration (1 hour)
- [ ] Register in Agents.ServiceAdapter Module
- [ ] Register in TheAssistantApi Program
- [ ] Update Constants
- [ ] Add configuration section
- [ ] Update RoutingAgent prompt

### Phase 4: Testing (2-3 hours)
- [ ] Create test project
- [ ] NotionServiceAdapterTests (10+ tests)
- [ ] NotionAgentTests (10+ tests)
- [ ] Integration smoke tests

### Phase 5: Documentation (30 mins)
- [ ] Update ARCHITECTURE.md
- [ ] Update README
- [ ] Add configuration guide

---

## Expected User Experience

```
User: "Search for recipes in Notion"
?
RoutingAgent ? notion-agent
?
NotionAgent ? SearchPages("recipes")
?
Notion MCP ? Returns results
?
LLM formats: "I found 5 recipes: ..."

User: "Create a new recipe for pasta"
?
NotionAgent ? CreatePage(database, "Pasta", properties)
?
Response: "Created new recipe: Pasta"
```

---

## Next Steps

Ready to implement? I can:
1. Create all the files automatically
2. Guide you step-by-step
3. Focus on specific parts first

What would you like me to start with?
