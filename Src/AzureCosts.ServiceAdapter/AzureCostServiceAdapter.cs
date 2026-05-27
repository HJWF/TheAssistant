using Azure.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TheAssistant.Core;
using TheAssistant.Core.AzureCosts;

namespace TheAssistant.AzureCosts.ServiceAdapter;

public class AzureCostServiceAdapter : IAzureCostServiceAdapter
{
    private readonly ILogger<AzureCostServiceAdapter> _logger;
    private readonly AzureCostQueryExecutor _queryExecutor;
    private readonly AzureCostResponseParser _responseParser;

    public AzureCostServiceAdapter(
        IOptions<AzureCostSettings> settings, 
        ILogger<AzureCostServiceAdapter> logger,
        IHttpClientFactory httpClientFactory,
        TokenCredential tokenCredential)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        if (tokenCredential == null)
        {
            throw new ArgumentNullException(nameof(tokenCredential));
        }

        var config = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        var subscriptionId = config.SubscriptionId ?? throw new ArgumentException("SubscriptionId is required");

        var httpClient = httpClientFactory.CreateClient();
        
        _queryExecutor = new AzureCostQueryExecutor(subscriptionId, httpClient, tokenCredential, logger);
        _responseParser = new AzureCostResponseParser(logger);
    }

    public async Task<AzureCostSummary> GetCurrentMonthCosts()
    {
        var now = DateTime.UtcNow;
        var startDate = new DateTime(now.Year, now.Month, 1);
        var endDate = now.Date;

        _logger.LogInformation("Getting current month costs: {StartDate} to {EndDate}", startDate, endDate);

        return await GetCostsForPeriod(startDate, endDate);
    }

    public async Task<AzureCostSummary> GetPreviousMonthCosts()
    {
        var now = DateTime.UtcNow;
        var firstDayOfCurrentMonth = new DateTime(now.Year, now.Month, 1);
        var firstDayOfPreviousMonth = firstDayOfCurrentMonth.AddMonths(-1);
        var lastDayOfPreviousMonth = firstDayOfCurrentMonth.AddDays(-1);

        _logger.LogInformation("Getting previous month costs: {StartDate} to {EndDate}", 
            firstDayOfPreviousMonth, lastDayOfPreviousMonth);

        return await GetCostsForPeriod(firstDayOfPreviousMonth, lastDayOfPreviousMonth);
    }

    public async Task<AzureCostSummary> GetCostsForPeriod(DateTime startDate, DateTime endDate)
    {
        try
        {
            _logger.LogInformation("Starting cost query for period {StartDate} to {EndDate}", startDate, endDate);

            var queryRequest = AzureCostQueryExecutor.CreateQueryRequest(startDate, endDate);
            var queryResult = await _queryExecutor.ExecuteQueryAsync(queryRequest);
            var summary = _responseParser.ParseResponse(queryResult, startDate, endDate);

            _logger.LogInformation("Successfully retrieved costs: {TotalCost} {Currency}", 
                summary.TotalCost, summary.Currency);

            return summary;
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
        {
            _logger.LogError(ex, "Access denied to Azure Cost Management");
            throw new InvalidOperationException("Insufficient permissions to read Azure costs", ex);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Azure Cost Management API request failed with status {Status}", ex.StatusCode);
            throw new InvalidOperationException($"Failed to retrieve Azure costs: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error retrieving Azure costs");
            throw;
        }
    }
}
