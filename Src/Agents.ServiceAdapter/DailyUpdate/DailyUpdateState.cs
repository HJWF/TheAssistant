namespace TheAssistant.Agents.ServiceAdapter.DailyUpdate
{
    public class DailyUpdateState
    {
        public bool HasAgenda { get; set; }
        public bool HasWeather { get; set; }
        public string? AgendaContent { get; set; }
        public string? WeatherContent { get; set; }

        public bool IsComplete => HasAgenda && HasWeather;
    }

}
