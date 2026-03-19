---
mode: ask
description: Review a pull request or staged changes for compliance with TheAssistant coding conventions, architecture rules, and quality standards.
---

Review the following changes in TheAssistant for correctness and convention compliance.

## Changes to review

${input:context:Paste the diff, file names, or describe the changes}

## Review checklist

### Architecture
- [ ] No business logic in the Azure Function classes — handlers only
- [ ] No external API calls from `Src/Core/` — only through service adapter interfaces
- [ ] New adapters implement the interface defined in `Src/Core/`
- [ ] New agents have a `Name` property matching a constant in `AgentConstants.Names`
- [ ] `Module.cs` is the only place DI is configured per project
- [ ] No new handler registrations added manually to `Core/Module.cs` — handlers are auto-discovered via open-generic assembly scanning

### Code quality
- [ ] No `.Result` or `.Wait()` on async code
- [ ] Constructor arguments validated with `?? throw new ArgumentNullException(nameof(x))`
- [ ] String parameters validated with `ArgumentException.ThrowIfNullOrWhiteSpace()`
- [ ] Structured logging used (no string interpolation in log messages)
- [ ] `CancellationToken` is the last parameter and named `cancellationToken`

### Packages
- [ ] No `Version` attribute on `<PackageReference>` in `.csproj` files
- [ ] New packages added only to `Directory.Packages.props`

### Agents
- [ ] `SystemPrompt` contains `{CurrentDate}` placeholder
- [ ] Every tool method has `[Description("...")]` attribute
- [ ] Tool methods return `JsonSerializer.Serialize(new { error = ... })` on failure

### Tests
- [ ] Test class named `{ClassName}Tests`
- [ ] Test method names are PascalCase descriptive sentences — no underscores
- [ ] Mocks use `MockBehavior.Strict`
- [ ] Setups use `.Verifiable()` and are followed by `.Verify(..., Times.Once)` in the Assert section
- [ ] Complex return types are validated via a `{TypeName}Validator` static extension class — no long inline assertion chains
- [ ] `ILogger<T>` dependencies use `Mock<ILogger<T>>` — no assertions on log calls unless logging is the explicit behavior under test
- [ ] Constructor null-guards are tested
- [ ] No `Module.cs` tests

Flag any violations and suggest the correct implementation based on the patterns in this codebase.
