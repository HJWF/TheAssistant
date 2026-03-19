using FluentAssertions;
using Moq;

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

    public class ServiceBusServiceAdapterTests
    {
        private readonly Mock<IServiceBusClient> _clientMock;
        private readonly ServiceBusServiceAdapter _adapter;

        public ServiceBusServiceAdapterTests()
        {
            _clientMock = new Mock<IServiceBusClient>(MockBehavior.Strict);
            _adapter = new ServiceBusServiceAdapter(_clientMock.Object);
        }

        [Fact]
        public async Task SendMessageAsyncShouldDelegateToClient()
        {
            _clientMock.Setup(x => x.SendMessageAsync("my-queue", "my-message"))
                .Returns(Task.CompletedTask)
                .Verifiable();

            await _adapter.SendMessageAsync("my-queue", "my-message");

            _clientMock.Verify(x => x.SendMessageAsync("my-queue", "my-message"), Times.Once);
        }

        [Fact]
        public async Task SendMessageAsyncShouldPassQueueNameAndBodyToClient()
        {
            string? capturedQueue = null;
            string? capturedBody = null;

            _clientMock.Setup(x => x.SendMessageAsync(It.IsAny<string>(), It.IsAny<string>()))
                .Callback<string, string>((q, b) => { capturedQueue = q; capturedBody = b; })
                .Returns(Task.CompletedTask);

            await _adapter.SendMessageAsync("receivedmessages", "hello world");

            capturedQueue.Should().Be("receivedmessages");
            capturedBody.Should().Be("hello world");
        }
    }
}

