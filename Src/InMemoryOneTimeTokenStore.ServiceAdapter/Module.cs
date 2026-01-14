using Microsoft.Extensions.DependencyInjection;
using TheAssistant.Core;

namespace TheAssistant.InMemoryOneTimeTokenStore.ServiceAdapter
{
    public static class Module
    {
        public static IServiceCollection AddOneTimeTokenStoreServices(this IServiceCollection services)
        {
            services.AddSingleton<IOneTimeTokenStoreServiceAdapter, InMemoryOneTimeTokenStoreServiceAdapter>();

            return services;
        }
    }
}
