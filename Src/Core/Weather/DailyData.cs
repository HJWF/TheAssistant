namespace TheAssistant.Core.Weather
{
    public class DailyData
    {
        public string[] Time { get; set; } = Array.Empty<string>();
        public int[] Weather_Alerts { get; set; } = Array.Empty<int>();
    }

}