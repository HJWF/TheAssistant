namespace TheAssistant.Agents.ServiceAdapter.Dispatcher
{
    public static class DispatcherConstants
    {
        public const string Prompt = @"
        You are an intelligent assistant. Based on the user's message, pick the correct skill (plugin) and function to call.

        Skills:
        - Agenda -> GetDailyAgenda

        Always respond with:
        skill: [SkillName]
        function: [FunctionName]
        arguments: { ""key"": ""value"" }

        Example:
        skill: Agenda
        function: GetDailyAgenda
        arguments: { ""date"": ""2025-07-10"" }

        User input: {{input}}
        ";

    }
}
