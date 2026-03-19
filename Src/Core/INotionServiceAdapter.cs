namespace TheAssistant.Core
{
    public interface INotionServiceAdapter
    {
        Task<string> SearchPagesAsync(string query);
        Task<string> GetDatabaseAsync(string databaseId);
        Task<string> QueryDatabaseAsync(string databaseId, string? filter);
        Task<string> CreatePageAsync(string databaseId, string title, Dictionary<string, object>? properties);
        Task<string> UpdatePageAsync(string pageId, Dictionary<string, object> properties);
        Task<string> GetPageContentAsync(string pageId);
    }
}
