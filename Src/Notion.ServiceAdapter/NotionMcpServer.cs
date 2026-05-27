using Microsoft.Extensions.Logging;
using Notion.Client;
using System.Text.Json;

namespace TheAssistant.Notion.ServiceAdapter;

public class NotionMcpServer
{
    private readonly INotionClient _notionClient;
    private readonly ILogger<NotionMcpServer> _logger;

    public NotionMcpServer(INotionClient notionClient, ILogger<NotionMcpServer> logger)
    {
        _notionClient = notionClient;
        _logger = logger;
    }

    public virtual async Task<string> CallToolAsync(string toolName, Dictionary<string, object> arguments)
    {
        try
        {
            return toolName switch
            {
                NotionTools.Search => await SearchAsync(arguments),
                NotionTools.CreatePage => await CreatePageAsync(arguments),
                NotionTools.QueryDatabase => await QueryDatabaseAsync(arguments),
                NotionTools.GetPageContent => await GetPageContentAsync(arguments),
                NotionTools.GetDatabase => await GetDatabaseAsync(arguments),
                NotionTools.UpdatePage => await UpdatePageAsync(arguments),
                _ => JsonSerializer.Serialize(new { error = $"Tool {toolName} not supported" })
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling tool {ToolName}", toolName);
            return JsonSerializer.Serialize(new { error = "Failed to call Notion tool" });
        }
    }

    private async Task<string> SearchAsync(Dictionary<string, object> args)
    {
        var query = args.TryGetValue(NotionParameters.Query, out var q) ? q?.ToString() : string.Empty;

        var results = await _notionClient.Search.SearchAsync(new()
        {
            Query = query
        });
        
        return JsonSerializer.Serialize(results);
    }

    private async Task<string> CreatePageAsync(Dictionary<string, object> args)
    {
        if (!args.TryGetValue(NotionParameters.DatabaseId, out var dbIdObj) || dbIdObj?.ToString() is not { } databaseId || string.IsNullOrWhiteSpace(databaseId))
        {
            throw new ArgumentException("database_id required");
        }
        
        var title = args.TryGetValue(NotionParameters.Title, out var titleObj) ? titleObj?.ToString() : null;
        title ??= "Untitled";

        var properties = new Dictionary<string, PropertyValue>
        {
            ["Name"] = new TitlePropertyValue
            {
                Title = new List<RichTextBase>
                {
                    new RichTextText
                    {
                        Text = new Text { Content = title }
                    }
                }
            }
        };

        var createParams = new PagesCreateParameters
        {
            Parent = new DatabaseParentInput { DatabaseId = databaseId },
            Properties = properties
        };

        var page = await _notionClient.Pages.CreateAsync(createParams);
        return JsonSerializer.Serialize(page);
    }

    private async Task<string> QueryDatabaseAsync(Dictionary<string, object> args)
    {
        var databaseId = (args.TryGetValue(NotionParameters.DatabaseId, out var dbId) ? dbId?.ToString() : null)
            ?? (args.TryGetValue(NotionParameters.DataSourceId, out var dsId) ? dsId?.ToString() : null)
            ?? throw new ArgumentException("database_id or data_source_id required");

        var queryParams = new DatabasesQueryParameters();

        // Add filter if provided
        if (args.TryGetValue(NotionParameters.Filter, out var filterObj) && filterObj != null)
        {
            // Notion.Net handles filter as object, pass it through
            // In production, you'd parse this more carefully
        }

        var results = await _notionClient.Databases.QueryAsync(databaseId, queryParams);
        return JsonSerializer.Serialize(results);
    }

    private async Task<string> GetPageContentAsync(Dictionary<string, object> args)
    {
        var pageId = (args.TryGetValue(NotionParameters.PageId, out var pId) ? pId?.ToString() : null)
            ?? (args.TryGetValue(NotionParameters.BlockId, out var bId) ? bId?.ToString() : null)
            ?? throw new ArgumentException("page_id or block_id required");

        var request = new BlockRetrieveChildrenRequest
        {
            BlockId = pageId
        };

        var blocks = await _notionClient.Blocks.RetrieveChildrenAsync(request);
        return JsonSerializer.Serialize(blocks);
    }

    private async Task<string> GetDatabaseAsync(Dictionary<string, object> args)
    {
        var databaseId = (args.TryGetValue(NotionParameters.DatabaseId, out var dbId) ? dbId?.ToString() : null)
            ?? (args.TryGetValue(NotionParameters.DataSourceId, out var dsId) ? dsId?.ToString() : null)
            ?? throw new ArgumentException("database_id or data_source_id required");

        var database = await _notionClient.Databases.RetrieveAsync(databaseId);
        return JsonSerializer.Serialize(database);
    }

    private async Task<string> UpdatePageAsync(Dictionary<string, object> args)
    {
        if (!args.TryGetValue(NotionParameters.PageId, out var pageIdObj) || pageIdObj?.ToString() is not { } pageId || string.IsNullOrWhiteSpace(pageId))
        {
            throw new ArgumentException("page_id required");
        }

        var properties = new Dictionary<string, PropertyValue>();

        // In production, parse properties from args
        // For now, return empty update

        var updateParams = new PagesUpdateParameters
        {
            Properties = properties
        };

        var page = await _notionClient.Pages.UpdateAsync(pageId, updateParams);
        return JsonSerializer.Serialize(page);
    }
}
