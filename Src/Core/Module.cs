using Microsoft.Extensions.DependencyInjection;
using TheAssistant.Core.Infrastructure;
using TheAssistant.Core.Messaging.HandleQueuedMessage;
using TheAssistant.Core.Messaging.HandleReceiveMessages;

namespace TheAssistant.Core
{
    public static class Module
    {
        public static IServiceCollection AddCoreServices(this IServiceCollection services)
        {
            services.AddTransient<ICommandHandler<HandleReceiveMessagesCommand>, HandleReceiveMessagesCommandHandler>();
            services.AddTransient<ICommandHandler<HandleQueuedMessageCommand>, HandleQueuedMessageCommandHandler>();

            return services;
        }
    }
}
