namespace TheAssistant.Agents.ServiceAdapter.AI
{
    public class ChatHistory
    {
        private readonly List<ChatHistoryMessage> _messages = new();

        public void AddSystemMessage(string content)
            => _messages.Add(new("system", content));

        public void AddUserMessage(string content)
            => _messages.Add(new("user", content));

        public void AddAssistantMessage(string content)
            => _messages.Add(new("assistant", content));

        public void AddToolMessage(string toolCallId, string content)
            => _messages.Add(new("tool", content, toolCallId));

        public IReadOnlyList<ChatHistoryMessage> Messages => _messages;
    }
}
