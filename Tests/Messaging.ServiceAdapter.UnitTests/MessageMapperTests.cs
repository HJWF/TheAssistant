using FluentAssertions;

namespace TheAssistant.Messaging.ServiceAdapter.UnitTests
{
    public class MessageMapperTests
    {
        [Fact]
        public void ToModelShouldMapAllProperties()
        {
            var source = new TheAssistant.Messaging.ServiceAdapter.Models.SentMessage
            {
                Destination = "dest",
                DestinationNumber = "+100",
                DestinationUuid = "uuid",
                Timestamp = 12345,
                Message = "hello",
                ExpiresInSeconds = 60,
                ViewOnce = true
            };

            var result = source.ToModel();

            result.Destination.Should().Be(source.Destination);
            result.DestinationNumber.Should().Be(source.DestinationNumber);
            result.DestinationUuid.Should().Be(source.DestinationUuid);
            result.Timestamp.Should().Be(source.Timestamp);
            result.Message.Should().Be(source.Message);
            result.ExpiresInSeconds.Should().Be(source.ExpiresInSeconds);
            result.ViewOnce.Should().BeTrue();
        }

        [Fact]
        public void ToModelShouldIgnoreNullItems()
        {
            var list = new List<TheAssistant.Messaging.ServiceAdapter.Models.SentMessage?>
            {
                new TheAssistant.Messaging.ServiceAdapter.Models.SentMessage { Destination = "a" },
                null,
                new TheAssistant.Messaging.ServiceAdapter.Models.SentMessage { Destination = "b" }
            };

            var result = list.ToModel();

            result.Should().HaveCount(2);
            result.Should().OnlyContain(m => m != null && (m.Destination == "a" || m.Destination == "b"));
        }
    }
}
