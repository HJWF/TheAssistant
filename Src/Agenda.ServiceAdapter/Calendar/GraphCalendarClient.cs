using Newtonsoft.Json;
using System.Net.Http.Headers;
using TheAssistant.Agenda.ServiceAdapter.Calendar.Models;
using TheAssistant.Core.Agenda;

namespace TheAssistant.Agenda.ServiceAdapter.Calendar
{
    public class GraphCalendarClient
    {
        private readonly HttpClient _httpClient;

        public GraphCalendarClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<IEnumerable<CalendarEvent>> GetTodayEventsAsync(string accessToken)
        {
            var now = DateTime.UtcNow;
            var startOfDay = now.Date;
            var endOfDay = startOfDay.AddDays(1).AddSeconds(-1);

            var url = $"https://graph.microsoft.com/v1.0/me/calendarview?startDateTime={startOfDay:O}&endDateTime={endOfDay:O}&$orderby=start/dateTime";

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            request.Headers.Add("Prefer", ["outlook.timezone=\"Europe/Amsterdam\""]);

            var response = await _httpClient.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Failed to get calendar events. Status: {response.StatusCode}, Body: {responseContent}");
            }

            var calendarEvents = JsonConvert.DeserializeObject<CalendarEventResponseWrapper>(responseContent);

            var results = new List<CalendarEvent>();

            if (calendarEvents?.value == null || !calendarEvents.value.Any())
            {
                return results;
            }

            foreach (var calendarEvent in calendarEvents.value)
            {
                results.Add(new CalendarEvent(calendarEvent.subject, calendarEvent.start.dateTime, calendarEvent.end.dateTime, calendarEvent.location.displayName, calendarEvent.organizer.emailAddress.name, calendarEvent.isAllDay));
            }

            return results;
        }
    }

}
