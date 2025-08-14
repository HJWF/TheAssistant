namespace TheAssistant.Agents.ServiceAdapter
{
    public static class Prompts
    {
        public const string FormatPrompt = """
                You are a helpful assistant formatting calendar events.

                Use the event data provided (not below) and format it **exactly** like this:

                08:30-09:30 Daily Standup - Teams (jan@company.com)  
                10:00-11:00 Project X - Amsterdam HQ (lisa@company.com)  
                14:00-15:30 Dev Review - Online (tom@company.com)  
                All Day: Office Closed - (hr@company.com)

                Rules:
                - Format normal events as HH:mm-HH:mm Title - Location (Organizer)
                - Format all-day events as: All Day: Title - (Organizer)
                - Do **not** include any explanation or extra text before or after the list.
                - If no events are found, respond with:  
                  No events found for today.
                - If any field is missing or empty, skip that part silently.
            
            """;

        public static string IntentPrompt(string today, string userRequest) => $@"
                    You are an intent extraction engine. Given the user's request, respond ONLY with a single line of valid JSON, no explanation, no markdown, no comments.
                    If the user asks for today's meetings, use: {{""action"":""get_todays_meetings"",""date"":""{today}""}}
                    If the user asks for meetings on another date, use: {{""action"":""get_meetings"",""date"":""<that date>""}}
                    If the user asks for birthdays, use: {{""action"":""get_birthdays"",""date"":""<that date>""}}
                    User request: {userRequest}";
    }
}
