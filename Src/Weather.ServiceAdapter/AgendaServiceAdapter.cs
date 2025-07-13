using TheAssistant.Core;

namespace TheAssistant.Weather.ServiceAdapter
{
    public class WeatherServiceAdapter : IWeatherServiceAdapter
    {
        public Task<string> GetWeather(DateTime date)
        {
            // Placeholder implementation: returns a simple weather info string
            var weatherInfo = $"Weather for {date:yyyy-MM-dd}: Sunny, 25°C";
            return Task.FromResult(weatherInfo);
        }
    }
}
