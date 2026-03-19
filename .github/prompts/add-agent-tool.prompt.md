---
mode: agent
description: Add a new tool method to an existing TheAssistant agent, including the [Description] attribute, error handling, and corresponding unit test.
---

Add a new tool to an existing agent in TheAssistant. Follow the conventions in `#file:.github/instructions/agents.instructions.md`.

## What to add

Agent name: ${input:agentName:e.g. AgendaAgent, WeatherAgent, NotionAgent}
Tool method name: ${input:toolName:e.g. GetEventsForNextWeek}
Tool description (shown to LLM): ${input:toolDescription:e.g. Gets calendar events for the next 7 days}
Parameters (if any): ${input:params:comma-separated name:type pairs, e.g. city:string or leave blank}

## Steps

1. **Add the method signature to the interface** `Src/Agents.ServiceAdapter/{Domain}/I{Domain}Agent.cs`:
   ```csharp
   Task<string> {ToolName}({params});
   ```

2. **Implement the tool** in `Src/Agents.ServiceAdapter/{Domain}/{Domain}Agent.cs`:
   - Add `[Description("{toolDescription}")]` attribute above the method
   - Call the appropriate service adapter method
   - Wrap in try/catch — return `JsonSerializer.Serialize(new { error = ex.Message })` on failure
   - Log errors with `_logger.LogError(ex, "Error in {ToolName}")`

3. **Register the tool in HandleAsync**:
   - Add `AIFunctionFactory.Create({ToolName})` to the `tools` array in `HandleAsync`

4. **Update the SystemPrompt** of the agent:
   - Add `- {ToolName}: {toolDescription}` to the "Available tools:" section
   - Add a usage example if helpful

5. **Add a unit test** in `Tests/Agents.ServiceAdapter.UnitTests/{Domain}AgentTests.cs`:
   - Happy path: mock returns data, verify result is non-empty JSON
   - Error path: mock throws, verify result contains `"error"` key
