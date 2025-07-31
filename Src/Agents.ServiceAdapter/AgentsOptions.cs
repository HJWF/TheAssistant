using System.ComponentModel.DataAnnotations;

namespace TheAssistant.Agents.ServiceAdapter
{
    public class AgentsOptions
    {
        [Required]
        public string AzureOpenAiDeploymentName { get; set; } = string.Empty;

        [Required]
        public string AzureOpenAiEndpoint { get; set; } = string.Empty;

        [Required]
        public string AzureOpenAiApiKey { get; set; } = string.Empty;

        [Required]
        public AgendaAgentOptions AgendaAgent { get; set; } = new AgendaAgentOptions();
    }
}