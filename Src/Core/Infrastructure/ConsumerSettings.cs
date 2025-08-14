using System.ComponentModel.DataAnnotations;

namespace TheAssistant.Core.Infrastructure
{
    public class ConsumerSettings
    {
        [Required]
        public string TenantId { get; set; }
        [Required]
        public string ClientId { get; set; }
        [Required]
        public string RedirectUri { get; set; }
        [Required]
        public string ClientSecret { get; set; }
    }

}
