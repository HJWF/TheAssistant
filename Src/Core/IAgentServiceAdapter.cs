using TheAssistant.Core.Messaging;

namespace TheAssistant.Core
{
    public interface IAgentServiceAdapter
    {
        Task<string> HandleMessageAsync(Message message);
    }
}
