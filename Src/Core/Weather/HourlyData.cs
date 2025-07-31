namespace TheAssistant.Core.Weather
{
    public class HourlyData
    {
        public string[] Time { get; set; } = Array.Empty<string>();
        public double[] Temperature_2m { get; set; } = Array.Empty<double>();
        public double[] Apparent_Temperature { get; set; } = Array.Empty<double>();
            public double[] Precipitation_Probability { get; set; } = Array.Empty<double>();
    }

}