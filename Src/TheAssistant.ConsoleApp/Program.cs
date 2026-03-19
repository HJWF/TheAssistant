using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TheAssistant.Agenda.ServiceAdapter;
using TheAssistant.Agents.ServiceAdapter;
using TheAssistant.AzureCosts.ServiceAdapter;
using TheAssistant.Core;
using TheAssistant.InMemoryOneTimeTokenStore.ServiceAdapter;
using TheAssistant.Messaging.ServiceAdapter;
using TheAssistant.ServiceBus.ServiceAdapter;
using TheAssistant.TokenStore.ServiceAdapter;
using TheAssistant.Weather.ServiceAdapter;

namespace TheAssistant.TheAssistant.ConsoleApp;

class Program
{
    static async Task Main(string[] args)
    {
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((context, config) =>
            {
                config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                      .AddUserSecrets<Program>(optional: true)
                      .AddEnvironmentVariables();
            })
            .ConfigureServices((context, services) =>
            {
                services.AddLogging(builder =>
                {
                    builder.AddConsole();
                    builder.SetMinimumLevel(LogLevel.Debug); // Changed from Information to Debug
                });

                services.AddHttpClient();

                var configuration = context.Configuration;
                var tokenCredential = GetTokenCredential(context.HostingEnvironment, configuration);

                // Register all service adapters like in TheAssistantApi
                services.AddCoreServices(ls => configuration.GetSection("Login").Bind(ls));
                services.AddAgendaServices();
                services.AddWeatherServices();
                services.AddAzureCostsServices(
                    acs => configuration.GetSection("AzureCosts").Bind(acs),
                    tokenCredential);
                services.AddOneTimeTokenStoreServices();
                services.AddMessagingServices(ss => configuration.GetSection("Signal").Bind(ss));
                services.AddServiceBusServices(sbs => configuration.GetSection("ServiceBus").Bind(sbs), tokenCredential);
                services.AddAgentServices(ags => configuration.GetSection("Agents").Bind(ags));
                services.AddTokenStoreServices(tss => configuration.GetSection("TokenStore").Bind(tss), tokenCredential);

                // Register UserDetails from configuration
                services.Configure<UserDetailsSettings>(configuration.GetSection("UserDetails"));

                // Register interactive menu runner
                services.AddTransient<InteractiveRunner>();
            })
            .Build();

        var runner = host.Services.GetRequiredService<InteractiveRunner>();
        await runner.RunAsync();
    }

    private static TokenCredential GetTokenCredential(IHostEnvironment environment, IConfiguration configuration)
    {
        var uamiSection = configuration.GetSection("UserAssignedManagedIdentity");
        
        // For console app (local development), prioritize Azure CLI and exclude Managed Identity
        // This prevents slow retries on authentication methods that won't work locally
        if (environment.IsDevelopment() || environment.EnvironmentName == "Production")
        {
            var tenantId = uamiSection.Get<UserAssignedManagedIdentitySettings>()?.TenantId;
            
            // For console app, use a credential chain optimized for local development
            // Try Azure CLI first (fastest for local dev), then fall back to other methods
            var options = new DefaultAzureCredentialOptions
            {
                ExcludeEnvironmentCredential = true,           // Not typically used in local dev
                ExcludeManagedIdentityCredential = true,        // Won't work in console app - causes slow retries
                ExcludeVisualStudioCredential = false,          // Can work if Visual Studio is signed in  
                ExcludeVisualStudioCodeCredential = false,      // Can work if VS Code is signed in
                ExcludeAzureCliCredential = false,              // Primary method for local dev
                ExcludeAzurePowerShellCredential = true,        // Skip PowerShell to speed up
                ExcludeAzureDeveloperCliCredential = false,     // Can work if azd is configured
                ExcludeInteractiveBrowserCredential = true,     // Don't pop up browser in console app
                ExcludeWorkloadIdentityCredential = true        // Only for Kubernetes/AKS
            };
            
            if (!string.IsNullOrEmpty(tenantId))
            {
                options.TenantId = tenantId;
            }
            
            return new DefaultAzureCredential(options);
        }

        // This path would only be used if somehow running in a non-development environment
        // (which shouldn't happen for a console app, but kept for completeness)
        var uamiSettings = uamiSection.Get<UserAssignedManagedIdentitySettings>() 
            ?? throw new InvalidOperationException("UserAssignedManagedIdentity configuration is required for non-development environments");
        
        return new ManagedIdentityCredential(uamiSettings.ClientId);
    }
}
