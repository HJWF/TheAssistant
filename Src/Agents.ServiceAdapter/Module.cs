using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using TheAssistant.Core;
using TheAssistant.Agents.ServiceAdapter.Agenda;
using TheAssistant.Agents.ServiceAdapter.Routing;
using TheAssistant.Agents.ServiceAdapter.Weather;
using TheAssistant.Agents.ServiceAdapter.Formatting;

namespace TheAssistant.Agents.ServiceAdapter
{
    public static class Module
    {
        public static IServiceCollection AddAgentServices(this IServiceCollection services, Action<AgentsOptions> options)
        {
            services.AddOptions<AgentsOptions>().Configure(options).ValidateDataAnnotations();

            services.AddSingleton<IRouter, LlmRouter>();
            services.AddSingleton<IAgendaAgent, AgendaAgent>();
            services.AddSingleton<IWeatherAgent, WeatherAgent>();
            services.AddSingleton<IFormattingAgent, FormattingAgent>();
            services.AddSingleton<Kernel>(sp => sp.CreateKernel(sp.GetRequiredService<IOptions<AgentsOptions>>()));

            services.AddTransient<IAgentServiceAdapter, AgentServiceAdapter>();
            return services;
        }

        private static Kernel CreateKernel(this IServiceProvider services, IOptions<AgentsOptions> options)
        {
            var agentOptions = options.Value;
            var builder = Kernel.CreateBuilder();

            builder.AddAzureOpenAIChatCompletion(agentOptions.AzureOpenAiDeploymentName, agentOptions.AzureOpenAiEndpoint, agentOptions.AzureOpenAiApiKey);

            var kernel = builder.Build();

            kernel.Plugins.AddFromObject(new AgendaAgent(services.GetRequiredService<IAgendaServiceAdapter>(), kernel));
            kernel.Plugins.AddFromObject(new WeatherAgent(services.GetRequiredService<IWeatherServiceAdapter>(), kernel));
            kernel.Plugins.AddFromObject(new FormattingAgent(kernel));

            return kernel;
        }
    }
}
