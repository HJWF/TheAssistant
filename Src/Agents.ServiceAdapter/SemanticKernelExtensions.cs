using Microsoft.Extensions.DependencyInjection;

namespace TheAssistant.Agents.ServiceAdapter
{
    public static class SemanticKernelExtensions
    {
        public static IServiceCollection AddKernelWithPlugins(this IServiceCollection services)
        {
            // Configure Semantic Kernel + Plugins
            return services;
        }
    }
}
