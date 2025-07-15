namespace TheAssistant.Weather.ServiceAdapter.Models
{
    public class WeatherForecast
    {
        public HourlyData Hourly { get; set; }
        public DailyData Daily { get; set; }
    }

}