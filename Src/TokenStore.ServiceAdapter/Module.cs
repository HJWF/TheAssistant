using Azure.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TheAssistant.Core;

namespace TheAssistant.TokenStore.ServiceAdapter
{
    public static class Module
    {
        public static IServiceCollection AddTokenStoreServices(this IServiceCollection services, Action<TokenStoreSettings> SignalSettings, TokenCredential credential)
        {
            services.AddOptions<TokenStoreSettings>().Configure(SignalSettings).ValidateDataAnnotations();

            services.AddTransient<ITokenStoreServiceAdapter>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<TokenStoreServiceAdapter>>();
                var settings = sp.GetRequiredService<IOptions<TokenStoreSettings>>().Value;
                return new TokenStoreServiceAdapter(new(new Uri(settings.VaultUrl), credential), logger);
            });

            return services;
        }
    }
}
