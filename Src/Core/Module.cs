using Microsoft.Extensions.DependencyInjection;
using TheAssistant.Core.Infrastructure;
using TheAssistant.Core.Messaging.HandleDailyOverview;
using TheAssistant.Core.Messaging.HandleNewSignIn;
using TheAssistant.Core.Messaging.HandleQueuedMessage;
using TheAssistant.Core.Messaging.HandleReceiveMessages;

namespace TheAssistant.Core
{
    public static class Module
    {
        public static IServiceCollection AddCoreServices(this IServiceCollection services, Action<LoginSettings> settings)
        {
            services.AddOptions<LoginSettings>().Configure(settings).ValidateDataAnnotations();

            services.AddTransient<ICommandHandler<HandleReceiveMessagesCommand>, HandleReceiveMessagesCommandHandler>();
            services.AddTransient<ICommandHandler<HandleQueuedMessageCommand>, HandleQueuedMessageCommandHandler>();
            services.AddTransient<ICommandHandler<HandleDailyOverviewCommand>, HandleDailyOverviewCommandHandler>();
            services.AddTransient<ICommandHandler<HandleNewPersonalSignInCommand>, HandleNewPersonalSignInCommandHandler>();

            return services;
        }
    }
}
