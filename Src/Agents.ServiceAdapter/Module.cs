using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using TheAssistant.Agents.ServiceAdapter.Agenda;
using TheAssistant.Agents.ServiceAdapter.Authentication;
using TheAssistant.Agents.ServiceAdapter.DailyUpdate;
using TheAssistant.Agents.ServiceAdapter.Formatting;
using TheAssistant.Agents.ServiceAdapter.Routing;
using TheAssistant.Agents.ServiceAdapter.Weather;
using TheAssistant.Core;
using TheAssistant.Core.Agents;

namespace TheAssistant.Agents.ServiceAdapter
{
    public static class Module
    {
        public static IServiceCollection AddAgentServices(this IServiceCollection services, Action<AgentsOptions> options)
        {
            services.AddOptions<AgentsOptions>().Configure(options).ValidateDataAnnotations();

            services.AddSingleton<IOneTimeTokenStore, InMemoryOneTimeTokenStore>();
            services.AddSingleton<ILoginUrlProvider, LoginUrlProvider>();

            services.AddSingleton<IAgentRouter, AgentRouter>();
            services.AddSingleton<IAgendaAgent, AgendaAgent>();
            services.AddSingleton<IWeatherAgent, WeatherAgent>();
            services.AddSingleton<IDailyUpdateAgent, DailyUpdateAgent>();
            services.AddSingleton<IFormattingAgent, FormattingAgent>();
            services.AddSingleton<Kernel>(sp => sp.CreateKernel(sp.GetRequiredService<IOptions<AgentsOptions>>()));

            services.AddTransient<IAgentServiceAdapter>(sp =>
            {
                var agents = new List<IAgent>
                {
                    sp.GetRequiredService<IAgendaAgent>(),
                    sp.GetRequiredService<IWeatherAgent>(),
                    sp.GetRequiredService<IDailyUpdateAgent>()
                };

                var agentRouter = sp.GetRequiredService<IAgentRouter>();
                var formattingAgent = sp.GetRequiredService<IFormattingAgent>();
                var logger = sp.GetRequiredService<ILogger<AgentServiceAdapter>>();

                return new AgentServiceAdapter(agentRouter, agents, formattingAgent, logger);
            });

            return services;
        }

        private static Kernel CreateKernel(this IServiceProvider services, IOptions<AgentsOptions> options)
        {
            var agentOptions = options.Value;
            var builder = Kernel.CreateBuilder();

            builder.AddAzureOpenAIChatCompletion(agentOptions.AzureOpenAiDeploymentName, agentOptions.AzureOpenAiEndpoint, agentOptions.AzureOpenAiApiKey);

            var kernel = builder.Build();

            kernel.Plugins.AddFromObject(new AgendaAgent(services.GetRequiredService<IAgendaServiceAdapter>(), kernel, services.GetRequiredService<ITokenStoreServiceAdapter>(), services.GetRequiredService<ILoginUrlProvider>()));
            kernel.Plugins.AddFromObject(new WeatherAgent(services.GetRequiredService<IWeatherServiceAdapter>(), kernel));
            kernel.Plugins.AddFromObject(new FormattingAgent(kernel));

            return kernel;
        }
    }
}
