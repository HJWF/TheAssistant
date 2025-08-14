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
        private readonly Kernel _kernel;
        private const string ApeldoornLatitude = "52.2112";
        private const string ApeldoornLongitude = "5.9699"; // Maybe in the future try Free Geocoding API (OpenStreetMap / Nominatim)

        public WeatherAgent(IWeatherServiceAdapter weatherServiceAdapter, Kernel kernel)
        {
            _weatherServiceAdapter = weatherServiceAdapter;
            _kernel = kernel;
        }

        public string Name => AgentConstants.Names.Weather;

        [KernelFunction]
        public async Task<IEnumerable<AgentMessage>> HandleAsync(AgentMessage message)
        {
            var weather = await _weatherServiceAdapter.GetWeather(ApeldoornLatitude, ApeldoornLongitude);

            var chat = _kernel.GetRequiredService<IChatCompletionService>();
            var history = new ChatHistory();
            history.AddSystemMessage(Prompt);
            history.AddUserMessage(JsonSerializer.Serialize(weather));

            var reply = await chat.GetChatMessageContentAsync(history);

            return new List<AgentMessage> { new AgentMessage(message.User, Name, AgentConstants.Roles.User, AgentConstants.Roles.Agent, reply.Content ?? AgentConstants.SorryMessage, null) };
        }
    }
}
