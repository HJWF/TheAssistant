using TheAssistant.Core.Weather;

namespace TheAssistant.Weather.ServiceAdapter
{
    public interface IWeatherClient
    {
        Task<WeatherForecast?> GetForecastAsync(string latitude, string longitude);
    }
}
