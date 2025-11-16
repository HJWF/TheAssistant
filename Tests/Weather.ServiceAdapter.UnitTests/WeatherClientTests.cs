using FluentAssertions;
using Moq;
using Moq.Protected;
using Newtonsoft.Json;
using System.Net;
using TheAssistant.Core.Weather;

namespace TheAssistant.Weather.ServiceAdapter.UnitTests
{
    public class WeatherClientTests
    {
        [Fact]
        public async Task GetForecastAsyncShouldReturnForecastWhenApiReturnsSuccess()
        {
            var forecast = new WeatherForecast
            {
                Hourly = new HourlyData { Time = new[] { "t1" }, Temperature_2m = new[] { 1.0 }, Apparent_Temperature = new[] { 2.0 }, Precipitation_Probability = new[] { 0.0 } },
                Daily = new DailyData { Time = new[] { "d1" }, Weather_Alerts = new[] { 0 } }
            };

            var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            handlerMock
               .Protected()
               .Setup<Task<HttpResponseMessage>>("SendAsync",
                   ItExpr.IsAny<HttpRequestMessage>(),
                   ItExpr.IsAny<CancellationToken>())
               .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
               {
                   Content = new StringContent(JsonConvert.SerializeObject(forecast))
               })
               .Verifiable();

            var httpClient = new HttpClient(handlerMock.Object);
            var client = new WeatherClient(httpClient);

            var result = await client.GetForecastAsync("52.0", "4.0");

            result.Should().NotBeNull();
            result!.Hourly.Time.Should().Contain("t1");

            handlerMock.Protected().Verify("SendAsync", Times.Once(), ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
        }

        [Fact]
        public async Task GetForecastAsyncShouldReturnNullWhenApiReturnsError()
        {
            var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            handlerMock
               .Protected()
               .Setup<Task<HttpResponseMessage>>("SendAsync",
                   ItExpr.IsAny<HttpRequestMessage>(),
                   ItExpr.IsAny<CancellationToken>())
               .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.BadRequest))
               .Verifiable();

            var httpClient = new HttpClient(handlerMock.Object);
            var client = new WeatherClient(httpClient);

            var result = await client.GetForecastAsync("52.0", "4.0");

            result.Should().BeNull();

            handlerMock.Protected().Verify("SendAsync", Times.Once(), ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
        }
    }
}
