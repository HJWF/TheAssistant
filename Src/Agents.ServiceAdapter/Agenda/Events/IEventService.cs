using TheAssistant.Core.Authentication;

namespace TheAssistant.Agents.ServiceAdapter.Agenda.Events
{
    public interface IEventService
    {
        Task<string> GetBirthdays(string date, Token token);
        Task<string> GetMeetings(string date, Token token);
        Task<string> GetTodaysEvents(string userId, Token token);
    }
}