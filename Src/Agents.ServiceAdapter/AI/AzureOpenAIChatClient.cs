using Microsoft.Extensions.AI;
using OpenAI.Chat;
using System.Runtime.CompilerServices;

namespace TheAssistant.Agents.ServiceAdapter.AI
{
    internal class AzureOpenAIChatClient : IChatClient
    {
        private readonly ChatClient _chatClient;

        public AzureOpenAIChatClient(ChatClient chatClient)
        {
            _chatClient = chatClient ?? throw new ArgumentNullException(nameof(chatClient));
        }

        public ChatClientMetadata Metadata => new("azure-openai");

        public async Task<ChatResponse> GetResponseAsync(
            IEnumerable<Microsoft.Extensions.AI.ChatMessage> chatMessages, 
            ChatOptions? options = null, 
            CancellationToken cancellationToken = default)
        {
            var azureMessages = new List<OpenAI.Chat.ChatMessage>();
            
            foreach (var message in chatMessages)
            {
                azureMessages.Add(message.Role.Value switch
                {
                    AgentConstants.ChatMessageRoles.System => new SystemChatMessage(message.Text),
                    AgentConstants.ChatMessageRoles.User => new UserChatMessage(message.Text),
                    AgentConstants.ChatMessageRoles.Assistant => new AssistantChatMessage(message.Text),
                    AgentConstants.ChatMessageRoles.Tool => new ToolChatMessage(
                        message.AdditionalProperties?["tool_call_id"]?.ToString() ?? string.Empty, 
                        message.Text),
                    var _ => new UserChatMessage(message.Text)
                });
            }

            var response = await _chatClient.CompleteChatAsync(azureMessages, options: null, cancellationToken);

            var completionMessage = new Microsoft.Extensions.AI.ChatMessage(
                ChatRole.Assistant, 
                response.Value.Content[0].Text);

            return new ChatResponse(new[] { completionMessage })
            {
                ModelId = response.Value.Model,
                FinishReason = ConvertFinishReason(response.Value.FinishReason),
                Usage = new UsageDetails
                {
                    InputTokenCount = response.Value.Usage?.InputTokenCount,
                    OutputTokenCount = response.Value.Usage?.OutputTokenCount,
                    TotalTokenCount = response.Value.Usage?.TotalTokenCount
                }
            };
        }

        public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<Microsoft.Extensions.AI.ChatMessage> chatMessages, 
            ChatOptions? options = null, 
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var response = await GetResponseAsync(chatMessages, options, cancellationToken);
            
            yield return new ChatResponseUpdate
            {
                Role = ChatRole.Assistant,
                Contents = new List<AIContent> { new TextContent(response.Text) }
            };
        }

        public object? GetService(Type serviceType, object? key = null)
        {
            return key is null && serviceType.IsInstanceOfType(_chatClient)
                ? _chatClient
                : null;
        }

        public void Dispose()
        {
            // ChatClient doesn't implement IDisposable, nothing to dispose
        }

        private static Microsoft.Extensions.AI.ChatFinishReason? ConvertFinishReason(
            OpenAI.Chat.ChatFinishReason? finishReason)
        {
            return finishReason switch
            {
                OpenAI.Chat.ChatFinishReason.Stop => Microsoft.Extensions.AI.ChatFinishReason.Stop,
                OpenAI.Chat.ChatFinishReason.Length => Microsoft.Extensions.AI.ChatFinishReason.Length,
                OpenAI.Chat.ChatFinishReason.ContentFilter => Microsoft.Extensions.AI.ChatFinishReason.ContentFilter,
                OpenAI.Chat.ChatFinishReason.ToolCalls => Microsoft.Extensions.AI.ChatFinishReason.ToolCalls,
                var _ => null
            };
        }
    }
}
