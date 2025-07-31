using System.ComponentModel.DataAnnotations;

namespace TheAssistant.Agenda.ServiceAdapter
{
    public class AgendaSettings
    {
        [Required]
        public string TenantId { get; set; } = string.Empty;

        [Required]
        public string ClientId { get; set; } = string.Empty;

        [Required]
        public string ClientSecret { get; set; } = string.Empty;
    }
}
