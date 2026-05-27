using Azure.Core;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using TheAssistant.AzureCosts.ServiceAdapter.Models;

namespace TheAssistant.AzureCosts.ServiceAdapter;

internal class AzureCostQueryExecutor
{
    private readonly string _subscriptionId;
    private readonly HttpClient _httpClient;
    private readonly TokenCredential _credential;
    private readonly ILogger _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public AzureCostQueryExecutor(
        string subscriptionId,
        HttpClient httpClient,
        TokenCredential credential,
        ILogger logger)
    {
        _subscriptionId = subscriptionId;
        _httpClient = httpClient;
        _credential = credential;
        _logger = logger;
        
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            NumberHandling = JsonNumberHandling.AllowReadingFromString
        };
    }

    public async Task<CostQueryResult> ExecuteQueryAsync(CostQueryRequest queryContent)
    {
        var scope = $"/subscriptions/{_subscriptionId}";
        var uri = $"https://management.azure.com{scope}/providers/Microsoft.CostManagement/query?api-version=2023-11-01";
        
        _logger.LogInformation("Executing Cost Query: {Uri}", uri);

        var accessToken = await _credential.GetTokenAsync(
            new TokenRequestContext(new[] { "https://management.azure.com/.default" }), 
            default);

        var request = new HttpRequestMessage(HttpMethod.Post, uri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken.Token);
        request.Content = JsonContent.Create(queryContent, options: new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });

        _logger.LogDebug("Request body: {RequestBody}", 
            JsonSerializer.Serialize(queryContent, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));

        var response = await _httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync();
            _logger.LogError("Cost Management API error response (Status {Status}): {ErrorBody}", 
                response.StatusCode, errorBody);
            throw new HttpRequestException(
                $"Cost Management API returned status {response.StatusCode}", 
                null, 
                response.StatusCode);
        }

        var responseBody = await response.Content.ReadAsStringAsync();
        _logger.LogDebug("Received response body: {ResponseBody}", responseBody);

        try
        {
            var queryResult = JsonSerializer.Deserialize<CostQueryResult>(responseBody, _jsonOptions);
            
            if (queryResult == null)
            {
                _logger.LogError("Deserialization returned null. Response: {ResponseBody}", responseBody);
                throw new InvalidOperationException("Failed to deserialize cost query response");
            }

            _logger.LogDebug("Successfully deserialized response with {RowCount} rows", 
                queryResult.Properties?.Rows?.Count ?? 0);

            return queryResult;
        }
        catch (JsonException jsonEx)
        {
            _logger.LogError(jsonEx, "JSON deserialization failed. Response: {ResponseBody}", responseBody);
            throw new InvalidOperationException("Failed to deserialize cost query response", jsonEx);
        }
    }

    public static CostQueryRequest CreateQueryRequest(DateTime startDate, DateTime endDate)
    {
        var fromDate = startDate.ToString("yyyy-MM-ddTHH:mm:ssZ");
        var toDate = endDate.ToString("yyyy-MM-ddTHH:mm:ssZ");
        
        return new CostQueryRequest
        {
            Type = "ActualCost",
            Timeframe = "Custom",
            TimePeriod = new TimePeriod
            {
                From = fromDate,
                To = toDate
            },
            Dataset = new Dataset
            {
                Granularity = "None",
                Aggregation = new Dictionary<string, Aggregation>
                {
                    ["totalCost"] = new Aggregation { Name = "PreTaxCost", Function = "Sum" }
                },
                Grouping = new List<Grouping>
                {
                    new Grouping { Type = "Dimension", Name = "ServiceName" }
                }
            }
        };
    }
}
