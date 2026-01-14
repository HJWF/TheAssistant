namespace TheAssistant.AzureCosts.ServiceAdapter.Models
{
    internal class CostQueryRequest
    {
        public string Type { get; set; } = string.Empty;
        public string Timeframe { get; set; } = string.Empty;
        public TimePeriod? TimePeriod { get; set; }
        public Dataset Dataset { get; set; } = new();
    }
}
