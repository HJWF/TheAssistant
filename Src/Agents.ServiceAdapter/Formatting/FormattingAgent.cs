using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Newtonsoft.Json;

namespace TheAssistant.Agents.ServiceAdapter.Formatting
{
    public class FormattingAgent : IFormattingAgent
    {
        private readonly Kernel _kernel;

        public FormattingAgent(Kernel kernel)
        {
            _kernel = kernel;
        }

        [KernelFunction]
        public async Task<string> HandleAsync(List<AgentResponse> agentResponses)
        {
            if (agentResponses == null || agentResponses.Count == 0)
            {
                return AgentConstants.SorryMessage;
            }
            if (agentResponses.Count == 1)
            {
                return agentResponses[0].Content ?? AgentConstants.SorryMessage;
            }

            var chat = _kernel.GetRequiredService<IChatCompletionService>();

            var chatHistory = new ChatHistory();
            chatHistory.AddSystemMessage("""
            You are a smart formatter. You receive a list of messages from different assistant agents.

            Your task is to combine them into a friendly, clear, and well-organized message for the user.

            Each message comes with the name of the agent and its response content. Format it cleanly using section headers or emojis.

            Avoid repeating agent names in each sentence. Only include what’s relevant.
            """);

            chatHistory.AddUserMessage(JsonConvert.SerializeObject(agentResponses));

            var formatted = await chat.GetChatMessageContentAsync(chatHistory);
            return formatted.Content ?? AgentConstants.SorryMessage;
        }
    }
}
