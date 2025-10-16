using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
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
            var eventsAsJson = string.Empty;

            var cacheKey = $"{CACHE_KEY_PREFIX}{userId}_{DateTime.Today:yyyyMMdd}";

            if (_cache.TryGetValue(cacheKey, out string? cachedEvents))
            {
                eventsAsJson = cachedEvents;
            }

            if (!string.IsNullOrEmpty(eventsAsJson))
            {
                return eventsAsJson;
            }

            var events = await _agendaServiceAdapter.GetTodayEvents(userId, token);

            eventsAsJson = JsonConvert.SerializeObject(events);

            _cache.Set(cacheKey, eventsAsJson, CACHE_DURATION);
            return eventsAsJson;
        }

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
