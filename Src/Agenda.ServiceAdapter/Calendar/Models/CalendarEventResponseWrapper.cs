namespace TheAssistant.Agenda.ServiceAdapter.Calendar.Models
{
    public class CalendarEventResponseWrapper
    {
        public CalendarEventResponse[] value { get; set; } = Array.Empty<CalendarEventResponse>();
        public string odataContext { get; set; } = string.Empty;
    }


}
