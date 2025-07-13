using FluentAssertions;
using Microsoft.Extensions.Options;

namespace TheAssistant.Messaging.ServiceAdapter.UnitTests
{
    public class SignalServiceAdapterTests
    {
        private readonly SignalServiceAdapter _signalServiceAdapter;

        public SignalServiceAdapterTests()
        {
            var options = Options.Create(new SignalSettings { BaseUrl = "http://localhost:8080", PhoneNumber = "+31630454969" });
            var httpClient = new HttpClient(new HttpClientHandler
            {
                AllowAutoRedirect = false,
                UseCookies = false
            });

            var apiClient = new SignalApiClient(httpClient, options);

            _signalServiceAdapter = new SignalServiceAdapter(apiClient, options);
        }

        [Fact]
        public async Task SendMessageAsyncShouldSendMessage()
        {
            var result = await _signalServiceAdapter.SendMessageAsync(new Core.Messaging.Message() { Content = "This is a test", To = "+31630454969" });

            result.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task ReceiveMessagesAsyncShouldReturnMessages()
        {
            var result = await _signalServiceAdapter.ReceiveMessagesAsync();
            result.Should().NotBeNullOrEmpty();
        }
    }
}