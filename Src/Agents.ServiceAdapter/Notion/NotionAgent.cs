using System.ComponentModel;
using System.Text.Json;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using TheAssistant.Core;
using TheAssistant.Core.Agents;

namespace TheAssistant.Agents.ServiceAdapter.Notion;

public class NotionAgent : INotionAgent
{
    private const string SystemPrompt = """
        You are a Notion assistant with access to tools for managing Notion pages and databases.
        
        IMPORTANT:
        - You MUST use the available tools to interact with Notion. Do not make up information.
        - Current date context: Today is {CurrentDate}.
        - ALWAYS provide ALL required parameters when calling tools
        - Extract the search query from the user's message and pass it to SearchPages
        - Be concise and user-friendly in your responses
        
        Available tools:
        - SearchPages(query: string): Search for pages and databases. ALWAYS provide the search query parameter.
          Example: SearchPages("chicken pasta recipe")
        - GetDatabase(databaseId: string): Get details about a specific database
        - QueryDatabase(databaseId: string, filter?: string): Query a database with optional filters
        - CreatePage(databaseId: string, title: string, propertiesJson?: string): Create a new page
        - UpdatePage(pageId: string, propertiesJson: string): Update properties of a page
        - GetPageContent(pageId: string): Retrieve the content/blocks of a page
        
        Examples:
        User: "Find my chicken pasta recipe"
        You: Call SearchPages with query="chicken pasta recipe"
        
        User: "Search for project notes"
        You: Call SearchPages with query="project notes"
        
        Process:
        1. Extract key terms from the user's request
        2. Call the appropriate Notion tool with ALL required parameters
        3. Wait for the tool result
        4. Format and present the information clearly
        """;

    private readonly INotionServiceAdapter _notionServiceAdapter;
    private readonly IChatClient _chatClient;
    private readonly ILogger<NotionAgent> _logger;
    private readonly ITokenUsageTracker _tokenUsageTracker;

    public string Name => AgentConstants.Names.Notion;
    public string Description => "For searching, querying, creating, or managing Notion pages and databases.";

    public NotionAgent(
        INotionServiceAdapter notionServiceAdapter,
        IChatClient chatClient,
        ILogger<NotionAgent> logger,
        ITokenUsageTracker tokenUsageTracker)
    {
        _notionServiceAdapter = notionServiceAdapter;
        _chatClient = chatClient;
        _logger = logger;
        _tokenUsageTracker = tokenUsageTracker;
    }

    [Description("Search for Notion pages and databases by query text. REQUIRED: You must provide a search query extracted from the user's message.")]
    public async Task<string> SearchPages(
        [Description("The search query text to find pages. Extract relevant keywords from user's request. Example: 'chicken pasta recipe' or 'project notes'")] 
        string query)
    {
        try
        {
            _logger.LogInformation("Searching Notion pages.");
            var result = await _notionServiceAdapter.SearchPagesAsync(query);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching Notion pages");
            return JsonSerializer.Serialize(new { error = "Failed to search Notion pages" });
        }
    }

    [Description("Get details about a specific Notion database")]
    public async Task<string> GetDatabase(
        [Description("The database ID")] string databaseId)
    {
        try
        {
            _logger.LogInformation("Getting Notion database: {DatabaseId}", databaseId);
            var result = await _notionServiceAdapter.GetDatabaseAsync(databaseId);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Notion database");
            return JsonSerializer.Serialize(new { error = "Failed to get database" });
        }
    }

    [Description("Query a Notion database with optional filters")]
    public async Task<string> QueryDatabase(
        [Description("The database ID")] string databaseId,
        [Description("Optional JSON filter for the query")] string? filter = null)
    {
        try
        {
            _logger.LogInformation("Querying Notion database: {DatabaseId}", databaseId);
            var result = await _notionServiceAdapter.QueryDatabaseAsync(databaseId, filter);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error querying Notion database");
            return JsonSerializer.Serialize(new { error = "Failed to query database" });
        }
    }

    [Description("Create a new page in a Notion database")]
    public async Task<string> CreatePage(
        [Description("The database ID where the page will be created")] string databaseId,
        [Description("The title of the new page")] string title,
        [Description("Optional additional properties as JSON string")] string? propertiesJson = null)
    {
        try
        {
            _logger.LogInformation("Creating Notion page in database: {DatabaseId}", databaseId);
            
            Dictionary<string, object>? properties = null;
            if (!string.IsNullOrEmpty(propertiesJson))
            {
                properties = JsonSerializer.Deserialize<Dictionary<string, object>>(propertiesJson);
            }

            var result = await _notionServiceAdapter.CreatePageAsync(databaseId, title, properties);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating Notion page");
            return JsonSerializer.Serialize(new { error = "Failed to create page" });
        }
    }

    [Description("Update properties of an existing Notion page")]
    public async Task<string> UpdatePage(
        [Description("The page ID to update")] string pageId,
        [Description("Properties to update as JSON string")] string propertiesJson)
    {
        try
        {
            _logger.LogInformation("Updating Notion page: {PageId}", pageId);
            
            var properties = JsonSerializer.Deserialize<Dictionary<string, object>>(propertiesJson);
            if (properties == null)
            {
                return JsonSerializer.Serialize(new { error = "Invalid properties JSON" });
            }

            var result = await _notionServiceAdapter.UpdatePageAsync(pageId, properties);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating Notion page");
            return JsonSerializer.Serialize(new { error = "Failed to update page" });
        }
    }

    [Description("Get the content/blocks of a Notion page")]
    public async Task<string> GetPageContent(
        [Description("The page ID")] string pageId)
    {
        try
        {
            _logger.LogInformation("Getting Notion page content: {PageId}", pageId);
            var result = await _notionServiceAdapter.GetPageContentAsync(pageId);
            return result;
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
        var now = DateTime.UtcNow;
        var systemPrompt = SystemPrompt
            .Replace("{CurrentDate}", now.ToString("yyyy-MM-dd"));

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, systemPrompt),
            new(ChatRole.User, message.Content)
        };

        var tools = new List<AITool>
        {
            AIFunctionFactory.Create(SearchPages),
            AIFunctionFactory.Create(GetDatabase),
            AIFunctionFactory.Create(QueryDatabase),
            AIFunctionFactory.Create(CreatePage),
            AIFunctionFactory.Create(UpdatePage),
            AIFunctionFactory.Create(GetPageContent)
        };

        try
        {
            var response = await _chatClient.GetResponseAsync(
                messages,
                new ChatOptions { Tools = tools },
                cancellationToken);

            _tokenUsageTracker.Track(response.Usage);

            return [new AgentMessage(
                message.User,
                Name,
                AgentConstants.Roles.User,
                AgentConstants.Roles.Agent,
                !string.IsNullOrWhiteSpace(response.Text) ? response.Text : AgentConstants.SorryMessage,
                null)];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling Notion request");
            return [new AgentMessage(
                message.User,
                Name,
                AgentConstants.Roles.User,
                AgentConstants.Roles.Agent,
                "Sorry, I encountered an error while processing your Notion request.",
                null)];
        }
    }
}
