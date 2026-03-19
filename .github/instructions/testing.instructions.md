---
applyTo: "Tests/**"
---

# Testing Instructions

## Project Naming
- Test project: `Tests/{Domain}.ServiceAdapter.UnitTests/`
- Test class: `{ClassName}Tests.cs`
- One test class per production class

## Required NuGet packages (no versions — managed in Directory.Packages.props)
```xml
<PackageReference Include="xunit" />
<PackageReference Include="xunit.runner.visualstudio" />
<PackageReference Include="Microsoft.NET.Test.Sdk" />
<PackageReference Include="Moq" />
<PackageReference Include="FluentAssertions" />
<PackageReference Include="coverlet.collector" />
```

## Test Structure
```csharp
public class {ClassName}Tests
{
    private readonly Mock<IDependency> _dependencyMock;
    private readonly {ClassName} _sut;

    public {ClassName}Tests()
    {
        _dependencyMock = new Mock<IDependency>();
        _sut = new {ClassName}(_dependencyMock.Object);
    }

    [Fact]
    public async Task MethodNameDescribesExpectedBehavior()
    {
        _dependencyMock.Setup(x => x.Method()).ReturnsAsync(value);

        var result = await _sut.MethodAsync();

        result.Should().Be(expected);
    }
}
```

## Naming Convention
Test method names use plain descriptive sentences in PascalCase — no underscores:
- `HandleMessageAsyncThrowsWhenUserIsNull`
- `GetCurrentMonthCostsReturnsMappedCostsWhenApiResponds`
- `RouteAsyncReturnsEmptyListForEmptyMessage`

## Rules
- Use `Moq` for all mocking — never create real implementations of interfaces in tests
- Use `FluentAssertions` for all assertions — never use `Assert.Equal` from xUnit directly
- Arrange / Act / Assert separated by blank lines (no comments labeling them)
- For async methods use `await` — never `.Result` or `.Wait()`
- Do not test `Module.cs` registration
- Test one behavior per test method
- Use `Mock<ILogger<T>>` when a logger is required — do not assert on logger calls unless it is the explicit behavior under test
- **Always use `http://localhost` for any URL in tests** — never use real external URLs (e.g. `https://api.example.com`). For clients that require HTTPS (e.g. `SecretClient`) use `https://localhost`.

## Validator Pattern
For any test that validates properties of a complex return type, create a static validator class instead of writing inline assertions:

```csharp
internal static class {TypeName}Validator
{
    public static void Validate(this {ActualType} actual, {ExpectedType} expected)
    {
        actual.Property1.Should().Be(expected.Property1);
        actual.Property2.Should().Be(expected.Property2);
        actual.NestedObject.Name.Should().Be(expected.Name);
    }
}
```

This keeps test methods clean:
```csharp
[Fact]
public async Task GetWeatherReturnsMappedForecast()
{
    var expected = new WeatherForecast { ... };
    _clientMock.Setup(...).ReturnsAsync(expected);

    var result = await _sut.GetWeather("52.0", "4.0");

    result.Validate(expected);
}
```

- One validator class per domain type, in the same test project
- Name: `{TypeName}Validator`
- Only use inline `.Should()` for primitives or single-property results where a validator would be overkill

## Exception Testing
```csharp
var act = () => new {ClassName}(null!);
act.Should().Throw<ArgumentNullException>().WithParameterName("dependency");
```

## Async Testing
```csharp
var act = async () => await _sut.MethodAsync(null!);
await act.Should().ThrowAsync<ArgumentException>();
```
