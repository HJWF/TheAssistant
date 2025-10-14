using System.ComponentModel.DataAnnotations;

namespace TheAssistant.Core.Infrastructure
{
    public class NotionSettings
    {
        [Required]
        public string ClientId { get; set; } = string.Empty;

        [Required]
        public string ClientSecret { get; set; } = string.Empty;

        [Required]
        public string RedirectUri { get; set; } = string.Empty;

        [Required]
        public string McpUrl { get; set; } = string.Empty;
    }
}