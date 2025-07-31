using System.ComponentModel.DataAnnotations;

namespace TheAssistant.TokenStore.ServiceAdapter
{
    public class TokenStoreSettings
    {
        [Required]
        public string VaultUrl { get; set; } = string.Empty;
    }
}