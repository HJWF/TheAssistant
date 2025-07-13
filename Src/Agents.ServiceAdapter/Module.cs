using Microsoft.Extensions.DependencyInjection;using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using TheAssistant.Core;
using TheAssistant.Core.Agents;
using TheAssistant.Agents.ServiceAdapter.Agenda;

namespace TheAssistant.Agents.ServiceAdapter
{
    public static class Module
    {
        public static IServiceCollection AddAgentServices(this IServiceCollection services, Action<AgentsOptions> options)
        {
            services.AddOptions<AgentsOptions>().Configure(options).ValidateDataAnnotations();

            services.AddSingleton<IRouter, LlmRouter>();
            services.AddSingleton<IAgendaAgent, AgendaAgent>();
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

            var adapter = services.GetRequiredService<IAgendaServiceAdapter>();
            kernel.Plugins.AddFromObject(new AgendaAgent(adapter, kernel));

            return kernel;
        }
    }
}
