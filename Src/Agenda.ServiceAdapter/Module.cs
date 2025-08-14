using Microsoft.Extensions.DependencyInjection;
using TheAssistant.Core;

namespace TheAssistant.Agenda.ServiceAdapter
{
    public static class Module
    {
        public static IServiceCollection AddAgendaServices(this IServiceCollection services)
        {
            services.AddTransient<IAgendaServiceAdapter, AgendaServiceAdapter>();

            return services;
        }
    }
}
