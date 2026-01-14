using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;
using TheAssistant.Agents.ServiceAdapter.Agenda;
using TheAssistant.Agents.ServiceAdapter.Agenda.Events;
using TheAssistant.Agents.ServiceAdapter.Authentication;
using TheAssistant.Agents.ServiceAdapter.AI;
using TheAssistant.Core;
using TheAssistant.Core.Agenda;
using TheAssistant.Core.Agents;
using TheAssistant.Core.Authentication;
using TheAssistant.Core.Infrastructure;

namespace TheAssistant.Agents.ServiceAdapter.UnitTests
{
    public class AgendaAgentTests
    {
        private readonly Mock<IChatCompletionService> mockChatService;
        private readonly Mock<ITokenStoreServiceAdapter> mockTokenStore;
        private readonly Mock<ILoginUrlProvider> mockLoginUrlProvider;
        private readonly Mock<ILogger<AgendaAgent>> mockLogger;
        private readonly Mock<IEventService> mockEventService;

        public AgendaAgentTests()
        {
            mockChatService = new Mock<IChatCompletionService>();
            mockTokenStore = new Mock<ITokenStoreServiceAdapter>();
            mockLoginUrlProvider = new Mock<ILoginUrlProvider>();
            mockLogger = new Mock<ILogger<AgendaAgent>>();
            mockEventService = new Mock<IEventService>();
        }

        [Fact]
        public void NameShouldReturnAgendaAgent()
        {
            var agent = CreateAgent();
            var name = agent.Name;
            name.Should().Be(AgentConstants.Names.Agenda);
        }

        [Fact]
        public async Task GetEventsForDateWithInvalidDateShouldReturnError()
        {
            var agent = CreateAgent();
            var user = CreateTestUser();
            var message = new AgentMessage(user, "user", AgentConstants.Names.Agenda, "user", "Events", null);
            
            await agent.HandleAsync(message);
            
            var result = await agent.GetEventsForDate("not-a-date");

            result.Should().Contain("error");
            result.Should().Contain("Invalid date format");
        }

        [Fact]
        public async Task GetEventsForDateRangeWithStartAfterEndShouldReturnError()
        {
            var agent = CreateAgent();
            var user = CreateTestUser();
            var message = new AgentMessage(user, "user", AgentConstants.Names.Agenda, "user", "Events", null);
            
            await agent.HandleAsync(message);

            var result = await agent.GetEventsForDateRange("2024-12-31", "2024-12-01");

            result.Should().Contain("error");
            result.Should().Contain("before");
        }

        [Fact]
        public async Task HandleAsyncWithNullUserShouldReturnError()
        {
            var agent = CreateAgent();
            var message = new AgentMessage(null!, "user", AgentConstants.Names.Agenda, "user", "Events", null);

            var result = await agent.HandleAsync(message);

            result.Should().NotBeNull();
            result.Should().HaveCount(1);
            result.First().Content.Should().Contain("User details are missing");
        }

        [Fact]
        public async Task HandleAsyncShouldCallChatServiceWithTools()
        {
            var agent = CreateAgent();
            var user = CreateTestUser();
            var token = CreateValidToken();
            
            SetupValidToken(token);
            
            var message = new AgentMessage(user, "user", AgentConstants.Names.Agenda, "user", "What's on my calendar?", null);

            mockChatService.Setup(x => x.GetChatMessageContentAsync(
                    It.IsAny<ChatHistory>(),
                    It.IsAny<Microsoft.Extensions.AI.ChatOptions>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ChatMessage("Your calendar has 2 meetings"));

            var result = await agent.HandleAsync(message);

            result.Should().NotBeNull();
            result.Should().HaveCount(1);
            result.First().Content.Should().Be("Your calendar has 2 meetings");
            
            mockChatService.Verify(x => x.GetChatMessageContentAsync(
                It.Is<ChatHistory>(h => h.Messages.Count >= 2),
                It.Is<Microsoft.Extensions.AI.ChatOptions>(o => o.Tools != null && o.Tools.Count == 4),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task HandleAsyncShouldInjectCurrentDate()
        {
            var agent = CreateAgent();
            var user = CreateTestUser();
            var token = CreateValidToken();
            ChatHistory? capturedHistory = null;

            SetupValidToken(token);

            mockChatService.Setup(x => x.GetChatMessageContentAsync(
                    It.IsAny<ChatHistory>(),
                    It.IsAny<Microsoft.Extensions.AI.ChatOptions>(),
                    It.IsAny<CancellationToken>()))
                .Callback<ChatHistory, Microsoft.Extensions.AI.ChatOptions, CancellationToken>((h, o, c) => capturedHistory = h)
                .ReturnsAsync(new ChatMessage("Response"));

            var message = new AgentMessage(user, "user", AgentConstants.Names.Agenda, "user", "Events", null);

            await agent.HandleAsync(message);

            capturedHistory.Should().NotBeNull();
            var systemMessage = capturedHistory!.Messages.First(m => m.Role == AgentConstants.ChatMessageRoles.System);
            systemMessage.Content.Should().Contain(DateTime.UtcNow.Year.ToString());
        }

        [Fact]
        public async Task HandleAsyncWithExpiredTokenShouldCallLLMWhichWillGetErrorFromTool()
        {
            var agent = CreateAgent();
            var user = CreateTestUser();
            var expiredToken = new Token("expired", "refresh", DateTime.Now.AddHours(-1));
            var loginUrl = "https://login.example.com/auth";

            mockTokenStore.Setup(x => x.GetToken(user.PersonalMailTag, "microsoftconsumer"))
                .ReturnsAsync(expiredToken);
            mockLoginUrlProvider.Setup(x => x.GetLoginUrlForUser(user.PersonalMailTag))
                .Returns(loginUrl);

            var message = new AgentMessage(user, "user", AgentConstants.Names.Agenda, "user", "Today's events", null);

            mockChatService.Setup(x => x.GetChatMessageContentAsync(
                    It.IsAny<ChatHistory>(),
                    It.IsAny<Microsoft.Extensions.AI.ChatOptions>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ChatMessage("Please log in to access your calendar"));

            var result = await agent.HandleAsync(message);

            result.Should().NotBeNull();
            result.Should().HaveCount(1);
            mockChatService.Verify(x => x.GetChatMessageContentAsync(
                It.IsAny<ChatHistory>(),
                It.IsAny<Microsoft.Extensions.AI.ChatOptions>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task HandleAsyncWhenChatServiceThrowsShouldReturnErrorMessage()
        {
            var agent = CreateAgent();
            var user = CreateTestUser();
            var token = CreateValidToken();

            SetupValidToken(token);

            mockChatService.Setup(x => x.GetChatMessageContentAsync(
                    It.IsAny<ChatHistory>(),
                    It.IsAny<Microsoft.Extensions.AI.ChatOptions>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("LLM service error"));

            var message = new AgentMessage(user, "user", AgentConstants.Names.Agenda, "user", "Events", null);

            var result = await agent.HandleAsync(message);

            result.Should().NotBeNull();
            result.Should().HaveCount(1);
            result.First().Content.Should().Contain("error while fetching your calendar");
        }

        [Fact]
        public async Task HandleAsyncWithValidTokenAndEventsShouldReturnFormattedResponse()
        {
            var agent = CreateAgent();
            var user = CreateTestUser();
            var token = CreateValidToken();
            var events = CreateTestEvents();

            SetupValidToken(token);
            mockEventService.Setup(x => x.GetTodaysEvents(It.IsAny<string>(), token))
                .ReturnsAsync(JsonSerializer.Serialize(events));

            mockChatService.Setup(x => x.GetChatMessageContentAsync(
                    It.IsAny<ChatHistory>(),
                    It.IsAny<Microsoft.Extensions.AI.ChatOptions>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ChatMessage("You have 2 meetings today: Team Meeting at 10:00 and Lunch at 12:00"));

            var message = new AgentMessage(user, "user", AgentConstants.Names.Agenda, "user", "What's on my calendar today?", null);

            var result = await agent.HandleAsync(message);

            result.Should().NotBeNull();
            result.Should().HaveCount(1);
            result.First().Content.Should().Contain("meetings");
        }

        private AgendaAgent CreateAgent() => new AgendaAgent(
                mockChatService.Object,
                mockTokenStore.Object,
                mockLoginUrlProvider.Object,
                mockLogger.Object,
                mockEventService.Object);

        private UserDetails CreateTestUser() => new UserDetails("+31630454969", "test@example.com", "work@example.com");

        private Token CreateValidToken() => new Token("access-token", "refresh-token", DateTime.Now.AddHours(1));

        private void SetupValidToken(Token token) => mockTokenStore.Setup(x => x.GetToken(It.IsAny<string>(), "microsoftconsumer"))
                .ReturnsAsync(token);

        private List<CalendarEvent> CreateTestEvents() => new List<CalendarEvent>
            {
                new("Team Meeting", DateTime.Today.AddHours(10), DateTime.Today.AddHours(11),
                    "Office", "organizer@example.com", false),
                new("Lunch", DateTime.Today.AddHours(12), DateTime.Today.AddHours(13),
                    "Cafeteria", "organizer@example.com", false)
            };
    }
}
