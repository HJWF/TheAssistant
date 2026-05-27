using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using TheAssistant.Agents.ServiceAdapter.Agenda.Events;
using TheAssistant.Core;
using TheAssistant.Core.Agenda;
using TheAssistant.Core.Authentication;

namespace TheAssistant.Agents.ServiceAdapter.UnitTests.Agenda;

public class EventServiceTests
{
    private readonly Mock<IAgendaServiceAdapter> _agendaMock;
    private readonly IMemoryCache _cache;
    private readonly Token _token;

    public EventServiceTests()
    {
        _agendaMock = new Mock<IAgendaServiceAdapter>(MockBehavior.Strict);
        _cache = new MemoryCache(new MemoryCacheOptions());
        _token = new Token("access", "refresh", DateTime.UtcNow.AddHours(1));
    }

    private static CalendarEvent CreateEvent(string subject) =>
        new(subject, DateTime.UtcNow, DateTime.UtcNow.AddHours(1), "Teams", "organizer", false);

    [Fact]
    public async Task GetTodaysEventsShouldCallAgendaAdapterAndReturnSerializedEvents()
    {
        var events = new[] { CreateEvent("Standup") };

        _agendaMock.Setup(x => x.GetTodayEvents("user@test.com", _token))
            .ReturnsAsync(events);

        var service = new EventService(_cache, _agendaMock.Object);

        var result = await service.GetTodaysEvents("user@test.com", _token);

        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("Standup");
    }

    [Fact]
    public async Task GetTodaysEventsSecondCallShouldReturnCachedResultWithoutCallingAdapter()
    {
        var events = new[] { CreateEvent("Standup") };

        _agendaMock.Setup(x => x.GetTodayEvents("user@test.com", _token))
            .ReturnsAsync(events);

        var service = new EventService(_cache, _agendaMock.Object);

        await service.GetTodaysEvents("user@test.com", _token);
        await service.GetTodaysEvents("user@test.com", _token);

        _agendaMock.Verify(x => x.GetTodayEvents(It.IsAny<string>(), It.IsAny<Token>()), Times.Once);
    }

    [Fact]
    public async Task GetTodaysEventsShouldReturnSameDataOnCacheHit()
    {
        var events = new[] { CreateEvent("Cached Meeting") };

        _agendaMock.Setup(x => x.GetTodayEvents(It.IsAny<string>(), It.IsAny<Token>()))
            .ReturnsAsync(events);

        var service = new EventService(_cache, _agendaMock.Object);

        var first = await service.GetTodaysEvents("user@test.com", _token);
        var second = await service.GetTodaysEvents("user@test.com", _token);

        first.Should().Be(second);
    }

    [Fact]
    public async Task GetMeetingsShouldDelegateToGetTodaysEvents()
    {
        var events = new[] { CreateEvent("Meeting") };

        _agendaMock.Setup(x => x.GetTodayEvents(It.IsAny<string>(), _token))
            .ReturnsAsync(events);

        var service = new EventService(_cache, _agendaMock.Object);

        var result = await service.GetMeetings("2024-12-01", _token);

        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("Meeting");
    }

    [Fact]
    public async Task GetBirthdaysShouldDelegateToGetTodaysEvents()
    {
        var events = new[] { CreateEvent("Birthday") };

        _agendaMock.Setup(x => x.GetTodayEvents(It.IsAny<string>(), _token))
            .ReturnsAsync(events);

        var service = new EventService(_cache, _agendaMock.Object);

        var result = await service.GetBirthdays("2024-12-01", _token);

        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("Birthday");
    }
}
