using Microsoft.Extensions.Logging;
using System.Text.Json;
using TheAssistant.Core.AzureCosts;
using TheAssistant.AzureCosts.ServiceAdapter.Models;

namespace TheAssistant.AzureCosts.ServiceAdapter;

internal class AzureCostResponseParser
{
    private readonly ILogger _logger;

    public AzureCostResponseParser(ILogger logger)
    {
        _logger = logger;
    }

    public AzureCostSummary ParseResponse(CostQueryResult queryResult, DateTime startDate, DateTime endDate)
    {
        var period = GetPeriodString(startDate, endDate);
        var currency = ExtractCurrency(queryResult);
        var (costColumnIndex, serviceNameColumnIndex) = GetColumnIndices(queryResult);

        if (costColumnIndex < 0)
        {
            _logger.LogWarning("Cost column not found in response");
            return new AzureCostSummary(period, 0, currency, new List<ServiceCost>());
        }

        var (totalCost, serviceCosts) = ExtractServiceCosts(queryResult, costColumnIndex, serviceNameColumnIndex);
        var topServices = BuildTopServicesList(serviceCosts, totalCost);

        _logger.LogInformation("Parsed {ServiceCount} services, total cost: {TotalCost} {Currency}", 
            serviceCosts.Count, totalCost, currency);

        return new AzureCostSummary(period, Math.Round(totalCost, 2), currency, topServices);
    }

    private string ExtractCurrency(CostQueryResult queryResult)
    {
        if (queryResult.Properties?.Rows == null || !queryResult.Properties.Rows.Any() 
            || queryResult.Properties.Columns == null || !queryResult.Properties.Columns.Any(c => c.Name == "Currency"))
        {
            return "EUR";
        }

        var currencyColumnIndex = Array.FindIndex(queryResult.Properties.Columns.ToArray(), c => c.Name == "Currency");
        
        if (currencyColumnIndex >= 0 && queryResult.Properties.Rows.First().Count > currencyColumnIndex)
        {
            var currencyValue = queryResult.Properties.Rows.First()[currencyColumnIndex];
            return currencyValue?.ToString() ?? "EUR";
        }

        return "EUR";
    }

    private (int costIndex, int serviceNameIndex) GetColumnIndices(CostQueryResult queryResult)
    {
        var columns = queryResult.Properties?.Columns?.ToArray() ?? Array.Empty<Column>();
        var costColumnIndex = Array.FindIndex(columns, c => c.Name == "PreTaxCost");
        var serviceNameColumnIndex = Array.FindIndex(columns, c => c.Name == "ServiceName");
        
        _logger.LogDebug("Column indices - Cost: {CostIndex}, ServiceName: {ServiceNameIndex}", 
            costColumnIndex, serviceNameColumnIndex);
        
        return (costColumnIndex, serviceNameColumnIndex);
    }

    private (decimal totalCost, List<(string ServiceName, decimal Cost)> serviceCosts) ExtractServiceCosts(
        CostQueryResult queryResult, 
        int costColumnIndex, 
        int serviceNameColumnIndex)
    {
        var serviceCosts = new List<(string ServiceName, decimal Cost)>();
        decimal totalCost = 0;
        var rows = queryResult.Properties?.Rows ?? new List<List<object>>();

        _logger.LogDebug("Processing {RowCount} rows from cost query", rows.Count);

        foreach (var row in rows)
        {
            if (row.Count <= costColumnIndex || row[costColumnIndex] == null)
            {
                _logger.LogWarning("Row has missing cost data, skipping");
                continue;
            }

            try
            {
                var costValue = row[costColumnIndex];
                decimal cost;

                if (costValue is JsonElement jsonElement)
                {
                    cost = jsonElement.GetDecimal();
                }
                else
                {
                    cost = Convert.ToDecimal(costValue);
                }

                totalCost += cost;

                if (serviceNameColumnIndex >= 0 && row.Count > serviceNameColumnIndex)
                {
                    var serviceNameValue = row[serviceNameColumnIndex];
                    string serviceName;

                    if (serviceNameValue is JsonElement jsonServiceName)
                    {
                        serviceName = jsonServiceName.GetString() ?? "Unknown";
                    }
                    else
                    {
                        serviceName = serviceNameValue?.ToString() ?? "Unknown";
                    }

                    serviceCosts.Add((serviceName, cost));
                    _logger.LogDebug("Parsed service: {ServiceName} = {Cost}", serviceName, cost);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse cost row, skipping");
            }
        }

        return (totalCost, serviceCosts);
    }

    private static List<ServiceCost> BuildTopServicesList(
        List<(string ServiceName, decimal Cost)> serviceCosts, 
        decimal totalCost)
    {
        return serviceCosts
            .OrderByDescending(s => s.Cost)
            .Take(10)
            .Select(s => new ServiceCost(
                s.ServiceName,
                Math.Round(s.Cost, 2),
                totalCost > 0 ? Math.Round((s.Cost / totalCost) * 100, 1) : 0))
            .ToList();
    }

    private static string GetPeriodString(DateTime startDate, DateTime endDate)
    {
        if (startDate.Day == 1 && endDate.Day == DateTime.DaysInMonth(endDate.Year, endDate.Month))
        {
            return startDate.ToString("MMMM yyyy");
        }

        var now = DateTime.UtcNow;
        if (startDate.Year == now.Year && startDate.Month == now.Month && startDate.Day == 1)
        {
            return $"{startDate:MMMM yyyy} (month-to-date)";
        }

        return $"{startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}";
    }
}
