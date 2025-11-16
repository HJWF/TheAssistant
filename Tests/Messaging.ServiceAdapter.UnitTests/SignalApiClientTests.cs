using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using Newtonsoft.Json.Linq;
using System.Net;

namespace TheAssistant.Messaging.ServiceAdapter.UnitTests
{
    public class SignalApiClientTests
    {
        private readonly IOptions<SignalSettings> _options;

        public SignalApiClientTests()
        {
            _options = Options.Create(new SignalSettings { BaseUrl = "http://localhost/", PhoneNumber = "+31630454969" });
        }

        [Fact]
        public async Task RegisterNumberAsyncShouldPostToRegisterEndpointAndReturnResponse()
        {
            var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);

            handlerMock
               .Protected()
               .Setup<Task<HttpResponseMessage>>("SendAsync",
                   ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Post && req.RequestUri.PathAndQuery.StartsWith("/v1/register/")),
                   ItExpr.IsAny<CancellationToken>())
               .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
               {
                   Content = new StringContent("registered")
               })
               .Verifiable();

            var httpClient = new HttpClient(handlerMock.Object) { BaseAddress = new System.Uri(_options.Value.BaseUrl) };
            var client = new SignalApiClient(httpClient, _options);

            var response = await client.RegisterNumberAsync(_options.Value.PhoneNumber);
            var content = await response.Content.ReadAsStringAsync();

            response.IsSuccessStatusCode.Should().BeTrue();
            content.Should().Be("registered");

            handlerMock.Protected().Verify("SendAsync", Times.Once(),
                ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
        }

        [Fact]
        public async Task SendMessageAsyncShouldPostToSendEndpointWithExpectedPayload()
        {
            HttpRequestMessage? capturedRequest = null;

            var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            handlerMock
               .Protected()
               .Setup<Task<HttpResponseMessage>>("SendAsync",
                   ItExpr.IsAny<HttpRequestMessage>(),
                   ItExpr.IsAny<CancellationToken>())
               .ReturnsAsync((HttpRequestMessage req, CancellationToken _) =>
               {
                   capturedRequest = req;
                   return new HttpResponseMessage(HttpStatusCode.Accepted)
                   {
                       Content = new StringContent("sent")
                   };
               })
               .Verifiable();

            var httpClient = new HttpClient(handlerMock.Object) { BaseAddress = new System.Uri(_options.Value.BaseUrl) };
            var client = new SignalApiClient(httpClient, _options);

            var recipients = new[] { "+31600000000" };
            var message = "hello world";

            var response = await client.SendMessageAsync(_options.Value.PhoneNumber, recipients, message);
            var content = await response.Content.ReadAsStringAsync();

            response.StatusCode.Should().Be(HttpStatusCode.Accepted);
            content.Should().Be("sent");

            capturedRequest.Should().NotBeNull();
            capturedRequest!.Method.Should().Be(HttpMethod.Post);
            capturedRequest.RequestUri!.PathAndQuery.Should().Be("/v2/send");

            var requestBody = await capturedRequest.Content!.ReadAsStringAsync();

            var json = JObject.Parse(requestBody);
            json["number"]!.Value<string>().Should().Be(_options.Value.PhoneNumber);
            json["message"]!.Value<string>().Should().Be(message);
            var recipientsArray = json["recipients"]!.Values<string>().ToList();
            recipientsArray.Should().Contain(recipients[0]);

            handlerMock.Protected().Verify("SendAsync", Times.Once(), ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
        }

        [Fact]
        public async Task ReceiveMessagesAsyncShouldCallReceiveEndpointAndReturnResponse()
        {
            var expectedJson = "[ { \"dummy\": \"value\" } ]";

            var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            handlerMock
               .Protected()
               .Setup<Task<HttpResponseMessage>>("SendAsync",
                   ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Get && req.RequestUri!.PathAndQuery.StartsWith($"/v1/receive/")),
                   ItExpr.IsAny<CancellationToken>())
               .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
               {
                   Content = new StringContent(expectedJson)
               })
               .Verifiable();

            var httpClient = new HttpClient(handlerMock.Object) { BaseAddress = new System.Uri(_options.Value.BaseUrl) };
            var client = new SignalApiClient(httpClient, _options);

            var response = await client.ReceiveMessagesAsync(_options.Value.PhoneNumber);
            var content = await response.Content.ReadAsStringAsync();

            response.IsSuccessStatusCode.Should().BeTrue();
            content.Should().Be(expectedJson);

            handlerMock.Protected().Verify("SendAsync", Times.Once(), ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
        }

        [Fact]
        public async Task GetQrCodeLinkAsyncShouldEncodeDeviceNameInQueryString()
        {
            HttpRequestMessage? capturedRequest = null;

            var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            handlerMock
               .Protected()
               .Setup<Task<HttpResponseMessage>>("SendAsync",
                   ItExpr.IsAny<HttpRequestMessage>(),
                   ItExpr.IsAny<CancellationToken>())
               .ReturnsAsync((HttpRequestMessage req, CancellationToken _) =>
               {
                   capturedRequest = req;
                   return new HttpResponseMessage(HttpStatusCode.OK)
                   {
                       Content = new StringContent("qrcode")
                   };
               })
               .Verifiable();

            var httpClient = new HttpClient(handlerMock.Object) { BaseAddress = new System.Uri(_options.Value.BaseUrl) };
            var client = new SignalApiClient(httpClient, _options);

            var deviceName = "My Device 1";

            var response = await client.GetQrCodeLinkAsync(deviceName);
            var content = await response.Content.ReadAsStringAsync();

            response.IsSuccessStatusCode.Should().BeTrue();
            content.Should().Be("qrcode");

            capturedRequest.Should().NotBeNull();
            capturedRequest!.Method.Should().Be(HttpMethod.Get);
            capturedRequest.RequestUri!.Query.Should().Contain("device_name=");
            capturedRequest.RequestUri!.Query.Should().Contain(Uri.EscapeDataString(deviceName));

            handlerMock.Protected().Verify("SendAsync", Times.Once(), ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
        }
    }
}
