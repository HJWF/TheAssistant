using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using System.Net;
using TheAssistant.Core.Messaging;

namespace TheAssistant.Messaging.ServiceAdapter.UnitTests
{
    public class SignalServiceAdapterTests
    {
        private readonly SignalServiceAdapter _signalServiceAdapter;
        private readonly Mock<ISignalApiClient> _apiClientMock;
        private const string TestPhoneNumber = "+31600000000";

        public SignalServiceAdapterTests()
        {
            var settings = new SignalSettings { BaseUrl = "http://localhost:8080", PhoneNumber = TestPhoneNumber };
            var options = Options.Create(settings);

            _apiClientMock = new Mock<ISignalApiClient>(MockBehavior.Strict);
            _signalServiceAdapter = new SignalServiceAdapter(_apiClientMock.Object, options);
        }

        [Fact]
        public async Task SendMessageAsyncShouldReturnResponseContentWhenApiSucceeds()
        {
            var message = new Message("This is a test", TestPhoneNumber);
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("ok-response")
            };

            _apiClientMock.Setup(x => x.SendMessageAsync(It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), It.IsAny<string>(), null, null))
                .ReturnsAsync(response)
                .Verifiable();

            var result = await _signalServiceAdapter.SendMessageAsync(message);

            result.Should().Be("ok-response");
            _apiClientMock.Verify(x => x.SendMessageAsync(It.IsAny<string>(), It.Is<IEnumerable<string>>(r => r != null && r.Contains(message.To)), message.Content, null, null), Times.Once);
        }

        [Fact]
        public async Task SendMessageAsyncShouldThrowWhenApiFails()
        {
            var message = new Message("This is a test", TestPhoneNumber);
            var response = new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                ReasonPhrase = "Error"
            };

            _apiClientMock.Setup(x => x.SendMessageAsync(It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), It.IsAny<string>(), null, null))
                .ReturnsAsync(response)
                .Verifiable();

            await (_signalServiceAdapter.Invoking(s => s.SendMessageAsync(message))).Should().ThrowAsync<Exception>().WithMessage("Failed to send message: Error");

            _apiClientMock.Verify(x => x.SendMessageAsync(It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), It.IsAny<string>(), null, null), Times.Once);
        }

        [Fact]
        public async Task ReceiveMessagesAsyncShouldReturnMessagesWhenApiSucceeds()
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("[]")
            };

            _apiClientMock.Setup(x => x.ReceiveMessagesAsync(It.IsAny<string>()))
                .ReturnsAsync(response)
                .Verifiable();

            var result = await _signalServiceAdapter.ReceiveMessagesAsync();

            result.Should().NotBeNull();
            _apiClientMock.Verify(x => x.ReceiveMessagesAsync(It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task ReceiveMessagesAsyncShouldThrowWhenApiFails()
        {
            var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                ReasonPhrase = "BadRequest"
            };

            _apiClientMock.Setup(x => x.ReceiveMessagesAsync(It.IsAny<string>()))
                .ReturnsAsync(response)
                .Verifiable();

            await (_signalServiceAdapter.Invoking(s => s.ReceiveMessagesAsync())).Should().ThrowAsync<Exception>().WithMessage("Failed to receive message: BadRequest");

            _apiClientMock.Verify(x => x.ReceiveMessagesAsync(It.IsAny<string>()), Times.Once);
        }
    }
}