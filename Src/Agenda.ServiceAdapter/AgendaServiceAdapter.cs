using TheAssistant.Core;

namespace TheAssistant.Agenda.ServiceAdapter
{
    public class AgendaServiceAdapter : IAgendaServiceAdapter
    {
        public Task<string> GetMeetings(DateTime date)
        {
            // Fetch from Microsoft Graph, format output
            return Task.FromResult("Agenda: 09:00 Daily Standup...");
        }
    }
}
