using Microsoft.Extensions.Logging;
using System.Text.Json;
using TheAssistant.Core;

namespace TheAssistant.Notion.ServiceAdapter;

public class NotionServiceAdapter : INotionServiceAdapter
{
    private readonly NotionMcpServer _mcpServer;
    private readonly ILogger<NotionServiceAdapter> _logger;

    public NotionServiceAdapter(
        NotionMcpServer mcpServer,
        ILogger<NotionServiceAdapter> logger)
    {
        _mcpServer = mcpServer;
        _logger = logger;
    }

    public async Task<string> SearchPagesAsync(string query)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query, nameof(query));
        
        try
        {
            var args = new Dictionary<string, object> { { NotionParameters.Query, query } };
            return await _mcpServer.CallToolAsync(NotionTools.Search, args);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching Notion pages");
            return JsonSerializer.Serialize(new { error = "Failed to search Notion pages" });
        }
    }

    public async Task<string> GetDatabaseAsync(string databaseId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseId, nameof(databaseId));
        
        try
        {
            var args = new Dictionary<string, object> { { NotionParameters.DatabaseId, databaseId } };
            return await _mcpServer.CallToolAsync(NotionTools.GetDatabase, args);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting database");
            return JsonSerializer.Serialize(new { error = "Failed to get database" });
        }
    }

    public async Task<string> QueryDatabaseAsync(string databaseId, string? filter)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseId, nameof(databaseId));
        
        try
        {
            var args = new Dictionary<string, object> { { NotionParameters.DatabaseId, databaseId } };
            
            if (!string.IsNullOrEmpty(filter))
            {
                args[NotionParameters.Filter] = filter;
            }

            return await _mcpServer.CallToolAsync(NotionTools.QueryDatabase, args);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error querying database");
            return JsonSerializer.Serialize(new { error = "Failed to query database" });
        }
    }

    public async Task<string> CreatePageAsync(string databaseId, string title, Dictionary<string, object>? properties)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseId, nameof(databaseId));
        ArgumentException.ThrowIfNullOrWhiteSpace(title, nameof(title));
        
        try
        {
            var args = new Dictionary<string, object>
            {
                { NotionParameters.DatabaseId, databaseId },
                { NotionParameters.Title, title }
            };

            if (properties != null)
            {
                foreach (var prop in properties)
                {
                    args[prop.Key] = prop.Value;
                }
            }

            return await _mcpServer.CallToolAsync(NotionTools.CreatePage, args);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating page");
            return JsonSerializer.Serialize(new { error = "Failed to create page" });
        }
    }

    public async Task<string> UpdatePageAsync(string pageId, Dictionary<string, object> properties)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pageId, nameof(pageId));
        ArgumentNullException.ThrowIfNull(properties, nameof(properties));
        
        try
        {
            var args = new Dictionary<string, object>
            {
                { NotionParameters.PageId, pageId },
                { NotionParameters.Properties, properties }
            };

            return await _mcpServer.CallToolAsync(NotionTools.UpdatePage, args);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating page");
            return JsonSerializer.Serialize(new { error = "Failed to update page" });
        }
    }

    public async Task<string> GetPageContentAsync(string pageId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pageId, nameof(pageId));
        
        try
        {
            var args = new Dictionary<string, object> { { NotionParameters.PageId, pageId } };
            return await _mcpServer.CallToolAsync(NotionTools.GetPageContent, args);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting page content");
            return JsonSerializer.Serialize(new { error = "Failed to get page content" });
        }
    }
}
