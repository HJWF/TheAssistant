using Microsoft.Extensions.DependencyInjection;
using TheAssistant.TheAssistantApi.Login.Infrastructure;

namespace TheAssistant.TheAssistantApi.Login
{
    public static class Module
    {
        public static IServiceCollection AddApiServices(this IServiceCollection services, Action<UserDetailsSettings> userSettings)
        {
            services.AddOptions<UserDetailsSettings>().Configure(userSettings).ValidateDataAnnotations();

            return services;
        }
    }
}
