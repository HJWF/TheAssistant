using FluentAssertions;
using Moq;
using TheAssistant.Core.Weather;

namespace TheAssistant.Weather.ServiceAdapter.UnitTests;

public class WeatherServiceAdapterTests
{
    [Fact]
    public async Task GetWeatherShouldReturnForecastWhenClientReturnsForecast()
    {
        var forecast = new WeatherForecast();
        var weatherClientMock = new Mock<IWeatherClient>(MockBehavior.Strict);
        weatherClientMock.Setup(x => x.GetForecastAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(forecast).Verifiable();

        var adapter = new WeatherServiceAdapter(weatherClientMock.Object);

        var result = await adapter.GetWeather("52.0", "4.0");

        result.Should().BeSameAs(forecast);
        weatherClientMock.Verify(x => x.GetForecastAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task GetWeatherShouldThrowWhenClientReturnsNull()
    {
        var weatherClientMock = new Mock<IWeatherClient>(MockBehavior.Strict);
        weatherClientMock.Setup(x => x.GetForecastAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync((WeatherForecast?)null).Verifiable();

        var adapter = new WeatherServiceAdapter(weatherClientMock.Object);

        await adapter.Invoking(a => a.GetWeather("52.0", "4.0")).Should().ThrowAsync<Exception>().WithMessage("Invalid forecast");

        weatherClientMock.Verify(x => x.GetForecastAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }
}
