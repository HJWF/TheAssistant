using TheAssistant.Weather.ServiceAdapter.Models;

namespace TheAssistant.Core
{
    public interface IWeatherServiceAdapter
    {
        Task<WeatherForecast> GetWeather(string latitude, string longitude);
    }
}
