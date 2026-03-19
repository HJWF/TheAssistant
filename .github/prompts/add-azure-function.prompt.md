---
mode: agent
description: Add a new Azure Function trigger endpoint (HTTP, Timer, or Service Bus) to TheAssistantApi following the isolated worker pattern.
---

Add a new Azure Function to TheAssistant. Follow the conventions used in `Src/TheAssistantApi/`.

## What to add

Function name: ${input:functionName:e.g. HandleWebhook}
Trigger type: ${input:triggerType:HTTP | Timer | ServiceBus}
Purpose: ${input:purpose:what this function does in one sentence}

## Steps

1. **Create the function file** in `Src/TheAssistantApi/{Feature}/{FunctionName}.cs`:
   - Use the isolated worker pattern (`[Function("...")]` attribute)
   - Inject the appropriate `ICommandHandler<TCommand>` via constructor
   - Delegate all logic to the command handler — keep the function class thin

   **HTTP example:**
   ```csharp
   public class {FunctionName}([FromServices] ICommandHandler<{Command}> handler)
   {
       [Function("{FunctionName}")]
       public async Task<HttpResponseData> RunAsync(
           [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req,
           CancellationToken cancellationToken)
       {
           var command = await req.ReadFromJsonAsync<{Command}>(cancellationToken);
           await handler.HandleAsync(command!, cancellationToken);
           return req.CreateResponse(HttpStatusCode.OK);
       }
   }
   ```

   **Timer example:**
   ```csharp
   public class {FunctionName}([FromServices] ICommandHandler<{Command}> handler)
   {
       [Function("{FunctionName}")]
       public async Task RunAsync(
           [TimerTrigger("0 0 7 * * *")] TimerInfo timer,
           CancellationToken cancellationToken)
       {
           await handler.HandleAsync(new {Command}(), cancellationToken);
       }
   }
   ```

2. **Define the command** in `Src/Core/Messaging/{Feature}/{FunctionName}Command.cs`:
   - Implement `ICommand`
   - Use `required` properties with `init` setters

3. **Define the handler** in `Src/Core/Messaging/{Feature}/{FunctionName}CommandHandler.cs`:
   - Implement `ICommandHandler<{FunctionName}Command>`
   - Inject service adapters via constructor
   - Log start/end/errors with structured logging

4. **Register the handler** in `Src/Core/Module.cs`:
   ```csharp
   services.AddTransient<ICommandHandler<{FunctionName}Command>, {FunctionName}CommandHandler>();
   ```

5. **Add a unit test** in `Tests/{relevant project}` following `#file:.github/instructions/testing.instructions.md`.
