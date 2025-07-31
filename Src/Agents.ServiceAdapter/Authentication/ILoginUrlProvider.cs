namespace TheAssistant.Agents.ServiceAdapter.Authentication
{
    public interface ILoginUrlProvider
    {
        string GetLoginUrlForUser(string userId);
    }

}
