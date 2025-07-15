using TheAssistant.Weather.ServiceAdapter.Models;

namespace TheAssistant.Weather.ServiceAdapter
{
    public interface IWeatherClient
    {
        Task<WeatherForecast?> GetForecastAsync(string latitude, string longitude);
    }
}
