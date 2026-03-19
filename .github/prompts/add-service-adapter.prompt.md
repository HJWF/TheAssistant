---
mode: agent
description: Scaffold a complete new service adapter (interface in Core, implementation project, settings, Module.cs, and unit test project) following the TheAssistant service adapter pattern.
---

Create a new service adapter for TheAssistant project. Follow the conventions in `#file:.github/instructions/service-adapters.instructions.md`.

## What to build

Domain name: ${input:domain:e.g. Spotify, GitHub, Slack}
External API base URL: ${input:baseUrl:e.g. https://api.spotify.com/v1}
Operations needed: ${input:operations:comma-separated, e.g. GetPlaylists,SearchTracks}

## Steps

1. **Create the interface** in `Src/Core/I{Domain}ServiceAdapter.cs`:
   - Namespace `TheAssistant.Core`
   - One method per operation: `Task<string> {Operation}Async(params, CancellationToken cancellationToken = default)`

2. **Create the project** `Src/{Domain}.ServiceAdapter/{Domain}.ServiceAdapter.csproj`:
   - Reference `Src/Core/Core.csproj`
   - No `Version` on `<PackageReference>` — add new packages to `Directory.Packages.props` only
   - Add the project to `TheAssistant.sln`

3. **Create settings** `Src/{Domain}.ServiceAdapter/{Domain}Settings.cs`:
   - Use `[Required]` annotations for mandatory values

4. **Create an HTTP client wrapper** `Src/{Domain}.ServiceAdapter/{Domain}Client.cs` (if HTTP-based):
   - Accept `HttpClient` via constructor
   - One method per API call with structured logging

5. **Create the adapter** `Src/{Domain}.ServiceAdapter/{Domain}ServiceAdapter.cs`:
   - Implements `I{Domain}ServiceAdapter`
   - Logs errors with `_logger.LogError(ex, "...")`
   - Returns error JSON on failure: `JsonSerializer.Serialize(new { error = ex.Message })`

6. **Create `Module.cs`** `Src/{Domain}.ServiceAdapter/Module.cs`:
   - Extension method `Add{Domain}Services(this IServiceCollection, Action<{Domain}Settings>)`
   - Register settings with `ValidateDataAnnotations().ValidateOnStart()`
   - Register `I{Domain}ServiceAdapter` as `Transient`

7. **Wire up in `Src/TheAssistantApi/Program.cs`**:
   - Add `using TheAssistant.{Domain}.ServiceAdapter;`
   - Call `services.Add{Domain}Services(s => builder.Configuration.GetSection("...").Bind(s));`

8. **Create unit test project** `Tests/{Domain}.ServiceAdapter.UnitTests/`:
- `.csproj` referencing the adapter project and test packages (no versions)
- `{Domain}ServiceAdapterTests.cs` following `#file:.github/instructions/testing.instructions.md`
- Use `http://localhost` for any URLs in tests — never reference real external API URLs
- Add the test project to `TheAssistant.sln`
