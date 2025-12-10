namespace TheAssistant.Agents.ServiceAdapter.AI
{
    public record ChatHistoryMessage(
        string Role,
        string Content,
        string? ToolCallId = null);
}
