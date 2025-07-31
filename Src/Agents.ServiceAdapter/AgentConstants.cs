namespace TheAssistant.Agents.ServiceAdapter
{
    public static class AgentConstants
    {
        public const string SorryMessage = "Sorry, I don't know how to handle that.";

        public static class Roles
        {
            public const string User = "user";
            public const string Agent = "agent";
            public const string Router = "router";
        }

        public static class Names
        {             
            public const string DailyUpdate = "dailyupdate-agent";
            public const string Agenda = "agenda-agent";
            public const string Weather = "weather-agent";
            public const string Formatting = "formatting-agent";
        }
    }
}
