using System.ComponentModel;
using System.Text.Json;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using TheAssistant.Core;
using TheAssistant.Core.Agents;

namespace TheAssistant.Agents.ServiceAdapter.Weather
{
    public class WeatherAgent : IWeatherAgent
    {
        private const string SystemPrompt = """
            You are a weather assistant with access to tools for retrieving weather forecasts.
            
            IMPORTANT:
            - You MUST use the available tools to get weather data. Do not make up weather information.
            - Current date context: Today is {CurrentDate}.
            - Format weather information clearly with temperature, "feels like" temp, and rain probability
            - Organize by time of day: Morning, Afternoon, Evening, Night
            - Be concise and user-friendly
            
            Available tools:
            - GetWeatherForDefaultLocation: Use when no specific location is mentioned
            - GetWeatherForLocation: Use when user specifies a city or location
            
            Process:
            1. Call the appropriate weather tool
            2. Wait for the tool result
            3. Format the weather data by time of day
            4. Include temperature, feels-like, and rain probability
            5. Be concise
            """;

        private readonly IWeatherServiceAdapter _weatherServiceAdapter;
        private readonly IChatClient _chatClient;
        private readonly ILogger<WeatherAgent> _logger;
        private readonly ITokenUsageTracker _tokenUsageTracker;
        
        // Default location (Apeldoorn)
        private const string DefaultLatitude = "52.2112";
        private const string DefaultLongitude = "5.9699";

        public string Name => AgentConstants.Names.Weather;
        public string Description => "For weather forecasts and current conditions at any location.";

        public WeatherAgent(
            IWeatherServiceAdapter weatherServiceAdapter,
            IChatClient chatClient,
            ILogger<WeatherAgent> logger,
            ITokenUsageTracker tokenUsageTracker)
        {
            _weatherServiceAdapter = weatherServiceAdapter;
            _chatClient = chatClient;
            _logger = logger;
            _tokenUsageTracker = tokenUsageTracker;
        }

        [Description("Gets weather forecast for the default location (Apeldoorn)")]
        public async Task<string> GetWeatherForDefaultLocation()
        {
            try
            {
                var weather = await _weatherServiceAdapter.GetWeather(DefaultLatitude, DefaultLongitude);
                return JsonSerializer.Serialize(weather);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching weather for default location");
                return JsonSerializer.Serialize(new { error = "Failed to fetch weather data" });
            }
        }

        [Description("Gets weather forecast for a specific city or location")]
        public async Task<string> GetWeatherForLocation(
            [Description("City name (e.g., 'Amsterdam', 'Utrecht', 'Rotterdam')")] string city)
        {
            // Map common Dutch cities to coordinates
            // In a real implementation, you'd use a geocoding API
            var (latitude, longitude) = GetCoordinatesForCity(city);

            if (latitude == null || longitude == null)
            {
                return JsonSerializer.Serialize(new 
                { 
                    error = $"Location '{city}' not found. Supported cities: Amsterdam, Utrecht, Rotterdam, Den Haag, Apeldoorn",
                    supportedCities = new[] { "Amsterdam", "Utrecht", "Rotterdam", "Den Haag", "Apeldoorn" }
                });
            }

            try
            {
                var weather = await _weatherServiceAdapter.GetWeather(latitude, longitude);
                return JsonSerializer.Serialize(new 
                { 
                    location = city,
                    weather 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching weather for {City}", city);
                return JsonSerializer.Serialize(new { error = $"Failed to fetch weather for {city}" });
            }
        }

        public async Task<IEnumerable<AgentMessage>> HandleAsync(
            AgentMessage message, 
            CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;
            var systemPrompt = SystemPrompt
                .Replace("{CurrentDate}", now.ToString("yyyy-MM-dd"));

            var messages = new List<ChatMessage>
            {
                new(ChatRole.System, systemPrompt),
                new(ChatRole.User, message.Content)
            };

            var tools = new List<AITool>
            {
                AIFunctionFactory.Create(GetWeatherForDefaultLocation),
                AIFunctionFactory.Create(GetWeatherForLocation)
            };

            try
            {
                var response = await _chatClient.GetResponseAsync(
                    messages,
                    new ChatOptions { Tools = tools },
                    cancellationToken);

                _tokenUsageTracker.Track(response.Usage);

                return [new AgentMessage(
                    message.User,
                    Name,
                    AgentConstants.Roles.User,
                    AgentConstants.Roles.Agent,
                    !string.IsNullOrWhiteSpace(response.Text) ? response.Text : AgentConstants.SorryMessage,
                    null)];
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling weather request");
                return [new AgentMessage(
                    message.User,
                    Name,
                    AgentConstants.Roles.User,
                    AgentConstants.Roles.Agent,
                    "Sorry, I couldn't fetch the weather forecast.",
                    null)];
            }
        }

        private static (string? latitude, string? longitude) GetCoordinatesForCity(string city)
        {
            // Simple city mapping - in production, use a geocoding API like OpenStreetMap Nominatim
            return city.ToLowerInvariant() switch
            {
                "amsterdam" => ("52.3676", "4.9041"),
                "utrecht" => ("52.0907", "5.1214"),
                "rotterdam" => ("51.9225", "4.4792"),
                "den haag" or "the hague" or "denhaag" => ("52.0705", "4.3007"),
                "apeldoorn" => (DefaultLatitude, DefaultLongitude),
                "eindhoven" => ("51.4416", "5.4697"),
                "groningen" => ("53.2194", "6.5665"),
                "tilburg" => ("51.5555", "5.0913"),
                "almere" => ("52.3508", "5.2647"),
                "breda" => ("51.5719", "4.7683"),
                _ => (null, null)
            };
        }
    }
}
