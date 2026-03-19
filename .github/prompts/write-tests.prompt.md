---
mode: agent
description: Generate comprehensive unit tests for a TheAssistant class following xUnit + Moq + FluentAssertions conventions.
---

Write unit tests for an existing class in TheAssistant. Follow `#file:.github/instructions/testing.instructions.md`.

## Target class

File to test: ${input:filePath:e.g. Src/Agents.ServiceAdapter/Agenda/AgendaAgent.cs}

## Instructions

1. Read the target file fully to understand:
   - Constructor dependencies (these become `Mock<T>` fields)
   - All public methods and their expected behaviors
   - Any exception guards (`?? throw`, `ThrowIfNullOrWhiteSpace`, etc.)

2. Create the test file at the matching path under `Tests/`:
   - Map `Src/X.ServiceAdapter/Y.cs` ? `Tests/X.ServiceAdapter.UnitTests/YTests.cs`

3. Generate test cases covering:
   - **Constructor**: one test per nullable argument — verify `ArgumentNullException` is thrown with the correct parameter name
   - **Happy path**: one test per method — mock returns valid data, verify the result
   - **Error path**: if the method has try/catch, mock throws an exception, verify the fallback or error JSON is returned
   - **Edge cases**: empty string inputs, null collections, zero values where applicable

4. **URL rule**: any URL used in tests must use `http://localhost`. Never use real external URLs such as `https://management.azure.com` or `https://api.example.com`. For SDK clients that enforce HTTPS (e.g. `SecretClient`) use `https://localhost`.

5. Follow this structure:
   ```csharp
   public class {ClassName}Tests
   {
       private readonly Mock<IDep> _depMock;
       private readonly {ClassName} _sut;

       public {ClassName}Tests()
       {
           _depMock = new Mock<IDep>(MockBehavior.Strict);
           _sut = new {ClassName}(_depMock.Object);
       }

       [Fact]
       public async Task DescribesExpectedBehaviorInPascalCase()
       {
           var expected = new {ReturnType} { ... };
           _depMock.Setup(...).ReturnsAsync(expected).Verifiable();

           var result = await _sut.{Method}(...);

           result.Validate(expected);
           _depMock.Verify(x => x.{Method}(...), Times.Once);
       }
   }
   ```

5. For every complex return type, create a validator class in the same test project:
   ```csharp
   internal static class {ReturnType}Validator
   {
       public static void Validate(this {ReturnType} actual, {ReturnType} expected)
       {
           actual.Property1.Should().Be(expected.Property1);
           actual.Property2.Should().Be(expected.Property2);
       }
   }
   ```
   Only use inline `.Should()` assertions for single primitive results (e.g. `result.Should().Be("ok")`).

6. Do not test private methods. Do not test `Module.cs`.

7. If `ILogger<T>` is a dependency, inject `Mock<ILogger<T>>` — do not assert on log calls unless logging is the explicit behavior under test.
