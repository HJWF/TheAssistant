using Microsoft.Extensions.AI;

namespace TheAssistant.Agents.ServiceAdapter
{
    public interface IChatClientFactory
    {
        IChatClient GetClient(string modelKey);
    }
}
