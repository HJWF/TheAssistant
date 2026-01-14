using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using TheAssistant.Agents.ServiceAdapter.AI;
using TheAssistant.Agents.ServiceAdapter.DailyUpdate;
using TheAssistant.Core;
using TheAssistant.Core.Agents;
using TheAssistant.Core.Infrastructure;

namespace TheAssistant.Agents.ServiceAdapter.UnitTests
{
    public class DailyUpdateAgentTests
    {
        private readonly Mock<IChatCompletionService> mockChatService;
        private readonly Mock<ILogger<DailyUpdateAgent>> mockLogger;
        private readonly DailyUpdateAgent agent;

        public DailyUpdateAgentTests()
        {
            mockChatService = new Mock<IChatCompletionService>();
            mockLogger = new Mock<ILogger<DailyUpdateAgent>>();
            agent = new DailyUpdateAgent(mockChatService.Object, mockLogger.Object);
        }

        [Fact]
        public void NameShouldReturnDailyUpdateAgent()
        {
            var name = agent.Name;
            name.Should().Be(AgentConstants.Names.DailyUpdate);
        }

        [Fact]
        public async Task GetDailyCalendarSummaryShouldReturnAgentRequestJson()
        {
            var result = await agent.GetDailyCalendarSummary();

            result.Should().NotBeNullOrEmpty();
            result.Should().Contain("agentRequest");
            result.Should().Contain("agenda-agent");
        }

        [Fact]
        public async Task GetDailyWeatherSummaryShouldReturnAgentRequestJson()
        {
            var result = await agent.GetDailyWeatherSummary();

            result.Should().NotBeNullOrEmpty();
            result.Should().Contain("agentRequest");
            result.Should().Contain("weather-agent");
        }

        [Fact]
        public async Task HandleAsyncShouldCallChatServiceWithTools()
        {
            var user = new UserDetails("+31630000000", "test@example.com", "work@example.com");
            var message = new AgentMessage(user, "user", AgentConstants.Names.DailyUpdate, "user", "Daily update", null);

            mockChatService.Setup(x => x.GetChatMessageContentAsync(
                    It.IsAny<ChatHistory>(),
                    It.IsAny<Microsoft.Extensions.AI.ChatOptions>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ChatMessage("Your daily update is ready"));

            var result = await agent.HandleAsync(message);

            result.Should().NotBeNull();
            mockChatService.Verify(x => x.GetChatMessageContentAsync(
                It.Is<ChatHistory>(h => h.Messages.Count >= 2),
                It.Is<Microsoft.Extensions.AI.ChatOptions>(o => o.Tools != null && o.Tools.Count == 2),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task HandleAsyncWhenToolsIndicatedShouldReturnAgentToAgentMessages()
        {
            var user = new UserDetails("+31630000000", "test@example.com", "work@example.com");
            var message = new AgentMessage(user, "user", AgentConstants.Names.DailyUpdate, "user", "Daily update", null);

            mockChatService.Setup(x => x.GetChatMessageContentAsync(
                    It.IsAny<ChatHistory>(),
                    It.IsAny<Microsoft.Extensions.AI.ChatOptions>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ChatMessage("agentRequest detected"));

            var result = await agent.HandleAsync(message);

            result.Should().NotBeNull();
            result.Should().HaveCountGreaterThan(1);
            
            result.Should().Contain(m => m.Receiver == AgentConstants.Names.Agenda);
            result.Should().Contain(m => m.Receiver == AgentConstants.Names.Weather);
        }

        [Fact]
        public async Task HandleAsyncWhenDirectResponseShouldReturnSingleMessage()
        {
            var user = new UserDetails("+31630000000", "test@example.com", "work@example.com");
            var message = new AgentMessage(user, "user", AgentConstants.Names.DailyUpdate, "user", "Info", null);

            mockChatService.Setup(x => x.GetChatMessageContentAsync(
                    It.IsAny<ChatHistory>(),
                    It.IsAny<Microsoft.Extensions.AI.ChatOptions>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ChatMessage("Here is your information"));

            var result = await agent.HandleAsync(message);

            result.Should().NotBeNull();
            result.Should().HaveCount(1);
            result.First().Content.Should().Be("Here is your information");
        }

        [Fact]
        public async Task HandleAsyncWhenExceptionOccursShouldReturnFallbackMessages()
        {
            var user = new UserDetails("+31630000000", "test@example.com", "work@example.com");
            var message = new AgentMessage(user, "user", AgentConstants.Names.DailyUpdate, "user", "Update", null);

            mockChatService.Setup(x => x.GetChatMessageContentAsync(
                    It.IsAny<ChatHistory>(),
                    It.IsAny<Microsoft.Extensions.AI.ChatOptions>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("LLM service error"));

            var result = await agent.HandleAsync(message);

            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            result.Should().Contain(m => m.Receiver == AgentConstants.Names.Agenda);
            result.Should().Contain(m => m.Receiver == AgentConstants.Names.Weather);
        }

        [Fact]
        public async Task HandleAsyncShouldIncludeReplyToMetadata()
        {
            var user = new UserDetails("+31630000000", "test@example.com", "work@example.com");
            var message = new AgentMessage(user, "user", AgentConstants.Names.DailyUpdate, "user", "Update", null);

            mockChatService.Setup(x => x.GetChatMessageContentAsync(
                    It.IsAny<ChatHistory>(),
                    It.IsAny<Microsoft.Extensions.AI.ChatOptions>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Force fallback"));

            var result = await agent.HandleAsync(message);

            result.Should().NotBeNull();
            result.Should().OnlyContain(m => 
                m.Metadata != null && 
                m.Metadata.ContainsKey("replyTo") && 
                m.Metadata["replyTo"] == AgentConstants.Names.DailyUpdate);
        }

        [Fact]
        public async Task HandleAsyncShouldInjectCurrentDate()
        {
            var user = new UserDetails("+31630000000", "test@example.com", "work@example.com");
            var message = new AgentMessage(user, "user", AgentConstants.Names.DailyUpdate, "user", "Update", null);
            ChatHistory? capturedHistory = null;

            mockChatService.Setup(x => x.GetChatMessageContentAsync(
                    It.IsAny<ChatHistory>(),
                    It.IsAny<Microsoft.Extensions.AI.ChatOptions>(),
                    It.IsAny<CancellationToken>()))
                .Callback<ChatHistory, Microsoft.Extensions.AI.ChatOptions, CancellationToken>((h, o, c) => capturedHistory = h)
                .ReturnsAsync(new ChatMessage("Response"));

            await agent.HandleAsync(message);

            capturedHistory.Should().NotBeNull();
            var systemMessage = capturedHistory!.Messages.First(m => m.Role == AgentConstants.ChatMessageRoles.System);
            systemMessage.Content.Should().Contain(DateTime.UtcNow.ToString("yyyy-MM-dd"));
        }
    }
}
