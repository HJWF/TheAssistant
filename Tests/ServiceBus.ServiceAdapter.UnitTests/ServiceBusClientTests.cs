using FluentAssertions;

namespace TheAssistant.ServiceBus.ServiceAdapter.UnitTests
{
    public class ServiceBusClientTests
    {
        [Fact]
        public void ServiceBusClientShouldImplementIServiceBusClient()
        {
            var client = new ServiceBusClient(new Azure.Identity.DefaultAzureCredential(), "namespace");
            client.Should().NotBeNull();
        }
    }
}
