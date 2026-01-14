using Microsoft.Extensions.AI;
using OpenAI.Chat;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Reflection;
using System.ComponentModel;

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
                switch (message.Role.Value)
                {
                    case AgentConstants.ChatMessageRoles.System:
                        azureMessages.Add(new SystemChatMessage(message.Text));
                        break;
                    
                    case AgentConstants.ChatMessageRoles.User:
                        azureMessages.Add(new UserChatMessage(message.Text));
                        break;
                    
                    case AgentConstants.ChatMessageRoles.Assistant:
                        var assistantToolCalls = new List<ChatToolCall>();
                        var textContent = message.Text;
                        
                        foreach (var content in message.Contents)
                        {
                            if (content is FunctionCallContent funcCall)
                            {
                                assistantToolCalls.Add(ChatToolCall.CreateFunctionToolCall(
                                    funcCall.CallId,
                                    funcCall.Name,
                                    BinaryData.FromString(JsonSerializer.Serialize(funcCall.Arguments))));
                            }
                        }
                        
                        if (assistantToolCalls.Any())
                        {
                            var assistantMsg = new AssistantChatMessage(textContent);
                            foreach (var toolCall in assistantToolCalls)
                            {
                                assistantMsg.ToolCalls.Add(toolCall);
                            }
                            azureMessages.Add(assistantMsg);
                        }
                        else
                        {
                            azureMessages.Add(new AssistantChatMessage(textContent));
                        }
                        break;
                    
                    case AgentConstants.ChatMessageRoles.Tool:
                        var toolCallId = message.AdditionalProperties?["tool_call_id"]?.ToString() ?? string.Empty;
                        
                        var functionResult = message.Contents.OfType<FunctionResultContent>().FirstOrDefault();
                        var toolResult = functionResult?.Result?.ToString() ?? message.Text ?? string.Empty;
                        
                        azureMessages.Add(new ToolChatMessage(toolCallId, toolResult));
                        break;
                    
                    default:
                        azureMessages.Add(new UserChatMessage(message.Text));
                        break;
                }
            }

            var chatOptions = new ChatCompletionOptions();
            
            if (options?.Tools != null && options.Tools.Count > 0)
            {
                foreach (var tool in options.Tools)
                {
                    if (tool is AIFunction func)
                    {
                        var parametersSchema = BuildParameterSchema(func);
                        
                        var functionDef = parametersSchema != null
                            ? ChatTool.CreateFunctionTool(func.Name, func.Description, parametersSchema)
                            : ChatTool.CreateFunctionTool(func.Name, func.Description);

                        chatOptions.Tools.Add(functionDef);
                    }
                }
            }

            var response = await _chatClient.CompleteChatAsync(azureMessages, chatOptions, cancellationToken);

            var contents = new List<AIContent>();
            
            foreach (var contentPart in response.Value.Content)
            {
                if (contentPart.Kind == ChatMessageContentPartKind.Text && !string.IsNullOrEmpty(contentPart.Text))
                {
                    contents.Add(new TextContent(contentPart.Text));
                }
            }

            foreach (var toolCall in response.Value.ToolCalls)
            {
                if (toolCall.Kind == ChatToolCallKind.Function)
                {
                    var arguments = JsonSerializer.Deserialize<Dictionary<string, object?>>(
                        toolCall.FunctionArguments.ToString()) ?? new Dictionary<string, object?>();
                    
                    contents.Add(new FunctionCallContent(
                        toolCall.Id,
                        toolCall.FunctionName,
                        arguments));
                }
            }

            if (contents.Count == 0)
            {
                contents.Add(new TextContent(string.Empty));
            }

            var completionMessage = new Microsoft.Extensions.AI.ChatMessage(ChatRole.Assistant, contents);

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

        private static BinaryData? BuildParameterSchema(AIFunction function)
        {
            try
            {
                var funcType = function.GetType();
                MethodInfo? method = null;
                
                var possibleFields = new[] { "_method", "_methodInfo", "_func", "_delegate", "method", "Method" };
                foreach (var fieldName in possibleFields)
                {
                    var field = funcType.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
                    if (field != null)
                    {
                        var value = field.GetValue(function);
                        
                        if (value is MethodInfo mi)
                        {
                            method = mi;
                            break;
                        }
                        else if (value is Delegate del)
                        {
                            method = del.Method;
                            break;
                        }
                    }
                }

                if (method == null)
                {
                    return null;
                }

                var parameters = method.GetParameters()
                    .Where(p => p.ParameterType != typeof(CancellationToken))
                    .ToList();

                if (parameters.Count == 0)
                {
                    return null;
                }

                var properties = new Dictionary<string, object>();
                var required = new List<string>();

                foreach (var param in parameters)
                {
                    var paramName = param.Name ?? "unknown";
                    var description = param.GetCustomAttribute<DescriptionAttribute>()?.Description ?? string.Empty;
                    
                    properties[paramName] = new
                    {
                        type = GetJsonSchemaType(param.ParameterType),
                        description
                    };

                    if (!param.HasDefaultValue)
                    {
                        required.Add(paramName);
                    }
                }

                var schema = new
                {
                    type = "object",
                    properties,
                    required = required.ToArray()
                };

                return BinaryData.FromObjectAsJson(schema);
            }
            catch
            {
                // If reflection fails, Azure OpenAI will infer parameters from function description
                return null;
            }
        }

        private static string GetJsonSchemaType(Type type)
        {
            var underlyingType = Nullable.GetUnderlyingType(type) ?? type;
            
            if (underlyingType == typeof(string))
            {
                return "string";
            }

            if (underlyingType == typeof(int) || underlyingType == typeof(long) || 
                underlyingType == typeof(short) || underlyingType == typeof(byte))
            {
                return "integer";
            }

            if (underlyingType == typeof(float) || underlyingType == typeof(double) || 
                underlyingType == typeof(decimal))
            {
                return "number";
            }

            if (underlyingType == typeof(bool))
            {
                return "boolean";
            }

            if (underlyingType.IsArray || (underlyingType != typeof(string) && 
                typeof(System.Collections.IEnumerable).IsAssignableFrom(underlyingType)))
            {
                return "array";
            }

            return "object";
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
