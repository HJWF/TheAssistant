namespace TheAssistant.Agents.ServiceAdapter.Authentication
{
    public interface IOneTimeTokenStore
    {
        void StoreToken(string token, string userId, TimeSpan ttl);
        string? GetUserIdForToken(string token);
        void InvalidateToken(string token);
    }

}
