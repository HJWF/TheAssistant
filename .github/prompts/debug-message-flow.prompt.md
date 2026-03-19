---
mode: ask
description: Diagnose why a user message is not being routed or handled correctly by walking through the full message flow step by step.
---

Help me debug a message routing or handling issue in TheAssistant.

## Symptom

${input:symptom:Describe what is happening vs. what you expected, e.g. "User sends 'what is the weather?' but gets a sorry message"}

## Message flow to investigate (in order)

1. **Signal ingestion** — `Src/TheAssistantApi/Messaging/HandleReceiveMessages/ReceiveSignalMessages.cs`
   - Is the message being received? Check the HTTP trigger and polling logic.
   - Is the user being validated against `UserDetails` config?

2. **Service Bus queuing** — `Src/Messaging.ServiceAdapter/`
   - Is the message being published to the `assistant-messages` topic?
   - Check `IServiceBusServiceAdapter` implementation for serialization issues.

3. **Queue handler** — `Src/TheAssistantApi/Messaging/HandleQueuedMessage/HandleQueuedMessage.cs`
   - Is the Service Bus trigger firing?
   - Is `HandleQueuedMessageCommand` being constructed correctly?

4. **Core handler** — `Src/Core/Messaging/HandleQueuedMessage/HandleQueuedMessageCommandHandler.cs`
   - Is `IAgentServiceAdapter.HandleMessageAsync` being called?
   - Is `UserDetails` passed correctly?

5. **Routing** — `Src/Agents.ServiceAdapter/Routing/RoutingAgent.cs`
   - Is the LLM returning a valid JSON routing result?
   - Is the agent name in the response matching a constant in `AgentConstants.Names`?
   - Is the agent listed in the routing prompt?

6. **Orchestration** — `Src/Agents.ServiceAdapter/Orchestration/AgentOrchestrator.cs`
   - Is the agent found in `_agents` (registered as `IAgent`)?
   - Did the iteration count exceed `MaxIterations = 10`?

7. **Agent execution** — `Src/Agents.ServiceAdapter/{Domain}/{Domain}Agent.cs`
   - Is the `SystemPrompt` correct?
   - Are tools registered via `AIFunctionFactory.Create`?
   - Is the service adapter returning valid data?

8. **Formatting** — `Src/Agents.ServiceAdapter/Formatting/FormattingAgent.cs`
   - Is the response from the agent non-empty?
   - Is the final formatted string returned to the user?

## What to check in application logs
Search Application Insights or local logs for these structured log entries:
- `"Starting orchestration for user {UserId}"`
- `"Routing completed. {AgentCount} agent(s) will be invoked"`
- `"No agents were routed for input"`
- `"Orchestration failed for user {UserId}"`

Identify which step is the first to show an anomaly and inspect the corresponding source file.
