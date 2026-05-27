using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Web;
using TheAssistant.Core.Infrastructure;
using TheAssistant.Core.Messaging.HandleNewSignIn;
using TheAssistant.Core.State.InvalidateStateToken;
using TheAssistant.Core.State.StoreStateToken;
using TheAssistant.Core.State.ValidateStateToken;
using TheAssistant.TheAssistantApi.Login.Infrastructure;

namespace TheAssistant.TheAssistantApi.Login.Notion;

public class NotionAuthorization
{
    private readonly NotionSettings _notionSettings;
    private readonly ILogger<NotionAuthorization> _logger;
    private readonly ICommandHandler<HandleNewPersonalSignInCommand> _handleNewPersonalSignInCommandHandler;
    private readonly ICommandHandler<StoreStateTokenCommand> _storeStateTokenCommandHandler;
    private readonly ICommandHandler<InvalidateStateTokenCommand> _invalidateStateTokenCommandHandler;
    private readonly IQueryHandler<ValidateStateTokenQuery, bool> _validateStateTokenQueryHandler;
    private readonly UserDetailsSettings _userDetailsSettings;
    private readonly UserDetails _userDetails;
    private const string TokenType = "notion";
    private const string BaseUrl = "https://api.notion.com/v1/oauth";

    public NotionAuthorization(IOptions<NotionSettings> notionSettings, ILogger<NotionAuthorization> logger, ICommandHandler<StoreStateTokenCommand> storeStateTokenCommandHandler, IQueryHandler<ValidateStateTokenQuery, bool> validateStateTokenQueryHandler, ICommandHandler<HandleNewPersonalSignInCommand> handleNewPersonalSignInCommandHandler, IOptions<UserDetailsSettings> userSettings, ICommandHandler<InvalidateStateTokenCommand> invalidateStateTokenCommandHandler)
    {
        _notionSettings = notionSettings.Value;
        _logger = logger;
        _storeStateTokenCommandHandler = storeStateTokenCommandHandler;
        _userDetailsSettings = userSettings.Value;
        _validateStateTokenQueryHandler = validateStateTokenQueryHandler;
        _handleNewPersonalSignInCommandHandler = handleNewPersonalSignInCommandHandler;
        _userDetails = new UserDetails(_userDetailsSettings.PhoneNumber, _userDetailsSettings.PersonalMailTag, _userDetailsSettings.WorkMailTag);
        _invalidateStateTokenCommandHandler = invalidateStateTokenCommandHandler;
    }

    [Function("NotionAuthStart")]
    public async Task<HttpResponseData> Start([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "notion/start")] HttpRequestData request)
    {
        _logger.LogInformation("Starting Notion OAuth process.");

        var query = HttpUtility.ParseQueryString(request.Url.Query);
        var userId = query["userId"];
        if (string.IsNullOrEmpty(userId))
        {
            return await CreateResponse(request, HttpStatusCode.BadRequest, "Missing userId");
        }

        var state = Guid.NewGuid().ToString("N");
        await _storeStateTokenCommandHandler.Handle(new StoreStateTokenCommand(state, userId));

        var authorizeUrl =
            $"{BaseUrl}authorize?owner=user&client_id={_notionSettings.ClientId}&redirect_uri={Uri.EscapeDataString(_notionSettings.RedirectUri)}&response_type=code&state={state}";

        _logger.LogInformation("Redirecting to Notion authorize URL");

        var response = request.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(new { url = authorizeUrl });
        return response;
    }


    [Function("NotionAuthCallback")]
    public async Task<HttpResponseData> Callback(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "notion/callback")] HttpRequestData request)
    {
        _logger.LogInformation("Handling Notion OAuth callback.");

        var query = HttpUtility.ParseQueryString(request.Url.Query);
        var code = query["code"];
        var state = query["state"];

        if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(state))
        {
            return await CreateResponse(request, HttpStatusCode.BadRequest, "Missing code or state");
        }

        _logger.LogInformation("Validating state.");
        var isValidState = await _validateStateTokenQueryHandler.Handle(new ValidateStateTokenQuery(state));
        if (!isValidState)
        {
            _logger.LogWarning("Notion OAuth callback rejected because the state was invalid or expired.");
            return await CreateResponse(request, HttpStatusCode.BadRequest, "Invalid state");
        }

        await _invalidateStateTokenCommandHandler.Handle(new InvalidateStateTokenCommand(state));

        using var http = new HttpClient();
        var tokenRequest = GetTokenRequest(code);

        var tokenResponse = await http.SendAsync(tokenRequest);
        var responseContent = await tokenResponse.Content.ReadAsStringAsync();

        if (!tokenResponse.IsSuccessStatusCode)
        {
            _logger.LogError("Notion token exchange failed with status code {StatusCode}", tokenResponse.StatusCode);
            return await CreateResponse(request, HttpStatusCode.BadRequest, "Notion token exchange failed.");
        }

        var token = Newtonsoft.Json.JsonConvert.DeserializeObject<NotionTokenResponse>(responseContent);
        if (token == null)
        {
            _logger.LogError("Failed to deserialize Notion token response.");
            return await CreateResponse(request, HttpStatusCode.BadRequest, "Failed to deserialize token response.");
        }

        await _handleNewPersonalSignInCommandHandler.Handle(new HandleNewPersonalSignInCommand(token.ToModel(), _userDetails, _userDetailsSettings.PersonalMailTag, TokenType));

        _logger.LogInformation("Notion OAuth process completed successfully.");

        return await CreateResponse(request, HttpStatusCode.OK, "Notion connected. You can return to the assistant.");
    }

    private static async Task<HttpResponseData> CreateResponse(HttpRequestData request, HttpStatusCode statusCode, string message)
    {
        var response = request.CreateResponse(statusCode);
        response.Headers.Add("Content-Type", "text/plain");
        await response.WriteStringAsync(message);
        return response;
    }

    private HttpRequestMessage GetTokenRequest(string code)
    {
        return new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/token")
        {
            Content = new FormUrlEncodedContent(
            [
                new KeyValuePair<string, string>("client_id", _notionSettings.ClientId),
            new KeyValuePair<string, string>("code", code),
            new KeyValuePair<string, string>("redirect_uri", _notionSettings.RedirectUri),
            new KeyValuePair<string, string>("grant_type", "authorization_code"),
            new KeyValuePair<string, string>("client_secret", _notionSettings.ClientSecret),
        ])
        };
    }
}
