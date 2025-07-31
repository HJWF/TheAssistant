using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Web;
using TheAssistant.Core.Infrastructure;
using TheAssistant.Core.Messaging.HandleNewSignIn;

namespace TheAssistant.TheAssistantApi.Login.Consumer;

public class ConsumerLogin
{
    private readonly ILogger<ConsumerLogin> _logger;
    private readonly IConfiguration _config;
    private readonly ICommandHandler<HandleNewSignInCommand> _commandHandler;
    private const string UserId = "31630454969";

    public ConsumerLogin(ILogger<ConsumerLogin> logger, IConfiguration config, ICommandHandler<HandleNewSignInCommand> commandHandler)
    {
        _logger = logger;
        _config = config;
        _commandHandler = commandHandler;
    }

    [Function("start")]
    public HttpResponseData Start([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "login/Consumer/start")] HttpRequestData req)
    {
        _logger.LogInformation("Starting consumer login process.");
        var state = Guid.NewGuid().ToString();

        var query = HttpUtility.ParseQueryString(string.Empty);
        query["client_id"] = GetConfigValue("Agenda:ClientId") ;
        query["response_type"] = "code";
        query["redirect_uri"] = GetConfigValue("Agenda:RedirectUri");
        query["response_mode"] = "query";
        query["scope"] = "offline_access Calendars.Read";
        query["state"] = state;

        var authUrl = $"https://login.microsoftonline.com/consumers/oauth2/v2.0/authorize?{query}";

        var response = req.CreateResponse(HttpStatusCode.Redirect);
        response.Headers.Add("Location", authUrl);
        return response;
    }

    [Function("callback")]
    public async Task<HttpResponseData> CallBack([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "login/Consumer/callback")] HttpRequestData req)
    {
        var query = HttpUtility.ParseQueryString(req.Url.Query);
        var code = query["code"];
        var state = query["state"];

        if (string.IsNullOrWhiteSpace(code))
        {
            var bad = req.CreateResponse(HttpStatusCode.BadRequest);
            await bad.WriteStringAsync("Missing code");
            return bad;
        }

        var tokenClient = new HttpClient();
        var tokenRequest = new HttpRequestMessage(HttpMethod.Post, "https://login.microsoftonline.com/consumers/oauth2/v2.0/token")
        {
            Content = new FormUrlEncodedContent(
            [
                new KeyValuePair<string, string>("client_id", GetConfigValue("Agenda:ClientId")),
                new KeyValuePair<string, string>("scope", "offline_access Calendars.Read"),
                new KeyValuePair<string, string>("code", code),
                new KeyValuePair<string, string>("redirect_uri", GetConfigValue("Agenda:RedirectUri")),
                new KeyValuePair<string, string>("grant_type", "authorization_code"),
                new KeyValuePair<string, string>("client_secret", GetConfigValue("Agenda:ClientSecret")),
            ])
        };

        var tokenResponse = await tokenClient.SendAsync(tokenRequest);
        var responseContent = await tokenResponse.Content.ReadAsStringAsync();

        if (!tokenResponse.IsSuccessStatusCode)
        {
            _logger.LogError("Token request failed: {StatusCode} - {ResponseContent}", tokenResponse.StatusCode, responseContent);
            var fail = req.CreateResponse(HttpStatusCode.BadRequest);
            await fail.WriteStringAsync(responseContent);
            return fail;
        }

        var token = Newtonsoft.Json.JsonConvert.DeserializeObject<AzureTokenResponse>(responseContent);
        if (token == null)
        {
            _logger.LogError("Failed to deserialize token response: {ResponseContent}", responseContent);
            var fail = req.CreateResponse(HttpStatusCode.BadRequest);
            await fail.WriteStringAsync("Failed to retrieve token.");
            return fail;
        }

        await _commandHandler.Handle(new HandleNewSignInCommand(token.ToModel(), UserId));

        var ok = req.CreateResponse(HttpStatusCode.OK);
        await ok.WriteStringAsync("Login completed. You can return to the assistant.");
        _logger.LogInformation("Login completed successfully. You can return to the assistant");

        return ok;
    }

    private string GetConfigValue(string key)
    {
        var value = _config[key];
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"Configuration value for '{key}' is not set.");
        }
        return value;
    }
}