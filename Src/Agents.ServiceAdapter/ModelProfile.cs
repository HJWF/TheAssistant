using System.ComponentModel.DataAnnotations;

namespace TheAssistant.Agents.ServiceAdapter;

public class ModelProfile
{
    [Required]
    public string DeploymentName { get; set; } = string.Empty;
}