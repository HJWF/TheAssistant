using TheAssistant.Core.Agents;

namespace TheAssistant.Agents.ServiceAdapter.DailyUpdate
{
    public class DailyUpdateAgent : IDailyUpdateAgent
    {
        public string Name => "dailyupdate-agent";

        public Task<AgentMessage> HandleAsync(AgentMessage message)
        {
            return Task.FromResult(
                new AgentMessage
                {
                    Sender = Name,
                    Receiver = "router",
                    Role = "agent",
                    Content = "What is the weather and do i have a meeting at 9pm?",
                    Metadata = new Dictionary<string, string>
                    {
                        { "needMeetingInformation", "true" },
                        { "needsWeatherInformation", "true" }
                    }
                });
        }
    }
}
