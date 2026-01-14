using System.ComponentModel.DataAnnotations;

namespace TheAssistant.AzureCosts.ServiceAdapter
{
    /// <summary>
    /// Configuration settings for Azure Cost Management integration
    /// </summary>
    public class AzureCostSettings
    {
        /// <summary>
        /// Azure Subscription ID to query costs for
        /// </summary>
        [Required]
        public string SubscriptionId { get; set; } = string.Empty;

        /// <summary>
        /// Optional: Azure Tenant ID (for Service Principal auth)
        /// Not needed when using Managed Identity or Azure CLI
        /// </summary>
        public string? TenantId { get; set; }

        /// <summary>
        /// Optional: Client ID for Service Principal authentication
        /// Not needed when using Managed Identity or Azure CLI
        /// </summary>
        public string? ClientId { get; set; }

        /// <summary>
        /// Optional: Client Secret for Service Principal authentication
        /// Not needed when using Managed Identity or Azure CLI
        /// Should be stored in Azure Key Vault in production
        /// </summary>
        public string? ClientSecret { get; set; }
    }
}
