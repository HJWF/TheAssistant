using Microsoft.Extensions.Logging;
using TheAssistant.Agents.ServiceAdapter.Formatting;
using TheAssistant.Core.Agents;
using TheAssistant.Core.Infrastructure;

namespace TheAssistant.Agents.ServiceAdapter.Orchestration
{
    public class AgentOrchestrator
    {
        private readonly IEnumerable<IAgent> _agents;
        private readonly IRoutingAgent _router;
        private readonly IFormattingAgent _formattingAgent;
        private readonly ILogger<AgentOrchestrator> _logger;

        public AgentOrchestrator(
            IEnumerable<IAgent> agents,
            IRoutingAgent router,
            IFormattingAgent formattingAgent,
            ILogger<AgentOrchestrator> logger)
        {
            _agents = agents ?? throw new ArgumentNullException(nameof(agents));
            _router = router ?? throw new ArgumentNullException(nameof(router));
            _formattingAgent = formattingAgent ?? throw new ArgumentNullException(nameof(formattingAgent));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<string> ExecuteAsync(
            string userInput,
            UserDetails user,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting orchestration for user {UserId}", user.PersonalMailTag);

            try
            {
                // Step 1: Route the request to determine which agents to invoke
                var routes = await _router.RouteAsync(userInput, user);

                if (routes == null || routes.Count == 0)
                {
                    _logger.LogWarning("No agents were routed for input: {Input}", userInput);
                    return AgentConstants.SorryMessage;
                }

                _logger.LogInformation("Routing completed. {AgentCount} agent(s) will be invoked", routes.Count);

                // Step 2: Group routes by dependency (parallel vs sequential)
                var executionPlan = CreateExecutionPlan(routes);

                // Step 3: Execute agents according to plan
                var results = await ExecutePlanAsync(executionPlan, cancellationToken);

                // Step 4: Format and combine results
                var formattedResponse = await FormatResultsAsync(results, cancellationToken);

                _logger.LogInformation("Orchestration completed successfully");
                return formattedResponse;
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Orchestration was cancelled for user {UserId}", user.PersonalMailTag);
                return "Request cancelled.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Orchestration failed for user {UserId}", user.PersonalMailTag);
                return AgentConstants.SorryMessage;
            }
        }

        private ExecutionPlan CreateExecutionPlan(List<AgentMessage> routes)
        {
            var plan = new ExecutionPlan();

            foreach (var route in routes)
            {
                var hasDependency = route.Metadata?.ContainsKey("dependsOn") ?? false;

                if (hasDependency)
                {
                    plan.SequentialTasks.Add(route);
                }
                else
                {
                    plan.ParallelTasks.Add(route);
                }
            }

            _logger.LogDebug(
                "Execution plan created: {ParallelCount} parallel, {SequentialCount} sequential",
                plan.ParallelTasks.Count,
                plan.SequentialTasks.Count);

            return plan;
        }

        private async Task<List<AgentExecutionResult>> ExecutePlanAsync(
            ExecutionPlan plan,
            CancellationToken cancellationToken)
        {
            var results = new List<AgentExecutionResult>();

            if (plan.ParallelTasks.Any())
            {
                _logger.LogDebug("Executing {Count} agents in parallel", plan.ParallelTasks.Count);

                var parallelTasks = plan.ParallelTasks.Select(route =>
                    ExecuteAgentWithRetryAsync(route, cancellationToken));

                var parallelResults = await Task.WhenAll(parallelTasks);
                results.AddRange(parallelResults);
            }

            if (plan.SequentialTasks.Any())
            {
                _logger.LogDebug("Executing {Count} agents sequentially", plan.SequentialTasks.Count);

                foreach (var route in plan.SequentialTasks)
                {
                    var result = await ExecuteAgentWithRetryAsync(route, cancellationToken);
                    results.Add(result);

                    if (!result.Success && result.IsCritical)
                    {
                        _logger.LogWarning("Critical sequential task failed, stopping execution");
                        break;
                    }
                }
            }

            return results;
        }

        private async Task<AgentExecutionResult> ExecuteAgentWithRetryAsync(
            AgentMessage route,
            CancellationToken cancellationToken,
            int maxRetries = 2)
        {
            var agent = _agents.FirstOrDefault(a => a.Name == route.Receiver);

            if (agent == null)
            {
                _logger.LogWarning("Agent not found: {AgentName}", route.Receiver);
                return new AgentExecutionResult
                {
                    AgentName = route.Receiver,
                    Success = false,
                    ErrorMessage = $"Agent '{route.Receiver}' not found"
                };
            }

            for (int attempt = 0; attempt <= maxRetries; attempt++)
            {
                try
                {
                    _logger.LogDebug(
                        "Executing agent {AgentName} (attempt {Attempt}/{MaxAttempts})",
                        agent.Name,
                        attempt + 1,
                        maxRetries + 1);

                    var messages = await agent.HandleAsync(route, cancellationToken);

                    return new AgentExecutionResult
                    {
                        AgentName = agent.Name,
                        Success = true,
                        Messages = messages.ToList()
                    };
                }
                catch (OperationCanceledException)
                {
                    _logger.LogWarning("Agent {AgentName} execution was cancelled", agent.Name);
                    throw;
                }
                catch (Exception ex) when (attempt < maxRetries && IsRetriableError(ex))
                {
                    _logger.LogWarning(
                        ex,
                        "Agent {AgentName} failed on attempt {Attempt} with retriable error, retrying...",
                        agent.Name,
                        attempt + 1);

                    await Task.Delay(TimeSpan.FromMilliseconds(100 * Math.Pow(2, attempt)), cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Agent {AgentName} failed after {MaxRetries} retries", agent.Name, maxRetries + 1);

                    return new AgentExecutionResult
                    {
                        AgentName = agent.Name,
                        Success = false,
                        ErrorMessage = ex.Message
                    };
                }
            }

            return new AgentExecutionResult
            {
                AgentName = agent.Name,
                Success = false,
                ErrorMessage = "Unexpected execution path"
            };
        }

        private static bool IsRetriableError(Exception ex)
        {
            var message = ex.Message.ToLowerInvariant();
            return message.Contains("timeout") ||
                   message.Contains("network") ||
                   message.Contains("connection") ||
                   message.Contains("503") ||
                   message.Contains("502") ||
                   ex is TimeoutException ||
                   ex is HttpRequestException;
        }

        private async Task<string> FormatResultsAsync(
            List<AgentExecutionResult> results,
            CancellationToken cancellationToken)
        {
            var successfulResults = results.Where(r => r.Success).ToList();

            if (!successfulResults.Any())
            {
                _logger.LogWarning("No successful agent results to format");
                return AgentConstants.SorryMessage;
            }

            var agentResponses = successfulResults
                .SelectMany(r => r.Messages)
                .Select(msg => new AgentResponse(msg.Sender, msg.Content))
                .ToList();

            return await _formattingAgent.HandleAsync(agentResponses, cancellationToken);
        }
    }
}
