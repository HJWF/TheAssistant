using System.Collections.Concurrent;
using Azure.AI.OpenAI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace TheAssistant.Agents.ServiceAdapter
{
    public class ChatClientFactory : IChatClientFactory
    {
        private readonly AgentsSettings _settings;
        private readonly AzureOpenAIClient _azureClient;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ConcurrentDictionary<string, IChatClient> _cache = new();

        public ChatClientFactory(AgentsSettings settings, AzureOpenAIClient azureClient, ILoggerFactory loggerFactory)
        {
            _settings = settings;
            _azureClient = azureClient;
            _loggerFactory = loggerFactory;
        }

        public IChatClient GetClient(string modelKey)
        {
            return _cache.GetOrAdd(modelKey, key =>
            {
                var deploymentName = _settings.Models.TryGetValue(key, out var profile)
                    ? profile.DeploymentName
                    : _settings.AzureOpenAiDeploymentName;

                return new ChatClientBuilder(
                        _azureClient.GetChatClient(deploymentName).AsIChatClient())
                    .UseFunctionInvocation()
                    .UseLogging(_loggerFactory)
                    .UseOpenTelemetry()
                    .Build();
            });
        }
    }
}
