using Azure.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TheAssistant.Core;

namespace TheAssistant.ServiceBus.ServiceAdapter
{
    public static class Module
    {
        public static IServiceCollection AddServiceBusServices(this IServiceCollection services, Action<ServiceBusSettings> SignalSettings, TokenCredential credential)
        {
            services.AddOptions<ServiceBusSettings>().Configure(SignalSettings).ValidateDataAnnotations();

            services.AddTransient<IServiceBusClient>(sp =>
            {
                var settings = sp.GetRequiredService<IOptions<ServiceBusSettings>>().Value;
                return new ServiceBusClient(credential, settings.FullyQualifiedNamespace);
            });
            services.AddTransient<IServiceBusServiceAdapter, ServiceBusServiceAdapter>();

            return services;
        }
    }
}
