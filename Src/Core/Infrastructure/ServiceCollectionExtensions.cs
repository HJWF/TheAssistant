using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace TheAssistant.Core.Infrastructure
{
    internal static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddTransient(this IServiceCollection services, Type openGenericType, Assembly assembly)
        {
            foreach (var type in assembly.GetTypes())
            {
                foreach (var implementedInterface in type.GetInterfaces())
                {
                    if (implementedInterface.IsGenericType && implementedInterface.GetGenericTypeDefinition() == openGenericType
                                                           && !type.ContainsGenericParameters)
                    {
                        services.AddTransient(implementedInterface, type);
                    }
                }
            }

            return services;
        }
    }
}
