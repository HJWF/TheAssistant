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
using TheAssistant.Agents.ServiceAdapter.Notion;
using TheAssistant.Agents.ServiceAdapter.MealPlan;

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

            services.AddSingleton<ITokenUsageTracker, TokenUsageTracker>();

            services.AddTransient<IEventService, EventService>();
            services.AddSingleton<ILoginUrlProvider, LoginUrlProvider>();

            services.AddSingleton<AzureOpenAIClient>(sp =>
            {
                var settings = sp.GetRequiredService<IOptions<AgentsSettings>>().Value;
                return new AzureOpenAIClient(
                    new Uri(settings.AzureOpenAiEndpoint),
                    new AzureKeyCredential(settings.AzureOpenAiApiKey));
            });

            services.AddSingleton<IChatClientFactory>(sp =>
            {
                var settings = sp.GetRequiredService<IOptions<AgentsSettings>>().Value;
                var azureClient = sp.GetRequiredService<AzureOpenAIClient>();
                var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
                return new ChatClientFactory(settings, azureClient, loggerFactory);
            });

            services.AddSingleton<IChatClient>(sp =>
            {
                var settings = sp.GetRequiredService<IOptions<AgentsSettings>>().Value;
                var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
                var azureClient = sp.GetRequiredService<AzureOpenAIClient>();

                var chatClient = azureClient.GetChatClient(settings.AzureOpenAiDeploymentName);

                return new ChatClientBuilder(chatClient.AsIChatClient())
                    .UseFunctionInvocation()
                    .UseLogging(loggerFactory)
                    .UseOpenTelemetry()
                    .Build();
            });

            services.AddSingleton<IRoutingAgent>(sp =>
            {
                var agents = new List<IAgent>
                {
                    sp.GetRequiredService<IAgendaAgent>(),
                    sp.GetRequiredService<IWeatherAgent>(),
                    sp.GetRequiredService<IAzureCostAgent>(),
                    sp.GetRequiredService<IDailyUpdateAgent>(),
                    sp.GetRequiredService<INotionAgent>(),
                    sp.GetRequiredService<IMealPlanAgent>()
                };

                var chatClient = sp.GetRequiredService<IChatClient>();
                var logger = sp.GetRequiredService<ILogger<RoutingAgent>>();
                return new RoutingAgent(agents, chatClient, logger);
            });
            services.AddSingleton<IDailyUpdateAgent, DailyUpdateAgent>();
            services.AddSingleton<IAgendaAgent, AgendaAgent>();
            services.AddSingleton<IWeatherAgent, WeatherAgent>();
            services.AddSingleton<IAzureCostAgent, AzureCostAgent>();
            services.AddSingleton<IFormattingAgent, FormattingAgent>();
            services.AddSingleton<INotionAgent, NotionAgent>();

            services.AddSingleton<MealPlanCouncil>(sp =>
            {
                var factory = sp.GetRequiredService<IChatClientFactory>();
                var synthesizer = sp.GetRequiredService<IChatClient>();
                var logger = sp.GetRequiredService<ILogger<MealPlanCouncil>>();

                var tracker = sp.GetRequiredService<ITokenUsageTracker>();

                var members = new List<CouncilMember>
                {
                    new(CouncilMemberKeys.NutritionExpert, CouncilPrompts.NutritionExpert, factory.GetClient(CouncilMemberKeys.NutritionExpert), tracker),
                    new(CouncilMemberKeys.GymTrainer,      CouncilPrompts.GymTrainer,      factory.GetClient(CouncilMemberKeys.GymTrainer),      tracker),
                    new(CouncilMemberKeys.Parent,          CouncilPrompts.Parent,          factory.GetClient(CouncilMemberKeys.Parent),          tracker),
                    new(CouncilMemberKeys.Chef,            CouncilPrompts.Chef,            factory.GetClient(CouncilMemberKeys.Chef),            tracker),
                    new(CouncilMemberKeys.BudgetAdvisor,   CouncilPrompts.BudgetAdvisor,   factory.GetClient(CouncilMemberKeys.BudgetAdvisor),   tracker)
                };

                return new MealPlanCouncil(members, synthesizer, logger, tracker);
            });

            services.AddSingleton<IMealPlanAgent, MealPlanAgent>();

            services.AddSingleton<AgentOrchestrator>(sp =>
            {
                var agents = new List<IAgent>
                {
                    sp.GetRequiredService<IAgendaAgent>(),
                    sp.GetRequiredService<IWeatherAgent>(),
                    sp.GetRequiredService<IAzureCostAgent>(),
                    sp.GetRequiredService<IDailyUpdateAgent>(),
                    sp.GetRequiredService<INotionAgent>(),
                    sp.GetRequiredService<IMealPlanAgent>()
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
