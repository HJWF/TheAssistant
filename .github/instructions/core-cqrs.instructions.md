---
applyTo: "Src/Core/**"
---

# Core Layer (CQRS) Instructions

## What Belongs in Core
- Command and query definitions (`ICommand`, `IQuery`)
- Command and query handler interfaces (`ICommandHandler<T>`, `IQueryHandler<T,R>`)
- Handler implementations (orchestrate use cases, never call external APIs directly)
- Domain model classes (e.g. `CalendarEvent`, `WeatherForecast`, `AzureCostSummary`)
- Service adapter interfaces (`I{Domain}ServiceAdapter`)
- Settings records used across the solution (`UserDetails`, `LoginSettings`)

## What Does NOT Belong in Core
- Any implementation that calls an external API
- Any Azure SDK reference
- Any `HttpClient` usage
- Any agent logic

## Command Pattern
```csharp
// Command (marker interface)
public record {Action}{Subject}Command : ICommand
{
    public required string Input { get; init; }
    public required UserDetails User { get; init; }
}

// Handler
public class {Action}{Subject}CommandHandler : ICommandHandler<{Action}{Subject}Command>
{
    private readonly I{Domain}ServiceAdapter _{adapter};

    public {Action}{Subject}CommandHandler(I{Domain}ServiceAdapter adapter)
    {
        _{adapter} = adapter ?? throw new ArgumentNullException(nameof(adapter));
    }

    public async Task HandleAsync({Action}{Subject}Command command, CancellationToken cancellationToken = default)
    {
        await _{adapter}.DoSomethingAsync(command.Input, cancellationToken);
    }
}
```

## Registration in Core/Module.cs
Handlers are registered via open-generic assembly scanning — **do not register individual handlers manually**:
```csharp
services.AddTransient(typeof(ICommandHandler<>), typeof(Module).Assembly);
services.AddTransient(typeof(IQueryHandler<,>), typeof(Module).Assembly);
```
Any class in `Src/Core/` that implements `ICommandHandler<T>` or `IQueryHandler<T,R>` is picked up automatically. No changes to `Module.cs` are needed when adding a new handler.

## Service Adapter Interfaces
Define all service adapter interfaces in `Src/Core/` at the root level:
```csharp
namespace TheAssistant.Core
{
    public interface I{Domain}ServiceAdapter
    {
        Task<ReturnType> OperationAsync(string param, CancellationToken cancellationToken = default);
    }
}
```
