using System.ComponentModel.DataAnnotations;

namespace TheAssistant.Agents.ServiceAdapter
{
    public class AgendaAgentSettings
    {

        [Required]
        public string PersonalEmail { get; set; } = string.Empty;
    }
}