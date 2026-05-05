using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Web;
using TheAssistant.Core;
using TheAssistant.Core.Infrastructure;
using TheAssistant.Core.Messaging.HandleNewSignIn;
using TheAssistant.TheAssistantApi.Login.Infrastructure;

namespace TheAssistant.TheAssistantApi.Login.Consumer;

public class Login
{
    private readonly ILogger<Login> _logger;
    private readonly ICommandHandler<HandleNewPersonalSignInCommand> _commandHandler;
    private readonly IOneTimeTokenStoreServiceAdapter _oneTimeTokenStoreServiceAdapter;
    private readonly UserDetailsSettings _userDetailsSettings;
    private readonly ConsumerSettings _consumerSettings;
    private readonly UserDetails _userDetails;
    private const string TokenType = "microsoftconsumer";
    private const string BaseUrl = "https://login.microsoftonline.com/consumers/oauth2/v2.0/";

    public Login(
        ILogger<Login> logger,
        ICommandHandler<HandleNewPersonalSignInCommand> commandHandler,
        IOptions<UserDetailsSettings> userSettings,
        IOptions<LoginSettings> LoginSettings,
        IOneTimeTokenStoreServiceAdapter oneTimeTokenStoreServiceAdapter)
    {
        _logger = logger;
        _commandHandler = commandHandler;
        _userDetailsSettings = userSettings.Value;
        _consumerSettings = LoginSettings.Value.Consumer;
        _oneTimeTokenStoreServiceAdapter = oneTimeTokenStoreServiceAdapter;
        _userDetails = new UserDetails(_userDetailsSettings.PhoneNumber, _userDetailsSettings.PersonalMailTag, _userDetailsSettings.WorkMailTag);
    }

    [Function("start")]
    public async Task<HttpResponseData> Start([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = Constants.Routes.ConsumerStart)] HttpRequestData req)
    {
        _logger.LogInformation("Starting consumer login process.");

        var query = HttpUtility.ParseQueryString(req.Url.Query);
        var loginToken = query["token"];

        if (string.IsNullOrWhiteSpace(loginToken))
        {
            return await CreateResponse(req, HttpStatusCode.BadRequest, "Missing token.");
        }

        var userId = _oneTimeTokenStoreServiceAdapter.GetUserIdForToken(loginToken);
        if (string.IsNullOrWhiteSpace(userId))
        {
            _logger.LogWarning("Consumer login start rejected because the token was invalid or expired.");
            return await CreateResponse(req, HttpStatusCode.BadRequest, "Invalid or expired token.");
        }

        _oneTimeTokenStoreServiceAdapter.InvalidateToken(loginToken);

        var state = Guid.NewGuid().ToString("N");
        _oneTimeTokenStoreServiceAdapter.StoreToken(state, userId, TimeSpan.FromMinutes(15));

        var response = req.CreateResponse(HttpStatusCode.Redirect);
        response.Headers.Add("Location", GetRedirectUri(state));

        return response;
    }

    [Function("callback")]
    public async Task<HttpResponseData> CallBack([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = Constants.Routes.ConsumerCallback)] HttpRequestData request)
    {
        var query = HttpUtility.ParseQueryString(request.Url.Query);
        var code = query["code"];
        var state = query["state"];

        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(state))
        {
            _logger.LogWarning("Missing code or state in consumer login callback.");
            return await CreateResponse(request, HttpStatusCode.BadRequest, "Missing code or state in callback request.");
        }

        var userId = _oneTimeTokenStoreServiceAdapter.GetUserIdForToken(state);
        if (string.IsNullOrWhiteSpace(userId))
        {
            _logger.LogWarning("Consumer login callback rejected because the state was invalid or expired.");
            return await CreateResponse(request, HttpStatusCode.BadRequest, "Invalid state.");
        }

        _oneTimeTokenStoreServiceAdapter.InvalidateToken(state);

        using var tokenClient = new HttpClient();
        var tokenRequest = GetTokenRequest(code);

        var tokenResponse = await tokenClient.SendAsync(tokenRequest);
        var responseContent = await tokenResponse.Content.ReadAsStringAsync();

        if (!tokenResponse.IsSuccessStatusCode)
        {
            _logger.LogError("Token request failed with status code {StatusCode}", tokenResponse.StatusCode);
            return await CreateResponse(request, tokenResponse.StatusCode, "Failed to retrieve token.");
        }

        var token = Newtonsoft.Json.JsonConvert.DeserializeObject<AzureTokenResponse>(responseContent);
        if (token == null)
        {
            _logger.LogError("Failed to deserialize token response.");
            return await CreateResponse(request, HttpStatusCode.BadRequest, "Failed to deserialize token response.");
        }

        await _commandHandler.Handle(new HandleNewPersonalSignInCommand(token.ToModel(), _userDetails, userId, TokenType));

        _logger.LogInformation("Login completed successfully for user {UserId}", userId);
        return await CreateResponse(request, HttpStatusCode.OK, "Login completed. You can return to the assistant.");
    }

    private string GetRedirectUri(string state)
    {
        var query = HttpUtility.ParseQueryString(string.Empty);
        query["client_id"] = _consumerSettings.ClientId;
        query["response_type"] = "code";
        query["redirect_uri"] = _consumerSettings.RedirectUri;
        query["response_mode"] = "query";
        query["scope"] = "offline_access Calendars.Read";
        query["state"] = state;

        return $"{BaseUrl}authorize?{query}";
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
                new KeyValuePair<string, string>("client_id", _consumerSettings.ClientId),
                new KeyValuePair<string, string>("scope", "offline_access Calendars.Read"),
                new KeyValuePair<string, string>("code", code),
                new KeyValuePair<string, string>("redirect_uri", _consumerSettings.RedirectUri),
                new KeyValuePair<string, string>("grant_type", "authorization_code"),
                new KeyValuePair<string, string>("client_secret", _consumerSettings.ClientSecret),
            ])
        };
    }
}