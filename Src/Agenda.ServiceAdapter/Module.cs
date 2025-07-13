using Microsoft.Extensions.DependencyInjection;
using TheAssistant.Core;

namespace TheAssistant.Agenda.ServiceAdapter
{
    public static class Module
    {
        public static IServiceCollection AddAgendaServices(this IServiceCollection services)
        {
            //services.AddOptions<SignalSettings>().Configure(SignalSettings).ValidateDataAnnotations();

            services.AddTransient<IAgendaServiceAdapter, AgendaServiceAdapter>();

            return services;
        }
    }
}
