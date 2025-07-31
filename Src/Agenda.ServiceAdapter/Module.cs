using Microsoft.Extensions.DependencyInjection;
using TheAssistant.Agenda.ServiceAdapter.Graph;
using TheAssistant.Core;

namespace TheAssistant.Agenda.ServiceAdapter
{
    public static class Module
    {
        public static IServiceCollection AddAgendaServices(this IServiceCollection services, Action<AgendaSettings> agendaSettings)
        {
            services.AddOptions<AgendaSettings>().Configure(agendaSettings).ValidateDataAnnotations();

            services.AddTransient<IAgendaServiceAdapter, AgendaServiceAdapter>();

            return services;
        }
    }
}
