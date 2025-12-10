using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace TheAssistant.Agents.ServiceAdapter.AI
{
    public class MicrosoftAgentsChatCompletionService : IChatCompletionService
    {
        private readonly IChatClient _chatClient;
        private readonly ILogger<MicrosoftAgentsChatCompletionService> _logger;

        public MicrosoftAgentsChatCompletionService(
            IChatClient chatClient,
            ILogger<MicrosoftAgentsChatCompletionService> logger)
        {
            _chatClient = chatClient ?? throw new ArgumentNullException(nameof(chatClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<ChatMessage> GetChatMessageContentAsync(
            ChatHistory history, 
            CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                _logger.LogDebug("Starting chat completion with {MessageCount} messages", history.Messages.Count);

                var messages = new List<Microsoft.Extensions.AI.ChatMessage>();

                foreach (var message in history.Messages)
                {
                    messages.Add(message.Role switch
                    {
                        AgentConstants.ChatMessageRoles.System => 
                            new Microsoft.Extensions.AI.ChatMessage(ChatRole.System, message.Content),
                        
                        AgentConstants.ChatMessageRoles.User => 
                            new Microsoft.Extensions.AI.ChatMessage(ChatRole.User, message.Content),
                        
                        AgentConstants.ChatMessageRoles.Assistant => 
                            new Microsoft.Extensions.AI.ChatMessage(ChatRole.Assistant, message.Content),
                        
                        AgentConstants.ChatMessageRoles.Tool => 
                            new Microsoft.Extensions.AI.ChatMessage(ChatRole.Tool, message.Content)
                            {
                                AdditionalProperties = new AdditionalPropertiesDictionary
                                {
                                    ["tool_call_id"] = message.ToolCallId
                                }
                            },
                        
                        var _ => new Microsoft.Extensions.AI.ChatMessage(ChatRole.User, message.Content)
                    });
                }

                var response = await _chatClient.GetResponseAsync(
                    messages, 
                    cancellationToken: cancellationToken);

                stopwatch.Stop();

                _logger.LogInformation(
                    "Chat completion succeeded. Duration: {Duration}ms, Model: {Model}",
                    stopwatch.ElapsedMilliseconds,
                    response.ModelId ?? "unknown");

                var responseContent = response.Text ?? string.Empty;

                return new ChatMessage(responseContent);
            }
            catch (Exception ex) when (ex.Message.Contains("429") || ex.Message.Contains("rate limit"))
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "Chat completion failed due to rate limiting. Duration: {Duration}ms",
                    stopwatch.ElapsedMilliseconds);
                throw new InvalidOperationException("Rate limit exceeded. Please try again later.", ex);
            }
            catch (Exception ex) when (IsClientError(ex))
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "Chat completion failed due to client error. Duration: {Duration}ms",
                    stopwatch.ElapsedMilliseconds);
                throw new InvalidOperationException($"Client error: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "Chat completion failed with unexpected error. Duration: {Duration}ms",
                    stopwatch.ElapsedMilliseconds);
                throw new InvalidOperationException("Unexpected error during chat completion.", ex);
            }
        }

        private static bool IsClientError(Exception ex)
        {
            var message = ex.Message.ToLowerInvariant();
            return message.Contains("400") || 
                   message.Contains("401") || 
                   message.Contains("403") || 
                   message.Contains("404") ||
                   message.Contains("unauthorized") ||
                   message.Contains("forbidden");
        }
    }
}
