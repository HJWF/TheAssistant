using FluentAssertions;
using Moq;
using Moq.Protected;
using Newtonsoft.Json;
using System.Net;
using TheAssistant.Agenda.ServiceAdapter.Calendar;
using TheAssistant.Agenda.ServiceAdapter.Calendar.Models;

namespace TheAssistant.Agenda.ServiceAdapter.UnitTests;

public class GraphCalendarClientTests
{
    [Fact]
    public async Task GetTodayEventsAsyncShouldReturnEventsWhenApiSucceeds()
    {
        var wrapper = new CalendarEventResponseWrapper
        {
            value = new[]
            {
                new CalendarEventResponse { subject = "s1", start = new Start { dateTime = DateTime.UtcNow }, end = new End { dateTime = DateTime.UtcNow.AddHours(1) }, location = new Location { displayName = "loc" }, organizer = new Organizer { emailAddress = new Emailaddress { name = "organizer" } }, isAllDay = false }
            }
        };

        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        handlerMock
           .Protected()
           .Setup<Task<HttpResponseMessage>>("SendAsync",
               ItExpr.IsAny<HttpRequestMessage>(),
               ItExpr.IsAny<CancellationToken>())
           .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
           {
               Content = new StringContent(JsonConvert.SerializeObject(wrapper))
           })
           .Verifiable();

        var httpClient = new HttpClient(handlerMock.Object);
        var client = new GraphCalendarClient(httpClient);

        var events = await client.GetTodayEventsAsync("token");

        events.Should().NotBeNull();
        events.Should().NotBeEmpty();

        handlerMock.Protected().Verify("SendAsync", Times.Once(), ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task GetTodayEventsAsyncShouldThrowWhenApiFails()
    {
        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        handlerMock
           .Protected()
           .Setup<Task<HttpResponseMessage>>("SendAsync",
               ItExpr.IsAny<HttpRequestMessage>(),
               ItExpr.IsAny<CancellationToken>())
           .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.BadRequest)
           {
               Content = new StringContent("error")
           })
           .Verifiable();

        var httpClient = new HttpClient(handlerMock.Object);
        var client = new GraphCalendarClient(httpClient);

        await client.Invoking(c => c.GetTodayEventsAsync("token")).Should().ThrowAsync<Exception>();

        handlerMock.Protected().Verify("SendAsync", Times.Once(), ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
    }
}
