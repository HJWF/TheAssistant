using TheAssistant.Core.Weather;

namespace TheAssistant.Core
{
    public interface IWeatherServiceAdapter
    {
        Task<WeatherForecast> GetWeather(string latitude, string longitude);
    }
}
