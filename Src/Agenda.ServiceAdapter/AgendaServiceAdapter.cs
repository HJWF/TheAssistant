using Microsoft.Extensions.Logging;
using TheAssistant.Agenda.ServiceAdapter.Graph;
using TheAssistant.Core;
using TheAssistant.Core.Agenda;
using TheAssistant.Core.Authentication;

namespace TheAssistant.Agenda.ServiceAdapter
{
    public class AgendaServiceAdapter : IAgendaServiceAdapter
    {
        private readonly GraphCalendarClient _graphCalendarClient;
        private readonly ILogger<AgendaServiceAdapter> _logger;

        public AgendaServiceAdapter(HttpClient httpClient, ILogger<AgendaServiceAdapter> logger)
        {
            _graphCalendarClient = new GraphCalendarClient(httpClient);
            _logger = logger;
        }

        public async Task<IEnumerable<CalendarEvent>> GetTodayEvents(string user, Token token)
        {
            _logger.LogInformation("Getting today's events for user {UserId}", user);

            if (token == null || token.ExpiresAt <= DateTime.Now || string.IsNullOrWhiteSpace(token.AccessToken) || string.IsNullOrWhiteSpace(token.RefreshToken))
            {
                _logger.LogWarning("Token is invalid or expired for user {UserId}", user);
                throw new UnauthorizedAccessException("Invalid or expired token.");
            }

            return await _graphCalendarClient.GetTodayEventsAsync(token.AccessToken);
        }
    }
}
