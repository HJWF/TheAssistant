using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using TheAssistant.Core;

namespace TheAssistant.Messaging.ServiceAdapter.UnitTests
{
    public class ModuleTests
    {
        [Fact]
        public void AddMessagingServicesShouldRegisterRequiredServices()
        {
            var services = new ServiceCollection();

            services.AddMessagingServices(s => { s.BaseUrl = "http://localhost"; s.PhoneNumber = "+100"; });

            services.Any(sd => sd.ServiceType == typeof(ISignalApiClient)).Should().BeTrue();
            services.Any(sd => sd.ServiceType == typeof(IMessageServiceAdapter)).Should().BeTrue();
        }
    }
}
