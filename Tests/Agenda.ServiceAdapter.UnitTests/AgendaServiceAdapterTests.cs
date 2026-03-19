using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Newtonsoft.Json;
using System.Net;
using TheAssistant.Agenda.ServiceAdapter.Calendar.Models;
using TheAssistant.Core.Authentication;

namespace TheAssistant.Agenda.ServiceAdapter.UnitTests
{
    public class AgendaServiceAdapterTests
    {
        private readonly Mock<ILogger<AgendaServiceAdapter>> _loggerMock;

        public AgendaServiceAdapterTests()
        {
            _loggerMock = new Mock<ILogger<AgendaServiceAdapter>>();
        }

        [Fact]
        public async Task GetTodayEventsShouldThrowWhenTokenIsNull()
        {
            var adapter = CreateAdapterWithHandler(HttpStatusCode.OK, "{}");

            var act = async () => await adapter.GetTodayEvents("user@test.com", null!);

            await act.Should().ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("*Invalid or expired token*");
        }

        [Fact]
        public async Task GetTodayEventsShouldThrowWhenAccessTokenIsEmpty()
        {
            var token = new Token(string.Empty, "refresh", DateTime.UtcNow.AddHours(1));
            var adapter = CreateAdapterWithHandler(HttpStatusCode.OK, "{}");

            var act = async () => await adapter.GetTodayEvents("user@test.com", token);

            await act.Should().ThrowAsync<UnauthorizedAccessException>();
        }

        [Fact]
        public async Task GetTodayEventsShouldThrowWhenTokenIsExpired()
        {
            var token = new Token("access", "refresh", DateTime.Now.AddHours(-1));
            var adapter = CreateAdapterWithHandler(HttpStatusCode.OK, "{}");

            var act = async () => await adapter.GetTodayEvents("user@test.com", token);

            await act.Should().ThrowAsync<UnauthorizedAccessException>();
        }

        [Fact]
        public async Task GetTodayEventsShouldThrowWhenRefreshTokenIsEmpty()
        {
            var token = new Token("access", string.Empty, DateTime.UtcNow.AddHours(1));
            var adapter = CreateAdapterWithHandler(HttpStatusCode.OK, "{}");

            var act = async () => await adapter.GetTodayEvents("user@test.com", token);

            await act.Should().ThrowAsync<UnauthorizedAccessException>();
        }

        [Fact]
        public async Task GetTodayEventsShouldReturnEventsForValidToken()
        {
            var wrapper = new CalendarEventResponseWrapper
            {
                value = new[]
                {
                    new CalendarEventResponse
                    {
                        subject = "Team sync",
                        start = new Start { dateTime = DateTime.UtcNow },
                        end = new End { dateTime = DateTime.UtcNow.AddHours(1) },
                        location = new Location { displayName = "Teams" },
                        organizer = new Organizer { emailAddress = new Emailaddress { name = "boss" } },
                        isAllDay = false
                    }
                }
            };

            var token = new Token("valid-access", "refresh", DateTime.Now.AddHours(1));
            var adapter = CreateAdapterWithHandler(HttpStatusCode.OK, JsonConvert.SerializeObject(wrapper));

            var result = await adapter.GetTodayEvents("user@test.com", token);

            result.Should().NotBeNull();
            result.Should().HaveCount(1);
        }

        private AgendaServiceAdapter CreateAdapterWithHandler(HttpStatusCode statusCode, string responseBody)
        {
            var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage(statusCode)
                {
                    Content = new StringContent(responseBody)
                });

            var httpClient = new HttpClient(handlerMock.Object);
            return new AgendaServiceAdapter(httpClient, _loggerMock.Object);
        }
    }
}
