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

        public async Task<ChatMessage> GetChatMessageContentAsync(ChatHistory history, CancellationToken cancellationToken = default)
        {
            return await GetChatMessageContentAsync(history, null, cancellationToken);
        }

        public async Task<ChatMessage> GetChatMessageContentAsync(ChatHistory history, ChatOptions? options, CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                var messages = ConvertToAIMessages(history);
                
                if (options?.Tools != null && options.Tools.Count > 0)
                {
                    _logger.LogDebug("Invoking with {ToolCount} tools", options.Tools.Count);
                    var response = await InvokeWithToolsAsync(messages, options, cancellationToken);
                    _logger.LogInformation("Chat completion with tools completed in {Duration}ms", stopwatch.ElapsedMilliseconds);
                    return new ChatMessage(response);
                }
                else
                {
                    var response = await _chatClient.GetResponseAsync(messages, options, cancellationToken);
                    _logger.LogInformation("Chat completion completed in {Duration}ms", stopwatch.ElapsedMilliseconds);
                    return new ChatMessage(response.Text ?? string.Empty);
                }
            }
            catch (Exception ex) when (ex.Message.Contains("429") || ex.Message.Contains("rate limit"))
            {
                _logger.LogError(ex, "Rate limit exceeded");
                throw new InvalidOperationException("Rate limit exceeded. Please try again later.", ex);
            }
            catch (Exception ex) when (IsClientError(ex))
            {
                _logger.LogError(ex, "Client error: {Message}", ex.Message);
                throw new InvalidOperationException($"Client error: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Chat completion failed");
                throw new InvalidOperationException("Unexpected error during chat completion.", ex);
            }
        }

        private async Task<string> InvokeWithToolsAsync(
            List<Microsoft.Extensions.AI.ChatMessage> messages,
            ChatOptions options,
            CancellationToken cancellationToken)
        {
            const int maxIterations = 5;
            
            for (int iteration = 0; iteration < maxIterations; iteration++)
            {
                var response = await _chatClient.GetResponseAsync(messages, options, cancellationToken);
                var assistantMessage = response.Messages.LastOrDefault();
                
                if (assistantMessage == null)
                {
                    _logger.LogWarning("Response contained no messages");
                    return response.Text ?? string.Empty;
                }
                
                var toolCalls = assistantMessage.Contents.OfType<FunctionCallContent>().ToList();
                
                if (toolCalls.Count == 0)
                {
                    return response.Text ?? string.Empty;
                }
                
                _logger.LogDebug("Iteration {Iteration}: Executing {ToolCallCount} tool(s)", iteration + 1, toolCalls.Count);
                
                messages.Add(assistantMessage);
                
                foreach (var toolCall in toolCalls)
                {
                    var result = await ExecuteToolAsync(toolCall, options.Tools, cancellationToken);
                    
                    var toolMessage = new Microsoft.Extensions.AI.ChatMessage(
                        ChatRole.Tool,
                        [new FunctionResultContent(toolCall.CallId, result)])
                    {
                        AdditionalProperties = new AdditionalPropertiesDictionary
                        {
                            ["tool_call_id"] = toolCall.CallId
                        }
                    };
                    
                    messages.Add(toolMessage);
                }
            }
            
            _logger.LogWarning("Maximum tool execution iterations ({MaxIterations}) reached", maxIterations);
            return "I apologize, but I encountered an issue processing your request.";
        }

        private async Task<object?> ExecuteToolAsync(
            FunctionCallContent toolCall,
            IList<AITool> tools,
            CancellationToken cancellationToken)
        {
            var tool = tools.OfType<AIFunction>().FirstOrDefault(t => t.Name == toolCall.Name);
            
            if (tool == null)
            {
                _logger.LogWarning("Tool '{ToolName}' not found", toolCall.Name);
                return $"Error: Tool '{toolCall.Name}' not found";
            }

            try
            {
                var args = new AIFunctionArguments(toolCall.Arguments ?? new Dictionary<string, object?>());
                return await tool.InvokeAsync(args, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Tool '{ToolName}' execution failed", toolCall.Name);
                return $"Error executing tool: {ex.Message}";
            }
        }

        private static List<Microsoft.Extensions.AI.ChatMessage> ConvertToAIMessages(ChatHistory history)
        {
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

            return messages;
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
