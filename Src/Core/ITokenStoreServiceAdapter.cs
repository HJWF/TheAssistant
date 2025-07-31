using TheAssistant.Core.Authentication;

namespace TheAssistant.Core
{
    public interface ITokenStoreServiceAdapter
    {
        Task StoreToken(string userId, Token tokens);
        Task<Token?> GetToken(string userId);
        Task ClearToken(string userId);
    }
}