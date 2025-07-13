namespace TheAssistant.Core
{
    public interface IAgendaServiceAdapter
    {
        Task<string> GetMeetings(DateTime date);
    }
}