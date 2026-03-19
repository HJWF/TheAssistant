using Microsoft.Extensions.DependencyInjection;
using TheAssistant.Core.Infrastructure;

namespace TheAssistant.Core
{
    public static class Module
    {
        public static IServiceCollection AddCoreServices(this IServiceCollection services, Action<LoginSettings> settings)
        {
            services.AddOptions<LoginSettings>().Configure(settings).ValidateDataAnnotations();

            services.AddTransient(typeof(ICommandHandler<>), typeof(Module).Assembly);
            services.AddTransient(typeof(IQueryHandler<,>), typeof(Module).Assembly);

            return services;
        }
    }
}
