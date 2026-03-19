using FluentAssertions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using Moq;
using TheAssistant.Agents.ServiceAdapter.Formatting;

namespace TheAssistant.Agents.ServiceAdapter.UnitTests.Formatting
{
    public class FormattingAgentTests
    {
        private readonly Mock<IChatClient> _chatClientMock;
        private readonly Mock<ITokenUsageTracker> _trackerMock;
        private readonly FormattingAgent _agent;

        public FormattingAgentTests()
        {
            _chatClientMock = new Mock<IChatClient>(MockBehavior.Strict);
            _trackerMock = new Mock<ITokenUsageTracker>();
            var settings = Options.Create(new AgentsSettings());
            _agent = new FormattingAgent(_chatClientMock.Object, _trackerMock.Object, settings);
        }

        [Fact]
        public async Task HandleAsyncShouldReturnSorryMessageForNullInput()
        {
            var result = await _agent.HandleAsync(null!);

            result.Should().Be(AgentConstants.SorryMessage);
        }

        [Fact]
        public async Task HandleAsyncShouldReturnSorryMessageForEmptyList()
        {
            var result = await _agent.HandleAsync(new List<AgentResponse>());

            result.Should().Be(AgentConstants.SorryMessage);
        }

        [Fact]
        public async Task HandleAsyncShouldReturnSingleResponseContentWithoutCallingLlm()
        {
            var responses = new List<AgentResponse>
            {
                new("weather-agent", "It is sunny today.")
            };

            var result = await _agent.HandleAsync(responses);

            result.Should().Be("It is sunny today.");
            _chatClientMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task HandleAsyncShouldCallLlmWhenMultipleResponsesProvided()
        {
            var responses = new List<AgentResponse>
            {
                new("weather-agent", "It is sunny."),
                new("agenda-agent", "You have 2 meetings.")
            };

            _chatClientMock.Setup(x => x.GetResponseAsync(
                    It.IsAny<IEnumerable<ChatMessage>>(),
                    It.IsAny<ChatOptions?>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ChatResponse([new ChatMessage(ChatRole.Assistant, "Combined response")]));

            var result = await _agent.HandleAsync(responses);

            result.Should().Be("Combined response");
            _chatClientMock.Verify(x => x.GetResponseAsync(
                It.IsAny<IEnumerable<ChatMessage>>(),
                It.IsAny<ChatOptions?>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task HandleAsyncShouldReturnSorryMessageWhenLlmReturnsEmptyContent()
        {
            var responses = new List<AgentResponse>
            {
                new("weather-agent", "Sunny."),
                new("agenda-agent", "Two meetings.")
            };

            _chatClientMock.Setup(x => x.GetResponseAsync(
                    It.IsAny<IEnumerable<ChatMessage>>(),
                    It.IsAny<ChatOptions?>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ChatResponse([new ChatMessage(ChatRole.Assistant, new List<AIContent>())]));

            var result = await _agent.HandleAsync(responses);

            result.Should().Be(AgentConstants.SorryMessage);
        }

        [Fact]
        public void NameShouldReturnFormattingAgentName()
        {
            FormattingAgent.Name.Should().Be(AgentConstants.Names.Formatting);
        }
    }
}
