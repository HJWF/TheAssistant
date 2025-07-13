using Microsoft.Extensions.DependencyInjection;
using TheAssistant.Core;

namespace TheAssistant.Weather.ServiceAdapter
{
    public static class Module
    {
        public static IServiceCollection AddWeatherServices(this IServiceCollection services)
        {
            //services.AddOptions<SignalSettings>().Configure(SignalSettings).ValidateDataAnnotations();

            services.AddTransient<IWeatherServiceAdapter, WeatherServiceAdapter>();

            return services;
        }
    }
}
