using Newtonsoft.Json;

namespace TheAssistant.TheAssistantApi.Login.Notion
{
    public class NotionTokenResponse
    {
        [JsonProperty("access_token")]
        public string AccessToken { get; set; } = string.Empty;

        [JsonProperty("refresh_token")]
        public string RefreshToken { get; set; } = string.Empty;

        [JsonProperty("bot_id")]
        public string BotId { get; set; } = string.Empty;

        [JsonProperty("duplicated_template_id")]
        public string? DuplicatedTemplateId { get; set; }

        [JsonProperty("owner")]
        public string Owner { get; set; } = string.Empty;

        [JsonProperty("workspace_icon")]
        public string? WorkspaceIcon { get; set; }

        [JsonProperty("workspace_id")]
        public string WorkspaceId { get; set; } = string.Empty;

        [JsonProperty("workspace_name")]
        public string? WorkspaceName { get; set; }


    }
}
