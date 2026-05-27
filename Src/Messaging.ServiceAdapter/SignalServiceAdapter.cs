using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using TheAssistant.Core;
using TheAssistant.Core.Messaging;
using TheAssistant.Messaging.ServiceAdapter.Models;

namespace TheAssistant.Messaging.ServiceAdapter;

public class SignalServiceAdapter : IMessageServiceAdapter
{
    private readonly SignalSettings _settings;
    private readonly ISignalApiClient _signalApiClient;
    private readonly ILogger<SignalServiceAdapter> _logger;

		public SignalServiceAdapter(ISignalApiClient signalApiClient, IOptions<SignalSettings> settings, ILogger<SignalServiceAdapter> logger)
    {
        _settings = settings.Value;
        _signalApiClient = signalApiClient;
        _logger = logger;
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

        var receivedMessages = new List<ReceiveResponse>();

        var sanitizedContent = SanitizeSignalResponse(responseContent);

        try
        {
            var array = JArray.Parse(sanitizedContent);
            foreach (var token in array)
            {
                try
                {
                    var msg = token.ToObject<ReceiveResponse>();
                    if (msg != null)
                        receivedMessages.Add(msg);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Skipping malformed message at {Path}: {Error}", token.Path, ex.Message);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse messages response");
            return [];
        }

        var messages = receivedMessages?
            .Select(r => r.Envelope.SyncMessage.SentMessage)
            .Where(m => m != null)
            .Cast<Models.SentMessage>();

        return messages?.ToModel() ?? [];
    }

    // Signal CLI occasionally writes unquoted Java/Android exception strings
    // (e.g. java.lang.NullPointerException, android.os.DeadObjectException) directly
    // into the JSON payload. We scan character-by-character, tracking string context,
    // and replace any out-of-string alphabetic token that is NOT a valid JSON keyword
    // (true, false, null) with null.
    private static string SanitizeSignalResponse(string json)
    {
        var result = new System.Text.StringBuilder(json.Length);
        var inString = false;
        var escaped = false;

        for (int i = 0; i < json.Length; i++)
        {
            char c = json[i];

            if (escaped)
            {
                result.Append(c);
                escaped = false;
                continue;
            }

            if (c == '\\' && inString)
            {
                result.Append(c);
                escaped = true;
                continue;
            }

            if (c == '"')
            {
                inString = !inString;
                result.Append(c);
                continue;
            }

            if (!inString && char.IsLetter(c))
            {
                var remaining = json.AsSpan(i);
                string? keyword = remaining.StartsWith("null") ? "null"
                    : remaining.StartsWith("true") ? "true"
                    : remaining.StartsWith("false") ? "false"
                    : null;

                if (keyword != null)
                {
                    int end = i + keyword.Length;
                    if (end >= json.Length || json[end] == ',' || json[end] == '}' || json[end] == ']' || char.IsWhiteSpace(json[end]))
                    {
                        result.Append(keyword);
                        i += keyword.Length - 1;
                        continue;
                    }
                }

                result.Append("null");
                while (i < json.Length - 1 && json[i + 1] != ',' && json[i + 1] != '}' && json[i + 1] != ']' && json[i + 1] != '\r' && json[i + 1] != '\n')
                    i++;
                continue;
            }

            result.Append(c);
        }

        return result.ToString();
    }
}
