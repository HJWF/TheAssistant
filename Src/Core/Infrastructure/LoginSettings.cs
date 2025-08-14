using System.ComponentModel.DataAnnotations;

namespace TheAssistant.Core.Infrastructure
{
    public class LoginSettings
    {
        [Required]
        public ConsumerSettings Consumer { get; set; }
    }
}
