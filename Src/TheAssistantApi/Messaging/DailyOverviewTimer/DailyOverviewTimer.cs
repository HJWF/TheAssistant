using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using TheAssistant.Core.Infrastructure;
using TheAssistant.Core.Messaging.HandleDailyOverview;

namespace TheAssistant.TheAssistantApi.Messaging.DailyOverviewTimer;

public class DailyOverviewTimer
{
    private readonly ILogger<DailyOverviewTimer> _logger;
    private readonly ICommandHandler<HandleDailyOverviewCommand> _handler;
    

    public DailyOverviewTimer(ILogger<DailyOverviewTimer> logger, ICommandHandler<HandleDailyOverviewCommand> handler)
    {
        _logger = logger;
        _handler = handler;
    }

    [Function(nameof(DailyOverviewTimer))]
    public async Task Run([TimerTrigger("0 30 7 * * *")] TimerInfo myTimer)
    {
        _logger.LogInformation($"Daily overview timer function executed at: {DateTime.Now}");
        await _handler.Handle(new HandleDailyOverviewCommand(DateTime.Now));
    }
}