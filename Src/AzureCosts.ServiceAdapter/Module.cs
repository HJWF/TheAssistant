using Azure.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TheAssistant.Core;

namespace TheAssistant.AzureCosts.ServiceAdapter;

public static class Module
{
    public static IServiceCollection AddAzureCostsServices(
        this IServiceCollection services,
        Action<AzureCostSettings> configure,
        TokenCredential tokenCredential)
    {
        services.AddOptions<AzureCostSettings>()
            .Configure(configure)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<IAzureCostServiceAdapter>(sp =>
            new AzureCostServiceAdapter(
                sp.GetRequiredService<IOptions<AzureCostSettings>>(),
                sp.GetRequiredService<ILogger<AzureCostServiceAdapter>>(),
                sp.GetRequiredService<IHttpClientFactory>(),
                tokenCredential));

        return services;
    }
}
