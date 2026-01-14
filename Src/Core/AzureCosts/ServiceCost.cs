namespace TheAssistant.Core.AzureCosts
{
    public class ServiceCost
    {
        public string ServiceName { get; set; }
        public decimal Cost { get; set; }
        public decimal Percentage { get; set; }

        public ServiceCost(string serviceName, decimal cost, decimal percentage)
        {
            ServiceName = serviceName;
            Cost = cost;
            Percentage = percentage;
        }
    }
}
