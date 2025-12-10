using Microsoft.Extensions.AI;
using OpenAI.Chat;

namespace TheAssistant.Agents.ServiceAdapter.AI
{
    public static class ChatClientExtensions
    {
        public static IChatClient AsChatClient(this ChatClient chatClient)
        {
            return new AzureOpenAIChatClient(chatClient);
        }
    }
}
