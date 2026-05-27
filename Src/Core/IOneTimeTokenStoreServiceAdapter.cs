namespace TheAssistant.Core;

public interface IOneTimeTokenStoreServiceAdapter
{
    void StoreToken(string token, string userId, TimeSpan ttl);
    string? GetUserIdForToken(string token);
    void InvalidateToken(string token);
}
