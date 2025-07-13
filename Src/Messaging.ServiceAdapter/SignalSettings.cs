using System.ComponentModel.DataAnnotations;

namespace TheAssistant.Messaging.ServiceAdapter
{
    public class SignalSettings
    {
        [Required]
        public string BaseUrl { get; set; } = string.Empty;

        [Required]
        public string PhoneNumber { get; set; } = string.Empty;
    }
}
