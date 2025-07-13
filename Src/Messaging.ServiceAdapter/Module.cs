using Microsoft.Extensions.DependencyInjection;
using TheAssistant.Core;

namespace TheAssistant.Messaging.ServiceAdapter
{
    public static class Module
    {
        public static IServiceCollection AddMessagingServices(this IServiceCollection services, Action<SignalSettings> SignalSettings)
        {
            services.AddOptions<SignalSettings>().Configure(SignalSettings).ValidateDataAnnotations();

            services.AddTransient<ISignalApiClient, SignalApiClient>();
			services.AddTransient<IMessageServiceAdapter, SignalServiceAdapter>();

            return services;
        }
    }
}
