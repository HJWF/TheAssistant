---
applyTo: "Src/**/*.ServiceAdapter/**,!Src/Agents.ServiceAdapter/**"
---

# Service Adapter Development Instructions

## Project Structure
Each service adapter lives in `Src/{Domain}.ServiceAdapter/` and contains:
```
{Domain}.ServiceAdapter/
  {Domain}ServiceAdapter.cs      ? Implementation
  {Domain}Settings.cs            ? Config record (if needed)
  {Domain}Client.cs              ? HTTP client wrapper (if needed)
  Module.cs                      ? DI registration
  {Domain}.ServiceAdapter.csproj
```

## Interface Contract
The interface lives in `Src/Core/I{Domain}ServiceAdapter.cs`:
```csharp
namespace TheAssistant.Core
{
    public interface I{Domain}ServiceAdapter
    {
        Task<ReturnType> MethodAsync(string param, CancellationToken cancellationToken = default);
    }
}
```

## Implementation Rules
- Class name: `{Domain}ServiceAdapter` in namespace `TheAssistant.{Domain}.ServiceAdapter`
- Constructor validates all dependencies with `?? throw new ArgumentNullException(nameof(x))`
- Log all operations with structured logging (named placeholders, no string interpolation)
- Catch exceptions, log them, and re-throw or return an error object — never swallow silently
- Use `HttpClient` injected via `IHttpClientFactory` for HTTP calls, never `new HttpClient()`

## Settings Pattern
```csharp
public class {Domain}Settings
{
    [Required]
    public string ApiKey { get; set; } = string.Empty;
}
```
Bind in `Module.cs` using `services.AddOptions<{Domain}Settings>().Configure(options).ValidateDataAnnotations()`.

## Module.cs Pattern
```csharp
public static class Module
{
    public static IServiceCollection Add{Domain}Services(
        this IServiceCollection services,
        Action<{Domain}Settings> options)
    {
        services.AddOptions<{Domain}Settings>()
            .Configure(options)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddHttpClient<{Domain}Client>();
        services.AddTransient<I{Domain}ServiceAdapter, {Domain}ServiceAdapter>();

        return services;
    }
}
```

## Project File Rules
- Target `net8.0` is inherited from `Directory.Build.props` — do not set it again
- Never add `Version` to a `<PackageReference>` — versions are managed in `Directory.Packages.props`
- Reference `Src/Core/Core.csproj` as a `<ProjectReference>`
