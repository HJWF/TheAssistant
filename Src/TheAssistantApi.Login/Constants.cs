namespace TheAssistant.TheAssistantApi.Login
{
    public static class Constants
    {
        public static class SectionNames
        {
            public const string UserDetails = "UserDetails";
            public const string Login = "Login";
            public const string Agents = "Agents";
            public const string Weather = "Weather";
            public const string Signal = "Signal";
            public const string ServiceBus = "ServiceBus";
            public const string TokenStore = "TokenStore";
            public const string UserAssignedManagedIdentity = "UserAssignedManagedIdentity";
        }

        public static class Routes
        {
            public const string ConsumerStart = "login/Consumer/start";
            public const string ConsumerCallback = "login/Consumer/callback";
        }
    }
}
