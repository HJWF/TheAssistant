
namespace TheAssistant.Messaging.ServiceAdapter
{
    public interface ISignalApiClient
    {
        Task<HttpResponseMessage> CreateGroupAsync(string number, string name, IEnumerable<string> members);
        Task<HttpResponseMessage> DeleteGroupAsync(string number, string groupId);
        Task<HttpResponseMessage> GetQrCodeLinkAsync(string deviceName);
        Task<HttpResponseMessage> ListGroupsAsync(string number);
        Task<HttpResponseMessage> ReceiveMessagesAsync(string number);
        Task<HttpResponseMessage> RegisterNumberAsync(string number, bool useVoice = false, string? captcha = null);
        Task<HttpResponseMessage> SendMessageAsync(string number, IEnumerable<string> recipients, string message, IEnumerable<string>? base64Attachments = null, object? linkPreview = null);
        Task<HttpResponseMessage> VerifyNumberAsync(string number, string code);
    }
}