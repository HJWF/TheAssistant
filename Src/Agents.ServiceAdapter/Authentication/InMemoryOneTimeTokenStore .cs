using System.Collections.Concurrent;

namespace TheAssistant.Agents.ServiceAdapter.Authentication
{
    public class InMemoryOneTimeTokenStore : IOneTimeTokenStore
    {
        private readonly ConcurrentDictionary<string, (string userId, DateTime expiresAt)> _tokens = new();

        public void StoreToken(string token, string userId, TimeSpan ttl) => _tokens[token] = (userId, DateTime.UtcNow.Add(ttl));

        public string? GetUserIdForToken(string token)
        {
            if (_tokens.TryGetValue(token, out var entry))
            {
                if (DateTime.UtcNow <= entry.expiresAt)
                    return entry.userId;
            }

            return null;
        }

        public void InvalidateToken(string token) => _tokens.TryRemove(token, out _);
    }

}
