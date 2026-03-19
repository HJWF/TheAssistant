using Microsoft.Extensions.AI;

namespace TheAssistant.Agents.ServiceAdapter
{
    public interface ITokenUsageTracker
    {
        void Initialize();
        void Track(UsageDetails? usage);
        (long InputTokens, long OutputTokens) GetTotals();
    }
}
