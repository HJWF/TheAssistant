using TheAssistant.Core.Agenda;
using TheAssistant.Core.Authentication;

namespace TheAssistant.Core
{
    public interface IAgendaServiceAdapter
    {
        Task<IEnumerable<CalendarEvent>> GetTodayEvents(string user, Token token);
    }
}