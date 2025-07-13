namespace TheAssistant.Core
{
    public interface IWeatherServiceAdapter
    {
        Task<string> GetWeather(DateTime date);
    }
}
