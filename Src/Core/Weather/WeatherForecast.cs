namespace TheAssistant.Core.Weather
{
    public class WeatherForecast
    {
        public HourlyData Hourly { get; set; } = new HourlyData();
        public DailyData Daily { get; set; } = new DailyData();
    }

}