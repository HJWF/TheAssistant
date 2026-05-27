namespace TheAssistant.AzureCosts.ServiceAdapter.Models;

internal class Dataset
{
    public string Granularity { get; set; } = string.Empty;
    public Dictionary<string, Aggregation> Aggregation { get; set; } = new();
    public List<Grouping> Grouping { get; set; } = new();
}
