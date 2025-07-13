using Azure.Core;
using Azure.Identity;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TheAssistant.Agenda.ServiceAdapter;
using TheAssistant.Agents.ServiceAdapter;
using TheAssistant.Core;
using TheAssistant.Messaging.ServiceAdapter;
using TheAssistant.ServiceBus.ServiceAdapter;
using TheAssistant.TheAssistantApi.Infrastructure;

namespace TheAssistant.TheAssistantApi
{
    public class Program
    {
        private static async Task Main(string[] args)
        {
            var builder = Host.CreateDefaultBuilder(args)
                .ConfigureFunctionsWebApplication((_, builder) => { })
                .ConfigureAppConfiguration((context, config) =>
                {
                    config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                          .AddUserSecrets<Program>(optional: true)
                          .AddEnvironmentVariables();
                })
                .ConfigureServices((builder, services) =>
                {
                    services.AddApplicationInsightsTelemetryWorkerService()
                        .ConfigureFunctionsApplicationInsights();

                    services.AddHttpClient();

                    var tokenCredential = GetToken(builder.Configuration, builder);

                    services.AddCoreServices();
                    services.AddAgendaServices();
                    services.AddMessagingServices(wao => builder.Configuration.GetSection("Signal").Bind(wao));
                    services.AddServiceBusServices(wao => builder.Configuration.GetSection("ServiceBus").Bind(wao), tokenCredential);
                    services.AddAgentServices(ao => builder.Configuration.GetSection("Agents").Bind(ao));
                });
            await builder.Build().RunAsync();
        }

        private static TokenCredential GetToken(IConfiguration configuration, HostBuilderContext builder)
        {
            var uamiOptions = configuration.GetSection("UserAssignedManagedIdentity").Get<UserAssignedManagedIdentityOptions>() ?? throw new Exception();

            if (builder.HostingEnvironment.IsDevelopment())
            {
                return new ChainedTokenCredential(new AzureCliCredential(), new DefaultAzureCredential(new DefaultAzureCredentialOptions() { TenantId = uamiOptions.TenantId }));
            }

            return new ManagedIdentityCredential(uamiOptions.ClientId);
        }
    }
}