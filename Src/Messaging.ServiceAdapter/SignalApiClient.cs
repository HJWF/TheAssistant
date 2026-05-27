using Microsoft.Extensions.Options;
using System.Net.Http.Json;

namespace TheAssistant.Messaging.ServiceAdapter;

public class SignalApiClient : ISignalApiClient
{
    private readonly HttpClient _client;

    public SignalApiClient(HttpClient httpClient, IOptions<SignalSettings> settings)
    {
        _client = httpClient;
        _client.BaseAddress = new Uri(settings.Value.BaseUrl);
    }

    public async Task<HttpResponseMessage> RegisterNumberAsync(string number, bool useVoice = false, string? captcha = null)
    {
        var payload = new Dictionary<string, object>();
        if (useVoice)
        {
            payload["use_voice"] = true;
        }

        if (!string.IsNullOrEmpty(captcha))
        {
            payload["captcha"] = captcha;
        }

        var content = JsonContent.Create(payload);
        var result = await _client.PostAsync($"v1/register/{number}", content);

        if (!result.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Error registering number: {result.StatusCode}");
        }

        return result;
    }

    public async Task<HttpResponseMessage> VerifyNumberAsync(string number, string code)
    {
        var result = await _client.PostAsync($"v1/register/{number}/verify/{code}", null);

        if (!result.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Error verifying number: {result.StatusCode}");
        }

        return result;
    }

    public async Task<HttpResponseMessage> SendMessageAsync(string number, IEnumerable<string> recipients, string message,
        IEnumerable<string>? base64Attachments = null, object? linkPreview = null)
    {
        var payload = new Dictionary<string, object>
        {
            ["number"] = number,
            ["recipients"] = recipients.ToList(),
            ["message"] = message
        };

        if (base64Attachments != null)
        {
            payload["base64_attachments"] = base64Attachments.ToList();
        }

        if (linkPreview != null)
        {
            payload["link_preview"] = linkPreview;
        }

        var content = JsonContent.Create(payload);
        var result = await _client.PostAsync($"v2/send", content);

        if (!result.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Error sending message: {result.StatusCode}");
        }

        return result;
    }

    public async Task<HttpResponseMessage> ReceiveMessagesAsync(string number)
    {
        var result = await _client.GetAsync($"v1/receive/{number}");

        if (!result.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Error receiving messages: {result.StatusCode}");
        }

        return result;
    }

    public async Task<HttpResponseMessage> CreateGroupAsync(string number, string name, IEnumerable<string> members)
    {
        var payload = new
        {
            name,
            members = members.ToList()
        };

        var content = JsonContent.Create(payload);
        var result = await _client.PostAsync($"v1/groups/{number}", content);

        if (!result.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Error creating group: {result.StatusCode}");
        }

        return result;
    }

    public async Task<HttpResponseMessage> ListGroupsAsync(string number)
    {
        var result = await _client.GetAsync($"v1/groups/{number}");

        if (!result.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Error listing groups: {result.StatusCode}");
        }

        return result;
    }

    public async Task<HttpResponseMessage> DeleteGroupAsync(string number, string groupId)
    {
        var result = await _client.DeleteAsync($"v1/groups/{number}/{groupId}");

        if (!result.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Error deleting group: {result.StatusCode}");
        }

        return result;
    }

    public async Task<HttpResponseMessage> GetQrCodeLinkAsync(string deviceName)
    {
        var encoded = Uri.EscapeDataString(deviceName);
        var result = await _client.GetAsync($"v1/qrcodelink?device_name={encoded}");

        if (!result.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Error getting QR code link: {result.StatusCode}");
        }

        return result;
    }
}
