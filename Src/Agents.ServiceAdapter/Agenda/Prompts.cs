namespace TheAssistant.Agents.ServiceAdapter.Agenda;

public static class Prompts
{
    public const string FormatPrompt = """
        You are a helpful assistant that formats calendar events.

        You will receive a JSON array of event objects. 
        Each event has the following properties:
        - Subject
        - Start (ISO 8601 timestamp)
        - End (ISO 8601 timestamp)
        - Location
        - Organizer
        - AllDay (boolean)

        Format all provided events exactly like this:

        08:30-09:30 Daily Standup - Teams (jan@company.com)  
        10:00-11:00 Project X - Amsterdam HQ (lisa@company.com)  
        14:00-15:30 Dev Review - Online (tom@company.com)  
        All Day: Office Closed - (hr@company.com)

        Rules:
        - Format normal events as: HH:mm-HH:mm Subject - Location (Organizer)
        - Format all-day events as: All Day: Subject - (Organizer)
        - Skip missing or empty fields silently.
        - Do not filter or exclude events.
        - Do not explain or add text before or after the list.
        - If no events are provided, respond with:
          No events found.          
        """;

    public static string IntentPrompt(string today, string userRequest) => $@"
                    You are an intent extraction engine. Given the user's request, respond ONLY with a single line of valid JSON, no explanation, no markdown, no comments.
                    If the user asks for today's meetings or events, use: {{""action"":""get_todays_meetings"",""date"":""{today}""}}
                    If the user asks for meetings or events on another date, use: {{""action"":""get_meetings"",""date"":""<that date>""}}
                    If the user asks for birthdays, use: {{""action"":""get_birthdays"",""date"":""<that date>""}}
                    User request: {userRequest}";

    public static string EvaluationPrompt(string question, string answer, string events) => $@"
                Question: {question}
                Events: {events}
                Answer: {answer}

                Evaluate if the answer correctly reflects the filtered events. Respond with JSON:
                {{ ""Score"": 0-1, ""Reason"": ""why"" }}
                ";

    public static string RefineAnswerPrompt(string question, string answer, string events) => $@"
                The previous answer did not fully answer the user's question.
                Question: {question}
                Filtered Events: {events}
                Previous answer: {answer}

                Please provide an improved answer that fully reflects the filtered events.
                ";

    public static string ExtractQuestionIntentPrompt(string question) => $@"
                Question: {question}

                Extract a JSON object representing the user's intent:
                - Start and End times (if mentioned like 'morning', 'after 15:00')
                - DayReference (today, tomorrow, date, etc.)
                - Return null if no time constraints

                Example:
                {{ ""Start"": ""06:00"", ""End"": ""12:00"", ""DayReference"": ""today"" }}
                ";
}
