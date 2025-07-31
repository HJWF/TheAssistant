using TheAssistant.Core.Infrastructure;

namespace TheAssistant.Core.Messaging.HandleDailyOverview
{
    public record HandleDailyOverviewCommand(DateTime TriggerTime) : ICommand;
}
