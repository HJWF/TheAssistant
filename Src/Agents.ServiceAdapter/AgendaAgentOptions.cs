using System.ComponentModel.DataAnnotations;

namespace TheAssistant.Agents.ServiceAdapter
{
    public class AgendaAgentOptions
    {

        [Required]
        public string PersonalEmail { get; set; } = string.Empty;
    }
}