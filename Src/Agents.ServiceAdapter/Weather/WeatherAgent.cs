using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Text.Json;
using TheAssistant.Core;

namespace TheAssistant.Agents.ServiceAdapter.Weather
{
    public class WeatherAgent : IWeatherAgent
    {
        private const string Prompt = """
                You are a helpful assistant summarizing the weather.
                Use the following weather information to create a short daily overview in Dutch.
            """;
        private readonly IWeatherServiceAdapter _weatherServiceAdapter;
        private readonly Kernel _kernel;

        public WeatherAgent(IWeatherServiceAdapter weatherServiceAdapter, Kernel kernel)
        {
            _weatherServiceAdapter = weatherServiceAdapter;
            _kernel = kernel;
        }

        public string Name => "Weather";

        [KernelFunction]
        public async Task<string> HandleAsync(string input)
        {
            var appointments = await _weatherServiceAdapter.GetWeather(DateTime.Today);

            var chat = _kernel.GetRequiredService<IChatCompletionService>();
            var history = new ChatHistory();
            history.AddSystemMessage(Prompt);
            history.AddUserMessage(JsonSerializer.Serialize(appointments));

            var reply = await chat.GetChatMessageContentAsync(history);
            return reply.Content ?? AgentConstants.SorryMessage;
        }

        public int Score(string message)
        {
            throw new NotImplementedException();
        }
    }
}
