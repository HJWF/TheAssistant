using System.ComponentModel.DataAnnotations;

namespace TheAssistant.Core.Infrastructure
{
    public class ConsumerSettings
    {
        [Required]
        public string TenantId { get; set; } = string.Empty;
        [Required]
        public string ClientId { get; set; } = string.Empty;
        [Required]
        public string RedirectUri { get; set; } = string.Empty;
        [Required]
        public string ClientSecret { get; set; } = string.Empty;
    }

}
