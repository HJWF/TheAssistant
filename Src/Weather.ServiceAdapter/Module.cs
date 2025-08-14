using Microsoft.Extensions.DependencyInjection;
using TheAssistant.Core;

namespace TheAssistant.Weather.ServiceAdapter
{
    public static class Module
    {
        public static IServiceCollection AddWeatherServices(this IServiceCollection services)
        {
            services.AddTransient<IWeatherClient, WeatherClient>();
            services.AddTransient<IWeatherServiceAdapter, WeatherServiceAdapter>();

            return services;
        }
    }
}
