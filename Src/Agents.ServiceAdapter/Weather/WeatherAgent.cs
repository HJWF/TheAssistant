using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Text.Json;
using TheAssistant.Core;
using TheAssistant.Core.Agents;

namespace TheAssistant.Agents.ServiceAdapter.Weather
{
    public class WeatherAgent : IWeatherAgent
    {
        private const string Prompt = """
                You are a helpful assistant summarizing the weather.

                Use the following weather information to generate a weather overview in Dutch.

                Always present the parts of the day in this exact order:
                === Ochtend ===
                === Middag ===
                === Avond ===
                === Nacht ===

                For each part of the day, use the following format:
                === [Part of Day] ===
                Temp (gem): [value] °C  
                Gevoel (gem): [value] °C  
                Regenkans (max): [value] %  
                Waarschuwingen: [True/False]

                Only translate the part-of-day labels to Dutch. Keep the rest as-is. Don't add any extra text or explanation.
            """;
        private readonly IWeatherServiceAdapter _weatherServiceAdapter;
        private readonly Kernel _kernel;
        private const string ApeldoornLatitude = "52.2112";
        private const string ApeldoornLongitude = "5.9699"; // Maybe in the future try Free Geocoding API (OpenStreetMap / Nominatim)

        public WeatherAgent(IWeatherServiceAdapter weatherServiceAdapter, Kernel kernel)
        {
            _weatherServiceAdapter = weatherServiceAdapter;
            _kernel = kernel;
        }

        public string Name => "weather-agent";

        [KernelFunction]
        public async Task<AgentMessage> HandleAsync(AgentMessage message)
        {
            var weather = await _weatherServiceAdapter.GetWeather(ApeldoornLatitude, ApeldoornLongitude);

            var chat = _kernel.GetRequiredService<IChatCompletionService>();
            var history = new ChatHistory();
            history.AddSystemMessage(Prompt);
            history.AddUserMessage(JsonSerializer.Serialize(weather));

            var reply = await chat.GetChatMessageContentAsync(history);

            return 
                new AgentMessage
                {
                    Sender = Name,
                    Receiver = "user",
                    Role = "agent",
                    Content = reply.Content ?? AgentConstants.SorryMessage
            };
        }
    }
}
