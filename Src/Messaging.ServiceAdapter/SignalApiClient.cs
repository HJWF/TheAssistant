using Microsoft.Extensions.Options;
using System.Net.Http.Json;

namespace TheAssistant.Messaging.ServiceAdapter
{
    public class SignalApiClient : ISignalApiClient
    {
        private readonly HttpClient _client;

        public SignalApiClient(HttpClient httpClient, IOptions<SignalSettings> settings)
        {
            _client = httpClient;
            _client.BaseAddress = new Uri(settings.Value.BaseUrl);
        }

        public Task<HttpResponseMessage> RegisterNumberAsync(string number, bool useVoice = false, string? captcha = null)
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
            return _client.PostAsync($"v1/register/{number}", content);
        }

        public Task<HttpResponseMessage> VerifyNumberAsync(string number, string code)
        {
            return _client.PostAsync($"v1/register/{number}/verify/{code}", null);
        }

        public Task<HttpResponseMessage> SendMessageAsync(string number, IEnumerable<string> recipients, string message,
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
            return _client.PostAsync($"v2/send", content);
        }

        public Task<HttpResponseMessage> ReceiveMessagesAsync(string number)
        {
            return _client.GetAsync($"v1/receive/{number}");
        }

        public Task<HttpResponseMessage> CreateGroupAsync(string number, string name, IEnumerable<string> members)
        {
            var payload = new
            {
                name,
                members = members.ToList()
            };

            var content = JsonContent.Create(payload);
            return _client.PostAsync($"v1/groups/{number}", content);
        }

        public Task<HttpResponseMessage> ListGroupsAsync(string number)
        {
            return _client.GetAsync($"v1/groups/{number}");
        }

        public Task<HttpResponseMessage> DeleteGroupAsync(string number, string groupId)
        {
            return _client.DeleteAsync($"v1/groups/{number}/{groupId}");
        }

        public Task<HttpResponseMessage> GetQrCodeLinkAsync(string deviceName)
        {
            var encoded = Uri.EscapeDataString(deviceName);
            return _client.GetAsync($"v1/qrcodelink?device_name={encoded}");
        }
    }
}
