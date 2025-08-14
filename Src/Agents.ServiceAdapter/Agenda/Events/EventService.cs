using Microsoft.Extensions.Caching.Memory;
using TheAssistant.Core;
using TheAssistant.Core.Agenda;
using TheAssistant.Core.Authentication;

namespace TheAssistant.Agents.ServiceAdapter.Agenda.Events
{
    public class EventService : IEventService
    {
        private readonly IMemoryCache _cache;
        private const string CACHE_KEY_PREFIX = "calendar_events_";
        private readonly IAgendaServiceAdapter _agendaServiceAdapter;
        private static readonly TimeSpan CACHE_DURATION = TimeSpan.FromMinutes(5);

        public EventService(IMemoryCache cache, IAgendaServiceAdapter agendaServiceAdapter)
        {
            _cache = cache;
            _agendaServiceAdapter = agendaServiceAdapter;
        }

        public async Task<string> GetTodaysEvents(string userId, Token token)
        {
            var cacheKey = $"{CACHE_KEY_PREFIX}{userId}_{DateTime.Today:yyyyMMdd}";

            if (_cache.TryGetValue(cacheKey, out string? cachedEvents))
            {
                return cachedEvents;
            }

            var events = await _agendaServiceAdapter.GetTodayEvents(userId, token);
            var formattedEvents = FormatEvents(events);

            _cache.Set(cacheKey, formattedEvents, CACHE_DURATION);
            return formattedEvents;
        }

        private static string FormatEvents(IEnumerable<CalendarEvent> events) =>
            string.Join("\n", events.Select(e =>
            {
                var time = e.AllDay ? "All day" : $"{e.Start:HH:mm}-{e.End:HH:mm}";
                var subject = e.Subject ?? "";
                var location = string.IsNullOrWhiteSpace(e.Location) ? "" : $" - {e.Location}";
                var organizer = string.IsNullOrWhiteSpace(e.Organizer) ? "" : $" ({e.Organizer})";
                return $"{time} {subject}{location}{organizer}";
            }));

        public async Task<string> GetMeetings(string date, Token token)
        {
            //TODO: implement actual implementation for meetings
            return await GetTodaysEvents(date, token);
        }

        public async Task<string> GetBirthdays(string date, Token token)
        {
            //TODO: implement actual implementation for birthdays
            return await GetTodaysEvents(date, token);
        }
    }
}
