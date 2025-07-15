namespace TheAssistant.Weather.ServiceAdapter.Models
{
    public class HourlyData
    {
        public string[] Time { get; set; }
        public double[] Temperature_2m { get; set; }
        public double[] Apparent_Temperature { get; set; }
        public double[] Precipitation_Probability { get; set; }
    }

}