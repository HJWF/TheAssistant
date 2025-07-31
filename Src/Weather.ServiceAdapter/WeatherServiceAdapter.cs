using TheAssistant.Core;
using TheAssistant.Core.Weather;

namespace TheAssistant.Weather.ServiceAdapter
{
    public class WeatherServiceAdapter : IWeatherServiceAdapter
    {
        private readonly IWeatherClient _weatherClient;

        public WeatherServiceAdapter(IWeatherClient weatherClient)
        {
            _weatherClient = weatherClient;
        }

        public async Task<WeatherForecast> GetWeather(string latitude, string longitude)
        {
            var forecast = await _weatherClient.GetForecastAsync(latitude, longitude);

            if (forecast == null)
            {
                throw new Exception("Invalid forecast"); // TODO: Consider using a more specific exception type
            }

            return forecast;
        }
    }
}
