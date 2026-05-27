using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Notion.Client;
using TheAssistant.Core;
using TheAssistant.Core.Infrastructure;

namespace TheAssistant.Notion.ServiceAdapter;

public static class Module
{
    public static IServiceCollection AddNotionServices(this IServiceCollection services, Action<NotionSettings> configure)
    {
        services.AddOptions<NotionSettings>()
            .Configure(configure)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<INotionClient>(sp =>
        {
            var settings = sp.GetRequiredService<IOptions<NotionSettings>>().Value;
            
            return NotionClientFactory.Create(new ClientOptions
            {
                AuthToken = settings.ClientSecret
            });
        });

        services.AddSingleton<NotionMcpServer>();
        services.AddSingleton<INotionServiceAdapter, NotionServiceAdapter>();

        return services;
    }
}
