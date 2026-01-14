using Azure;
using Azure.AI.OpenAI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TheAssistant.Agents.ServiceAdapter.Agenda;
using TheAssistant.Agents.ServiceAdapter.Agenda.Events;
using TheAssistant.Agents.ServiceAdapter.Authentication;
using TheAssistant.Agents.ServiceAdapter.AzureCosts;
using TheAssistant.Agents.ServiceAdapter.DailyUpdate;
using TheAssistant.Agents.ServiceAdapter.Formatting;
using TheAssistant.Agents.ServiceAdapter.Orchestration;
using TheAssistant.Agents.ServiceAdapter.Routing;
using TheAssistant.Agents.ServiceAdapter.Weather;
using TheAssistant.Core;
using TheAssistant.Core.Agents;
using TheAssistant.Agents.ServiceAdapter.AI;

namespace TheAssistant.Agents.ServiceAdapter
{
    public static class Module
    {
        public static IServiceCollection AddAgentServices(this IServiceCollection services, Action<AgentsSettings> options)
        {
            services.AddOptions<AgentsSettings>()
                .Configure(options)
                .ValidateDataAnnotations()
                .ValidateOnStart();

            services.AddMemoryCache();

            services.AddTransient<IEventService, EventService>();
            services.AddSingleton<ILoginUrlProvider, LoginUrlProvider>();

            services.AddSingleton<IChatClient>(sp =>
            {
                var settings = sp.GetRequiredService<IOptions<AgentsSettings>>().Value;
                var logger = sp.GetRequiredService<ILogger<IChatClient>>();

                var credential = new AzureKeyCredential(settings.AzureOpenAiApiKey);
                var azureClient = new AzureOpenAIClient(
                    new Uri(settings.AzureOpenAiEndpoint), 
                    credential);

                var chatClient = azureClient.GetChatClient(settings.AzureOpenAiDeploymentName);

                return chatClient.AsChatClient();
            });

            services.AddSingleton<IChatCompletionService, MicrosoftAgentsChatCompletionService>();

            services.AddSingleton<IRoutingAgent>(sp =>
            {
                var chat = sp.GetRequiredService<IChatCompletionService>();
                var logger = sp.GetRequiredService<ILogger<RoutingAgent>>();
                return new RoutingAgent(chat, logger);
            });

            services.AddSingleton<IDailyUpdateAgent, DailyUpdateAgent>();

            services.AddSingleton<IAgendaAgent>(sp =>
            {
                var chat = sp.GetRequiredService<IChatCompletionService>();
                var agent = new AgendaAgent(
                    chat,
                    sp.GetRequiredService<ITokenStoreServiceAdapter>(),
                    sp.GetRequiredService<ILoginUrlProvider>(),
                    sp.GetRequiredService<ILogger<AgendaAgent>>(),
                    sp.GetRequiredService<IEventService>());

                return agent;
            });

            services.AddSingleton<IWeatherAgent>(sp =>
            {
                var chat = sp.GetRequiredService<IChatCompletionService>();
                var logger = sp.GetRequiredService<ILogger<WeatherAgent>>();
                var agent = new WeatherAgent(sp.GetRequiredService<IWeatherServiceAdapter>(), chat, logger);
                return agent;
            });

            services.AddSingleton<IAzureCostAgent>(sp =>
            {
                var chat = sp.GetRequiredService<IChatCompletionService>();
                var adapter = sp.GetRequiredService<IAzureCostServiceAdapter>();
                var logger = sp.GetRequiredService<ILogger<AzureCostAgent>>();
                return new AzureCostAgent(adapter, chat, logger);
            });

            services.AddSingleton<IFormattingAgent>(sp =>
            {
                var chat = sp.GetRequiredService<IChatCompletionService>();
                var agent = new FormattingAgent(chat);
                return agent;
            });

            services.AddSingleton<AgentOrchestrator>(sp =>
            {
                var agents = new List<IAgent>
                {
                    sp.GetRequiredService<IAgendaAgent>(),
                    sp.GetRequiredService<IWeatherAgent>(),
                    sp.GetRequiredService<IAzureCostAgent>(),
                    sp.GetRequiredService<IDailyUpdateAgent>()
                };

                var router = sp.GetRequiredService<IRoutingAgent>();
                var formattingAgent = sp.GetRequiredService<IFormattingAgent>();
                var logger = sp.GetRequiredService<ILogger<AgentOrchestrator>>();

                return new AgentOrchestrator(agents, router, formattingAgent, logger);
            });

            services.AddTransient<IAgentServiceAdapter, AgentServiceAdapter>();

            return services;
        }
    }
}
