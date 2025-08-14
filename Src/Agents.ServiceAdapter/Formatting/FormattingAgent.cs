using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Newtonsoft.Json;

namespace TheAssistant.Agents.ServiceAdapter.Formatting
{
    public class FormattingAgent : IFormattingAgent
    {
        private readonly Kernel _kernel;

        public static string Name => AgentConstants.Names.Formatting;

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
                You are a smart formatter.

                You receive a list of messages from different assistant agents.

                Your task is to combine these into one clean, user-friendly message.

                Follow these rules:

                1. Use clear section headers or emojis to group content by topic.
                2. Do not repeat agent names in each sentence.
                3. Only include relevant, useful content from the messages.
                4. Keep line length under 50 characters.
                5. Avoid using vertical bars (|), quotes, or bullets.
                6. Use this format for calendar items:  
                   08:30–09:30 Standup – Teams (john@company.com)
                7. Do not add any explanation, intros, or summaries.
                8. Output must be concise, clean, and readable on a phone.
            
            """);

            chatHistory.AddUserMessage(JsonConvert.SerializeObject(agentResponses));

            var formatted = await chat.GetChatMessageContentAsync(chatHistory);
            return formatted.Content ?? AgentConstants.SorryMessage;
        }
    }
}
