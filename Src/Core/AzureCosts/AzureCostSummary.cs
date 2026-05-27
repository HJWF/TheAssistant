namespace TheAssistant.Core.AzureCosts;

public class AzureCostSummary
{
    public string Period { get; set; }
    public decimal TotalCost { get; set; }
    public string Currency { get; set; }
    public List<ServiceCost> TopServices { get; set; }

    public AzureCostSummary(string period, decimal totalCost, string currency, List<ServiceCost> topServices)
    {
        Period = period;
        TotalCost = totalCost;
        Currency = currency;
        TopServices = topServices;
    }
}
