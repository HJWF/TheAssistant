
using Newtonsoft.Json;
using TheAssistant.Core.Weather;

namespace TheAssistant.Weather.ServiceAdapter
{
    public class WeatherClient : IWeatherClient
    {
        private readonly HttpClient _httpClient;
        private const string BaseUrl = "https://api.open-meteo.com/v1/forecast";

        public WeatherClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<WeatherForecast?> GetForecastAsync(string latitude, string longitude)
        {
            var url = $"{BaseUrl}?" +
                      $"latitude={latitude}&longitude={longitude}&hourly=temperature_2m,apparent_temperature,precipitation_probability,weathercode&timezone=Europe/Amsterdam";

            var response = await _httpClient.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return JsonConvert.DeserializeObject<WeatherForecast>(content);
        }
    }
}
