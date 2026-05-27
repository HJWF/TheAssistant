using TheAssistant.Core.AzureCosts;

namespace TheAssistant.Core;

public interface IAzureCostServiceAdapter
{
    Task<AzureCostSummary> GetCurrentMonthCosts();
    Task<AzureCostSummary> GetPreviousMonthCosts();
    Task<AzureCostSummary> GetCostsForPeriod(DateTime startDate, DateTime endDate);
}
