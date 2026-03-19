# GitHub Copilot Instructions – TheAssistant

## Project Overview
TheAssistant is an AI-powered personal assistant built on **.NET 8 Azure Functions**. It receives messages via Signal, processes them through a multi-agent orchestration pipeline, and replies with structured responses. It integrates with Microsoft Graph (Agenda), a Weather API, Azure Cost Management, and Notion (via MCP).

---

## Architecture Patterns

### Service Adapter Pattern
Every external service integration lives in its own `*.ServiceAdapter` project under `Src/`. Each adapter:
- Implements an interface defined in `Src/Core/` (e.g. `IAgendaServiceAdapter`, `IWeatherServiceAdapter`)
- Registers itself via a `Module.cs` static class with an `Add*Services(this IServiceCollection)` extension method
- Is decoupled from the Core business logic through the interface

### CQRS (Core Layer)
The `Src/Core/` project defines commands and queries using `ICommand`/`IQuery` marker interfaces and `ICommandHandler<T>`/`IQueryHandler<T,R>` handler interfaces. Never place business logic in the API layer—route it through command/query handlers.

### Agent Pattern (`Agents.ServiceAdapter`)
Each agent must:
- Implement `IAgent` (from `Core`) with a `string Name` property matching a constant in `AgentConstants.Names`
- Declare a `private const string SystemPrompt` with `{CurrentDate}` placeholder injected at runtime
- Expose tool methods decorated with `[Description("...")]` for LLM tool calling via `Microsoft.Extensions.AI`
- Accept `IChatClient` via constructor injection (shared singleton)
- Return results as `IEnumerable<AgentMessage>` or `string` depending on the interface

### Module Registration
Every project exposes a single `public static class Module` with one or more `Add*Services` extension methods on `IServiceCollection`. Registration is called from `TheAssistantApi/Program.cs`. Do not use `[AutoRegister]` or source generators—keep DI explicit.

---

## Technology Stack
- **Runtime:** .NET 8, Azure Functions (Isolated Worker)
- **AI:** `Microsoft.Extensions.AI` + `Azure.AI.OpenAI` — always use `IChatClient` abstraction, never `OpenAIClient` directly in agents
- **Messaging:** Signal API (HTTP), Azure Service Bus (topic: `assistant-messages`)
- **Auth:** Azure Key Vault (`Azure.Security.KeyVault.Secrets`), Managed Identity (`Azure.Identity`)
- **Persistence:** Azure Key Vault for tokens, in-memory fallback for dev
- **Notion:** Model Context Protocol (`ModelContextProtocol` package) via `NotionMcpServer`
- **Testing:** xUnit, Moq, FluentAssertions
- **Packages:** centrally managed in `Directory.Packages.props` — never add a `Version` attribute to a `<PackageReference>` in a `.csproj`

---

## Coding Conventions

### General
- Use `async/await` throughout — never `.Result` or `.Wait()`
- Validate constructor arguments with `?? throw new ArgumentNullException(nameof(x))`
- Prefer `ArgumentException.ThrowIfNullOrWhiteSpace()` for string parameters
- Keep `CancellationToken` as last parameter, named `cancellationToken`
- `using` statements at file top, no redundant qualifications
- Place private methods below public ones, even if they are static.

### Naming
| Concept | Convention |
|---|---|
| Service adapter interface | `I{Domain}ServiceAdapter` in Core |
| Service adapter implementation | `{Domain}ServiceAdapter` |
| Agent interface | `I{Domain}Agent` |
| Agent implementation | `{Domain}Agent` |
| Command | `{Action}{Subject}Command` |
| Command handler | `{Action}{Subject}CommandHandler` |
| Unit test class | `{ClassName}Tests` |
| Module file | `Module.cs` (one per project) |
| Test method names | Do not use underscores; avoid snake_case naming for test methods in TheAssistant project |

### Logging
Use structured logging with named placeholders, never string interpolation:
```csharp
_logger.LogInformation("Processing message for user {UserId}", user.PersonalMailTag);
```

### JSON Serialization
- Use `System.Text.Json` by default
- Use `Newtonsoft.Json` only in places already using it (e.g. `RoutingAgent`, `FormattingAgent`)
- Return errors from agent tools as `JsonSerializer.Serialize(new { error = message })`

---

## Testing Conventions
- One test class per production class, named `{ClassName}Tests`
- Use Moq for mocking, FluentAssertions for assertions
- Arrange / Act / Assert sections (no comments needed, just blank lines)
- Test file lives in the corresponding `Tests/*.UnitTests` project
- Do not test `Module.cs` registration directly — test behavior via the handlers/adapters
- **Always use `http://localhost` for any URL in tests** — never reference real external URLs such as `https://management.azure.com` or `https://api.example.com`. For SDK clients that require HTTPS (e.g. `SecretClient`) use `https://localhost`.
- For complex return types, create an internal static class named `{TypeName}Validator` with an extension method `Validate(this ActualType actual, ExpectedType expected)` that groups all `Should()` assertions. Test methods stay clean by calling `result.Validate(expected)` instead of writing inline assertions.

---

## Infrastructure
- Deployed as two Azure Functions apps: `TheAssistantApi` (main) and `TheAssistantApi.Login` (OAuth flow)
- IaC lives in `Infra/` (Bicep)
- CI/CD via `.github/workflows/deploy.yml` — manual trigger (`workflow_dispatch`)
- App config sections: `UserDetails`, `Login`, `Agents`, `Signal`, `ServiceBus`, `AzureCosts`, `TokenStore`, `Notion`, `UserAssignedManagedIdentity`
