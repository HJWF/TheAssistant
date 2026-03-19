---
mode: agent
description: Scaffold a complete new agent (interface, implementation, registration, and unit test stub) following the TheAssistant agent pattern.
---

Create a new agent for TheAssistant project. Follow the conventions in `#file:.github/instructions/agents.instructions.md`.

## What to build

Agent domain: ${input:domain:e.g. Recipe, News, Finance}
Agent description: ${input:description:what this agent does in one sentence}
Tools the agent needs: ${input:tools:comma-separated list of tool method names, e.g. GetRecipeByName,SearchRecipes}

## Steps

1. **Add the agent name constant** to `Src/Agents.ServiceAdapter/AgentConstants.cs`:
   - Add `public const string {Domain} = "{domain-kebab-case}-agent";` inside the `Names` class

2. **Create the interface** at `Src/Agents.ServiceAdapter/{Domain}/I{Domain}Agent.cs`:
   - Extend `IAgent`
   - Declare each tool as a method signature

3. **Create the implementation** at `Src/Agents.ServiceAdapter/{Domain}/{Domain}Agent.cs`:
   - `private const string SystemPrompt` with `{CurrentDate}` placeholder
   - `public string Name => AgentConstants.Names.{Domain};`
   - Each tool decorated with `[Description("...")]`
   - `HandleAsync(AgentMessage, CancellationToken)` using `IChatClient` with `AIFunctionFactory`
   - Error handling per tool: `JsonSerializer.Serialize(new { error = ex.Message })`

4. **Register in `Src/Agents.ServiceAdapter/Module.cs`**:
   - `services.AddSingleton<I{Domain}Agent, {Domain}Agent>();`
   - Also register as `IAgent` so the orchestrator picks it up

5. **Add the agent to the routing prompt** in `Src/Agents.ServiceAdapter/Routing/RoutingAgent.cs`:
   - Add `- {domain-kebab-case}-agent: {description}` to the known agents list

6. **Create a unit test stub** at `Tests/Agents.ServiceAdapter.UnitTests/{Domain}AgentTests.cs`:
   - Follow `#file:.github/instructions/testing.instructions.md`
   - Include at least: constructor null-guard tests and one happy-path test per tool

Do not create a new service adapter project — if external data is needed, assume the service adapter already exists or will be added separately.
