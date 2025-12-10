using System.Text.Json;
using TheAssistant.Core;
using TheAssistant.Core.Agents;
using TheAssistant.Agents.ServiceAdapter.AI;

namespace TheAssistant.Agents.ServiceAdapter.Weather
{
    public class WeatherAgent : IWeatherAgent
    {
        private const string Prompt = """
                You are a helpful assistant summarizing weather information.

                Use the weather data provided (not below) and format it **exactly** like this:

                Morning  
                Temp: 18°C / Feels: 17°C / Rain: 30%

                Afternoon  
                Temp: 23°C / Feels: 22°C / Rain: 10%

                Evening  
                Temp: 20°C / Feels: 19°C / Rain: 15%

                Night  
                Temp: 16°C / Feels: 15°C / Rain: 40%

                Rules:
                - Output must include **only** these four parts of the day: Morning, Afternoon, Evening, Night, in this order.
                - Keep label order and punctuation **exactly** as in the example.
                - Do **not** add any extra text, explanation, or units beyond what is shown.
            """;
        private readonly IWeatherServiceAdapter _weatherServiceAdapter;
        private readonly IChatCompletionService _chat;
        private const string ApeldoornLatitude = "52.2112";
        private const string ApeldoornLongitude = "5.9699"; // Maybe in the future try Free Geocoding API (OpenStreetMap / Nominatim)

        public WeatherAgent(IWeatherServiceAdapter weatherServiceAdapter, IChatCompletionService chat)
        {
            _weatherServiceAdapter = weatherServiceAdapter;
            _chat = chat;
        }

        public string Name => AgentConstants.Names.Weather;

        public async Task<IEnumerable<AgentMessage>> HandleAsync(AgentMessage message, CancellationToken cancellationToken = default)
        {
            var weather = await _weatherServiceAdapter.GetWeather(ApeldoornLatitude, ApeldoornLongitude);

            var history = new ChatHistory();
            history.AddSystemMessage(Prompt);
            history.AddUserMessage(JsonSerializer.Serialize(weather));

            var reply = await _chat.GetChatMessageContentAsync(history, cancellationToken: cancellationToken);

            return new List<AgentMessage> { new AgentMessage(message.User, Name, AgentConstants.Roles.User, AgentConstants.Roles.Agent, reply.Content ?? AgentConstants.SorryMessage, null) };
        }
    }
}
