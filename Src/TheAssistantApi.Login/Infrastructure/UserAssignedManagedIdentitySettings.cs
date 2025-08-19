using System.ComponentModel.DataAnnotations;

namespace TheAssistant.TheAssistantApi.Login.Infrastructure
{
    public class UserAssignedManagedIdentitySettings
    {
        [Required]
        public string ClientId { get; set; } = string.Empty;
        public string TenantId { get; set; } = string.Empty;
    }
}
