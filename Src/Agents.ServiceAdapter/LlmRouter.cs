using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace TheAssistant.Agents.ServiceAdapter
{
    public class LlmRouter : IRouter
    {
        private readonly Kernel _kernel;

        public LlmRouter(Kernel kernel)
        {
            _kernel = kernel;
        }

        public async Task<string> RouteAsync(string input)
        {
            var chat = _kernel.GetRequiredService<IChatCompletionService>();

            var chatHistory = new ChatHistory();
            chatHistory.AddUserMessage($"""
                Decide which agent should handle this input:
                - Agenda

                Just return the agent name. Nothing else. Only use the exact name of the agent.

                Input: {input}
            """);

            var result = await chat.GetChatMessageContentAsync(chatHistory);

            return result.Content?.Trim() ?? "No agend found";
        }
    }

}
