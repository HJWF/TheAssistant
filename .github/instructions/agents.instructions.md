---
applyTo: "Src/Agents.ServiceAdapter/**"
---

# Agent Development Instructions

## Mandatory Structure
Every agent **must** follow this exact structure:

```csharp
public class {Domain}Agent : I{Domain}Agent
{
    private const string SystemPrompt = """
        You are a ... assistant ...
        Current date context: Today is {CurrentDate}.
        Available tools:
        - ToolName: description
        Process:
        1. ...
        """;

    private readonly I{Domain}ServiceAdapter _{serviceAdapter};
    private readonly IChatClient _chatClient;
    private readonly ILogger<{Domain}Agent> _logger;

    public string Name => AgentConstants.Names.{Domain};

    public {Domain}Agent(
        I{Domain}ServiceAdapter serviceAdapter,
        IChatClient chatClient,
        ILogger<{Domain}Agent> logger)
    {
        _{serviceAdapter} = serviceAdapter ?? throw new ArgumentNullException(nameof(serviceAdapter));
        _chatClient = chatClient ?? throw new ArgumentNullException(nameof(chatClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
}
```

## System Prompt Rules
- Always include `{CurrentDate}` placeholder — it is replaced at runtime with `DateTime.UtcNow.ToString("yyyy-MM-dd")`
- Start with "You are a ... assistant with access to tools for ..."
- List all available tools with their exact method names
- Define a numbered Process section
- Keep the tone instructive (CAPS for IMPORTANT notes)

## Tool Methods
- Decorate every public tool method with `[Description("...")]`
- Keep descriptions concise — this text is sent to the LLM
- Wrap all tool body in try/catch; on error return `JsonSerializer.Serialize(new { error = ex.Message })`
- Log errors with `_logger.LogError(ex, "...")`

## HandleAsync Pattern
Use the standard pattern for chat-based execution:

```csharp
public async Task<IEnumerable<AgentMessage>> HandleAsync(
    AgentMessage message,
    CancellationToken cancellationToken = default)
{
    var now = DateTime.UtcNow;
    var systemPrompt = SystemPrompt.Replace("{CurrentDate}", now.ToString("yyyy-MM-dd"));

    var messages = new List<ChatMessage>
    {
        new(ChatRole.System, systemPrompt),
        new(ChatRole.User, message.Content)
    };

    var tools = AIFunctionFactory.Create(GetTool1);
    var options = new ChatOptions { Tools = [tools] };

    var response = await _chatClient.GetResponseAsync(messages, options, cancellationToken);
    return [new AgentMessage { Sender = Name, Content = response.Text }];
}
```

## Agent Registration
Register in `Agents.ServiceAdapter/Module.cs` as `AddSingleton`:
```csharp
services.AddSingleton<I{Domain}Agent, {Domain}Agent>();
services.AddSingleton<IAgent, {Domain}Agent>(sp => ({Domain}Agent)sp.GetRequiredService<I{Domain}Agent>());
```

Add the agent name constant to `AgentConstants.Names` and add it to the routing prompt in `RoutingAgent.cs`.
