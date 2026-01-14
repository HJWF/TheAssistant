using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;
using TheAssistant.Agents.ServiceAdapter.AI;
using TheAssistant.Agents.ServiceAdapter.AzureCosts;
using TheAssistant.Core;
using TheAssistant.Core.Agents;
using TheAssistant.Core.AzureCosts;
using TheAssistant.Core.Infrastructure;

namespace TheAssistant.Agents.ServiceAdapter.UnitTests
{
    public class AzureCostAgentTests
    {
        private readonly Mock<IAzureCostServiceAdapter> mockCostAdapter;
        private readonly Mock<IChatCompletionService> mockChatService;
        private readonly Mock<ILogger<AzureCostAgent>> mockLogger;
        private readonly AzureCostAgent agent;

        public AzureCostAgentTests()
        {
            mockCostAdapter = new Mock<IAzureCostServiceAdapter>();
            mockChatService = new Mock<IChatCompletionService>();
            mockLogger = new Mock<ILogger<AzureCostAgent>>();
            agent = new AzureCostAgent(mockCostAdapter.Object, mockChatService.Object, mockLogger.Object);
        }

        [Fact]
        public void NameShouldReturnAzureCostAgent()
        {
            var name = agent.Name;

            name.Should().Be(AgentConstants.Names.AzureCost);
        }

        [Fact]
        public async Task GetCurrentMonthCostsShouldReturnSerializedCostData()
        {
            var expectedCosts = new AzureCostSummary(
                "2024-12-01 to 2024-12-22",
                100.50m,
                "EUR",
                new List<ServiceCost>
                {
                    new("Azure Functions", 50.25m, 50),
                    new("Storage", 50.25m, 50)
                });

            mockCostAdapter.Setup(x => x.GetCurrentMonthCosts())
                .ReturnsAsync(expectedCosts);

            var result = await agent.GetCurrentMonthCosts();

            result.Should().NotBeNullOrEmpty();
            var deserialized = JsonSerializer.Deserialize<AzureCostSummary>(result);
            deserialized.Should().NotBeNull();
            deserialized!.TotalCost.Should().Be(100.50m);
            deserialized.Currency.Should().Be("EUR");
            deserialized.TopServices.Should().HaveCount(2);
        }

        [Fact]
        public async Task GetPreviousMonthCostsShouldReturnSerializedCostData()
        {
            var expectedCosts = new AzureCostSummary(
                "2024-11-01 to 2024-11-30",
                75.00m,
                "EUR",
                new List<ServiceCost>());

            mockCostAdapter.Setup(x => x.GetPreviousMonthCosts())
                .ReturnsAsync(expectedCosts);

            var result = await agent.GetPreviousMonthCosts();

            result.Should().NotBeNullOrEmpty();
            var deserialized = JsonSerializer.Deserialize<AzureCostSummary>(result);
            deserialized.Should().NotBeNull();
            deserialized!.TotalCost.Should().Be(75.00m);
        }

        [Fact]
        public async Task GetCostsForPeriodWithValidDatesShouldReturnCostData()
        {
            var startDate = "2024-10-01";
            var endDate = "2024-12-31";
            var expectedCosts = new AzureCostSummary(
                $"{startDate} to {endDate}",
                250.00m,
                "EUR",
                new List<ServiceCost>());

            mockCostAdapter.Setup(x => x.GetCostsForPeriod(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync(expectedCosts);

            var result = await agent.GetCostsForPeriod(startDate, endDate);

            result.Should().NotBeNullOrEmpty();
            var deserialized = JsonSerializer.Deserialize<AzureCostSummary>(result);
            deserialized.Should().NotBeNull();
            deserialized!.TotalCost.Should().Be(250.00m);
        }

        [Fact]
        public async Task GetCostsForPeriodWithInvalidStartDateShouldReturnError()
        {
            var result = await agent.GetCostsForPeriod("invalid-date", "2024-12-31");

            result.Should().Contain("error");
            result.Should().Contain("Invalid start date format");
        }

        [Fact]
        public async Task GetCostsForPeriodWithInvalidEndDateShouldReturnError()
        {
            var result = await agent.GetCostsForPeriod("2024-12-01", "not-a-date");

            result.Should().Contain("error");
            result.Should().Contain("Invalid end date format");
        }

        [Fact]
        public async Task GetCostsForPeriodWithStartAfterEndShouldReturnError()
        {
            var result = await agent.GetCostsForPeriod("2024-12-31", "2024-12-01");

            result.Should().Contain("error");
            result.Should().Contain("before");
        }

        [Fact]
        public async Task GetCostsForPeriodWithOldDatesShouldReturnWarning()
        {
            var startDate = "2021-01-01";
            var endDate = "2021-12-31";

            var result = await agent.GetCostsForPeriod(startDate, endDate);

            result.Should().Contain("warning");
            result.Should().Contain("2 years ago");
            var deserialized = JsonSerializer.Deserialize<Dictionary<string, object>>(result);
            deserialized.Should().ContainKey("warning");
        }

        [Fact]
        public async Task HandleAsyncShouldCallChatServiceWithTools()
        {
            var user = new UserDetails("+31630000000", "test@example.com", "work@example.com");
            var message = new AgentMessage(user, "user", AgentConstants.Names.AzureCost, "user", "What are my costs?", null);

            mockChatService.Setup(x => x.GetChatMessageContentAsync(
                    It.IsAny<ChatHistory>(),
                    It.IsAny<Microsoft.Extensions.AI.ChatOptions>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ChatMessage("Your costs are €100"));

            var result = await agent.HandleAsync(message);

            result.Should().NotBeNull();
            result.Should().HaveCount(1);
            result.First().Content.Should().Be("Your costs are €100");
            
            mockChatService.Verify(x => x.GetChatMessageContentAsync(
                It.Is<ChatHistory>(h => h.Messages.Count >= 2),
                It.Is<Microsoft.Extensions.AI.ChatOptions>(o => o.Tools != null && o.Tools.Count == 3),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task HandleAsyncWithNullResponseShouldReturnSorryMessage()
        {
            var user = new UserDetails("+31630000000", "test@example.com", "work@example.com");
            var message = new AgentMessage(user, "user", AgentConstants.Names.AzureCost, "user", "What are my costs?", null);

            mockChatService.Setup(x => x.GetChatMessageContentAsync(
                    It.IsAny<ChatHistory>(),
                    It.IsAny<Microsoft.Extensions.AI.ChatOptions>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ChatMessage((string?)null));

            var result = await agent.HandleAsync(message);

            result.Should().NotBeNull();
            result.Should().HaveCount(1);
            result.First().Content.Should().Be(AgentConstants.SorryMessage);
        }

        [Fact]
        public async Task HandleAsyncShouldInjectCurrentDate()
        {
            var user = new UserDetails("+31630000000", "test@example.com", "work@example.com");
            var message = new AgentMessage(user, "user", AgentConstants.Names.AzureCost, "user", "Current costs", null);
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
            systemMessage.Content.Should().Contain(DateTime.UtcNow.Year.ToString());
        }
    }
}
