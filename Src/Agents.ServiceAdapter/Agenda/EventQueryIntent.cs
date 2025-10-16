namespace TheAssistant.Agents.ServiceAdapter.Agenda
{
    internal record EventQueryIntent(TimeSpan? Start, TimeSpan? End, string? DayReference);
}
