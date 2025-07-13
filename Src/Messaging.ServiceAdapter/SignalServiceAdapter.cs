using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using TheAssistant.Core;
using TheAssistant.Core.Messaging;
using TheAssistant.Messaging.ServiceAdapter.Models;

namespace TheAssistant.Messaging.ServiceAdapter
{
    public class SignalServiceAdapter : IMessageServiceAdapter
    {
        private readonly SignalSettings _settings;
        private readonly ISignalApiClient _signalApiClient;

		public SignalServiceAdapter(ISignalApiClient signalApiClient, IOptions<SignalSettings> settings)
        {
            _settings = settings.Value;
            _signalApiClient = signalApiClient;
		}

        public async Task<string> SendMessageAsync(Message message)
        {
            List<string> recipients = !string.IsNullOrEmpty(message.To) ? [message.To] : [_settings.PhoneNumber];

            var result = await _signalApiClient.SendMessageAsync(_settings.PhoneNumber, recipients, message.Content);
            var responseContent = await result.Content.ReadAsStringAsync();

            if (!result.IsSuccessStatusCode)
            {
                throw new Exception($"Failed to send message: {result.ReasonPhrase}"); //TODO: Use a more specific exception type
            }

            return responseContent;

        }

        public async Task<IEnumerable<Core.Messaging.SentMessage>> ReceiveMessagesAsync()
        {
            var result = await _signalApiClient.ReceiveMessagesAsync(_settings.PhoneNumber);
            var responseContent = await result.Content.ReadAsStringAsync();

            if (!result.IsSuccessStatusCode)
            {
                throw new Exception($"Failed to receive message: {result.ReasonPhrase}"); //TODO: Use a more specific exception type
            }

            var receivedMessages = JsonConvert.DeserializeObject<IEnumerable<ReceiveResponse>>(responseContent);

            var messages = receivedMessages?
                .Select(r => r.Envelope.SyncMessage.SentMessage)
                .Where(m => m != null)
                .Cast<Models.SentMessage>();

            return messages?.ToModel() ?? [];
        }
    }
}
