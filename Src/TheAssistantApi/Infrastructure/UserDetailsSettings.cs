using System.ComponentModel.DataAnnotations;

namespace TheAssistant.TheAssistantApi.Infrastructure
{
    public class UserDetailsSettings
    {
        [Required]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required]
        public string PersonalMailTag { get; set; } = string.Empty;

        [Required]
        public string WorkMailTag { get; set; } = string.Empty;

    }
}
