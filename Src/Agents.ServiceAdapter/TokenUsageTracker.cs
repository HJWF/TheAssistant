using Microsoft.Extensions.AI;

namespace TheAssistant.Agents.ServiceAdapter;

public class TokenUsageTracker : ITokenUsageTracker
{
    private static readonly AsyncLocal<TokenAccumulator?> _current = new();

    public void Initialize()
    {
        _current.Value = new TokenAccumulator();
    }

    public void Track(UsageDetails? usage)
    {
        _current.Value?.Add(usage);
    }

    public (long InputTokens, long OutputTokens) GetTotals()
    {
        return _current.Value?.GetTotals() ?? (0, 0);
    }

    private sealed class TokenAccumulator
    {
        private long _inputTokens;
        private long _outputTokens;

        public void Add(UsageDetails? usage)
        {
            if (usage is null) return;
            Interlocked.Add(ref _inputTokens, usage.InputTokenCount ?? 0);
            Interlocked.Add(ref _outputTokens, usage.OutputTokenCount ?? 0);
        }

        public (long InputTokens, long OutputTokens) GetTotals() =>
            (_inputTokens, _outputTokens);
    }
}
