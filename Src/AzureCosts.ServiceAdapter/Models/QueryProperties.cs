namespace TheAssistant.AzureCosts.ServiceAdapter.Models;

internal class QueryProperties
{
    public List<Column>? Columns { get; set; }
    public List<List<object>>? Rows { get; set; }
}
