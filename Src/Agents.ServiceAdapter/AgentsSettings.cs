using System.ComponentModel.DataAnnotations;

namespace TheAssistant.Agents.ServiceAdapter
{
    public class TokenCostSettings
    {
        // Azure gpt-4o-mini pay-as-you-go defaults
        public decimal InputPricePerMillionTokens { get; set; } = 0.15m;
        public decimal OutputPricePerMillionTokens { get; set; } = 0.60m;
        public decimal UsdToEurRate { get; set; } = 0.92m;
    }

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

        public Dictionary<string, ModelProfile> Models { get; set; } = new();

        public TokenCostSettings TokenCost { get; set; } = new();
    }
}