using System.ComponentModel.DataAnnotations;

namespace TheAssistant.Agents.ServiceAdapter
{
    public class AgentsSettings
    {
        [Required]
        public string AzureOpenAiDeploymentName { get; set; } = string.Empty;

        [Required]
        public string AzureOpenAiEndpoint { get; set; } = string.Empty;

        [Required]
        public string AzureOpenAiApiKey { get; set; } = string.Empty;

        [Required]
        public AgendaAgentSettings AgendaAgent { get; set; } = new AgendaAgentSettings();
    }
}