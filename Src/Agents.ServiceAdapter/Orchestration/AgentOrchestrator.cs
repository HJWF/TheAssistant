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
        private const int MaxIterations = 10;

        public AgentOrchestrator(IEnumerable<IAgent> agents, IRoutingAgent router, IFormattingAgent formattingAgent, ILogger<AgentOrchestrator> logger)
        {
            _agents = agents ?? throw new ArgumentNullException(nameof(agents));
            _router = router ?? throw new ArgumentNullException(nameof(router));
            _formattingAgent = formattingAgent ?? throw new ArgumentNullException(nameof(formattingAgent));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<string> ExecuteAsync(string userInput, UserDetails user, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting orchestration for user {UserId}", user.PersonalMailTag);

            try
            {
                var routes = await _router.RouteAsync(userInput, user);

                if (routes == null || routes.Count == 0)
                {
                    _logger.LogWarning("No agents were routed for input: {Input}", userInput);
                    return AgentConstants.SorryMessage;
                }

                _logger.LogInformation("Routing completed. {AgentCount} agent(s) will be invoked", routes.Count);

                var finalResults = await ExecuteWithAgentCommunicationAsync(routes, cancellationToken);

                var formattedResponse = await FormatResultsAsync(finalResults, cancellationToken);

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

        private async Task<List<AgentExecutionResult>> ExecuteWithAgentCommunicationAsync(List<AgentMessage> initialMessages, CancellationToken cancellationToken)
        {
            var pendingMessages = new Queue<AgentMessage>(initialMessages);
            var allResults = new List<AgentExecutionResult>();
            var iteration = 0;

            while (pendingMessages.Any() && iteration < MaxIterations)
            {
                iteration++;
                _logger.LogDebug("Agent communication iteration {Iteration}, {PendingCount} messages to process", iteration, pendingMessages.Count);

                var currentBatch = new List<AgentMessage>();
                while (pendingMessages.Any())
                {
                    currentBatch.Add(pendingMessages.Dequeue());
                }

                var executionPlan = CreateExecutionPlan(currentBatch);
                var results = await ExecutePlanAsync(executionPlan, cancellationToken);

                allResults.AddRange(results);

                foreach (var result in results.Where(r => r.Success && r.Messages.Any()))
                {
                    foreach (var message in result.Messages)
                    {
                        if (IsAgentToAgentMessage(message))
                        {
                            _logger.LogDebug("Agent {From} sent message to agent {To}", message.Sender, message.Receiver);
                            pendingMessages.Enqueue(message);
                        }
                    }
                }
            }

            if (iteration >= MaxIterations)
            {
                _logger.LogWarning("Max iterations reached in agent communication loop");
            }

            return allResults;
        }

        private bool IsAgentToAgentMessage(AgentMessage message)
        {
            var knownAgentNames = _agents.Select(a => a.Name).ToList();
            
            return knownAgentNames.Contains(message.Receiver);
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

            _logger.LogDebug("Execution plan created: {ParallelCount} parallel, {SequentialCount} sequential", plan.ParallelTasks.Count, plan.SequentialTasks.Count);

            return plan;
        }

        private async Task<List<AgentExecutionResult>> ExecutePlanAsync(ExecutionPlan plan, CancellationToken cancellationToken)
        {
            var results = new List<AgentExecutionResult>();
            await AddParallelTasks(plan, results, cancellationToken);
            await AddSequentialTasks(plan, results, cancellationToken);

            return results;
        }

        private async Task AddSequentialTasks(ExecutionPlan plan, List<AgentExecutionResult> results, CancellationToken cancellationToken)
        {
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
        }

        private async Task AddParallelTasks(ExecutionPlan plan, List<AgentExecutionResult> results, CancellationToken cancellationToken)
        {
            if (plan.ParallelTasks.Any())
            {
                _logger.LogDebug("Executing {Count} agents in parallel", plan.ParallelTasks.Count);

                var parallelTasks = plan.ParallelTasks.Select(route => ExecuteAgentWithRetryAsync(route, cancellationToken));

                var parallelResults = await Task.WhenAll(parallelTasks);
                results.AddRange(parallelResults);
            }
        }

        private async Task<AgentExecutionResult> ExecuteAgentWithRetryAsync(AgentMessage route, CancellationToken cancellationToken, int maxRetries = 2)
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
                    _logger.LogDebug("Executing agent {AgentName} (attempt {Attempt}/{MaxAttempts})", agent.Name, attempt + 1, maxRetries + 1);

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
                    _logger.LogWarning(ex, "Agent {AgentName} failed on attempt {Attempt} with retriable error, retrying...", agent.Name, attempt + 1);

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

        private async Task<string> FormatResultsAsync(List<AgentExecutionResult> results, CancellationToken cancellationToken)
        {
            var successfulResults = results.Where(r => r.Success).ToList();

            if (!successfulResults.Any())
            {
                _logger.LogWarning("No successful agent results to format");
                return AgentConstants.SorryMessage;
            }

            var finalMessages = successfulResults.SelectMany(r => r.Messages).Where(msg => !IsAgentToAgentMessage(msg)).ToList();

            if (!finalMessages.Any())
            {
                _logger.LogWarning("No final user-facing messages to format");
                return AgentConstants.SorryMessage;
            }

            var agentResponses = finalMessages.Select(msg => new AgentResponse(msg.Sender, msg.Content)).ToList();

            return await _formattingAgent.HandleAsync(agentResponses, cancellationToken);
        }
    }
}
