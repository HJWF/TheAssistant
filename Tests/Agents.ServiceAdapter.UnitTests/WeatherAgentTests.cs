using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;
using TheAssistant.Agents.ServiceAdapter.AI;
using TheAssistant.Agents.ServiceAdapter.Weather;
using TheAssistant.Core;
using TheAssistant.Core.Agents;
using TheAssistant.Core.Infrastructure;
using TheAssistant.Core.Weather;

namespace TheAssistant.Agents.ServiceAdapter.UnitTests
{
    public class WeatherAgentTests
    {
        private readonly Mock<IWeatherServiceAdapter> mockWeatherAdapter;
        private readonly Mock<IChatCompletionService> mockChatService;
        private readonly Mock<ILogger<WeatherAgent>> mockLogger;
        private readonly WeatherAgent agent;

        public WeatherAgentTests()
        {
            mockWeatherAdapter = new Mock<IWeatherServiceAdapter>();
            mockChatService = new Mock<IChatCompletionService>();
            mockLogger = new Mock<ILogger<WeatherAgent>>();
            agent = new WeatherAgent(mockWeatherAdapter.Object, mockChatService.Object, mockLogger.Object);
        }

        [Fact]
        public void NameShouldReturnWeatherAgent()
        {
            var name = agent.Name;
            name.Should().Be(AgentConstants.Names.Weather);
        }

        [Fact]
        public async Task GetWeatherForDefaultLocationShouldReturnWeatherData()
        {
            var expectedWeather = CreateTestWeatherForecast();
            
            mockWeatherAdapter.Setup(x => x.GetWeather("52.2112", "5.9699"))
                .ReturnsAsync(expectedWeather);

            var result = await agent.GetWeatherForDefaultLocation();

            result.Should().NotBeNullOrEmpty();
            var deserialized = JsonSerializer.Deserialize<WeatherForecast>(result);
            deserialized.Should().NotBeNull();
        }

        [Theory]
        [InlineData("Amsterdam", "52.3676", "4.9041")]
        [InlineData("Utrecht", "52.0907", "5.1214")]
        [InlineData("Rotterdam", "51.9225", "4.4792")]
        [InlineData("Den Haag", "52.0705", "4.3007")]
        [InlineData("Eindhoven", "51.4416", "5.4697")]
        public async Task GetWeatherForLocationWithSupportedCityShouldReturnWeatherData(
            string city, string expectedLat, string expectedLon)
        {
            var expectedWeather = CreateTestWeatherForecast();
            
            mockWeatherAdapter.Setup(x => x.GetWeather(expectedLat, expectedLon))
                .ReturnsAsync(expectedWeather);

            var result = await agent.GetWeatherForLocation(city);

            result.Should().NotBeNullOrEmpty();
            result.Should().Contain(city);
            
            mockWeatherAdapter.Verify(x => x.GetWeather(expectedLat, expectedLon), Times.Once);
        }

        [Theory]
        [InlineData("amsterdam")]
        [InlineData("UTRECHT")]
        [InlineData("Den Haag")]
        [InlineData("denhaag")]
        public async Task GetWeatherForLocationShouldBeCaseInsensitive(string city)
        {
            var expectedWeather = CreateTestWeatherForecast();
            
            mockWeatherAdapter.Setup(x => x.GetWeather(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(expectedWeather);

            var result = await agent.GetWeatherForLocation(city);

            result.Should().NotBeNullOrEmpty();
            result.Should().NotContain("error");
        }

        [Fact]
        public async Task GetWeatherForLocationWithUnsupportedCityShouldReturnError()
        {
            var result = await agent.GetWeatherForLocation("UnknownCity");

            result.Should().Contain("error");
            result.Should().Contain("not found");
            result.Should().Contain("supportedCities");
            
            var deserialized = JsonSerializer.Deserialize<Dictionary<string, object>>(result);
            deserialized.Should().ContainKey("error");
        }

        [Fact]
        public async Task GetWeatherForLocationWhenServiceThrowsShouldReturnError()
        {
            mockWeatherAdapter.Setup(x => x.GetWeather(It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new HttpRequestException("Weather service unavailable"));

            var result = await agent.GetWeatherForLocation("Amsterdam");

            result.Should().Contain("error");
            result.Should().Contain("Failed to fetch weather");
        }

        [Fact]
        public async Task HandleAsyncShouldCallChatServiceWithTools()
        {
            var user = new UserDetails("+31630454969", "test@example.com", "work@example.com");
            var message = new AgentMessage(user, "user", AgentConstants.Names.Weather, "user", "What's the weather?", null);

            mockChatService.Setup(x => x.GetChatMessageContentAsync(
                    It.IsAny<ChatHistory>(),
                    It.IsAny<Microsoft.Extensions.AI.ChatOptions>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ChatMessage("It's sunny with 20°C"));

            var result = await agent.HandleAsync(message);

            result.Should().NotBeNull();
            result.Should().HaveCount(1);
            result.First().Content.Should().Be("It's sunny with 20°C");
            
            mockChatService.Verify(x => x.GetChatMessageContentAsync(
                It.Is<ChatHistory>(h => h.Messages.Count >= 2),
                It.Is<Microsoft.Extensions.AI.ChatOptions>(o => o.Tools != null && o.Tools.Count == 2),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task HandleAsyncWhenChatServiceThrowsShouldReturnErrorMessage()
        {
            var user = new UserDetails("+31630454969", "test@example.com", "work@example.com");
            var message = new AgentMessage(user, "user", AgentConstants.Names.Weather, "user", "Weather", null);

            mockChatService.Setup(x => x.GetChatMessageContentAsync(
                    It.IsAny<ChatHistory>(),
                    It.IsAny<Microsoft.Extensions.AI.ChatOptions>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("LLM service error"));

            var result = await agent.HandleAsync(message);

            result.Should().NotBeNull();
            result.Should().HaveCount(1);
            result.First().Content.Should().Contain("couldn't fetch the weather forecast");
        }

        [Fact]
        public async Task HandleAsyncShouldInjectCurrentDate()
        {
            var user = new UserDetails("+31630454969", "test@example.com", "work@example.com");
            var message = new AgentMessage(user, "user", AgentConstants.Names.Weather, "user", "Weather", null);
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

        private WeatherForecast CreateTestWeatherForecast() => new WeatherForecast();
    }
}
