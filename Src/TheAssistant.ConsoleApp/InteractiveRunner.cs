using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TheAssistant.Core;
using TheAssistant.Core.AzureCosts;
using TheAssistant.Core.Infrastructure;
using TheAssistant.Core.Messaging;
using TheAssistant.Core.Messaging.HandleQueuedMessage;
using TheAssistant.Core.Messaging.HandleReceiveMessages;
using TheAssistant.Core.Messaging.HandleDailyOverview;

namespace TheAssistant.ConsoleApp;

public class InteractiveRunner
{
    private readonly ILogger<InteractiveRunner> _logger;
    private readonly IAgentServiceAdapter _agentServiceAdapter;
    private readonly IAzureCostServiceAdapter _azureCostServiceAdapter;
    private readonly IWeatherServiceAdapter _weatherServiceAdapter;
    private readonly IAgendaServiceAdapter _agendaServiceAdapter;
    private readonly IMessageServiceAdapter _messageServiceAdapter;
    private readonly IServiceBusServiceAdapter? _serviceBusServiceAdapter;
    private readonly ICommandHandler<HandleQueuedMessageCommand> _handleQueuedMessageHandler;
    private readonly ICommandHandler<HandleReceiveMessagesCommand> _handleReceiveMessagesHandler;
    private readonly ICommandHandler<HandleDailyOverviewCommand> _handleDailyOverviewHandler;
    private readonly UserDetailsSettings _userDetails;

    public InteractiveRunner(
        ILogger<InteractiveRunner> logger,
        IAgentServiceAdapter agentServiceAdapter,
        IAzureCostServiceAdapter azureCostServiceAdapter,
        IWeatherServiceAdapter weatherServiceAdapter,
        IAgendaServiceAdapter agendaServiceAdapter,
        IMessageServiceAdapter messageServiceAdapter,
        IServiceBusServiceAdapter? serviceBusServiceAdapter,
        ICommandHandler<HandleQueuedMessageCommand> handleQueuedMessageHandler,
        ICommandHandler<HandleReceiveMessagesCommand> handleReceiveMessagesHandler,
        ICommandHandler<HandleDailyOverviewCommand> handleDailyOverviewHandler,
        IOptions<UserDetailsSettings> userDetailsOptions)
    {
        _logger = logger;
        _agentServiceAdapter = agentServiceAdapter;
        _azureCostServiceAdapter = azureCostServiceAdapter;
        _weatherServiceAdapter = weatherServiceAdapter;
        _agendaServiceAdapter = agendaServiceAdapter;
        _messageServiceAdapter = messageServiceAdapter;
        _serviceBusServiceAdapter = serviceBusServiceAdapter;
        _handleQueuedMessageHandler = handleQueuedMessageHandler;
        _handleReceiveMessagesHandler = handleReceiveMessagesHandler;
        _handleDailyOverviewHandler = handleDailyOverviewHandler;
        _userDetails = userDetailsOptions.Value;
    }

    public async Task RunAsync()
    {
        Console.Clear();
        Console.WriteLine("??????????????????????????????????????????????????????????");
        Console.WriteLine("?      TheAssistant - Interactive Console App           ?");
        Console.WriteLine("??????????????????????????????????????????????????????????");
        Console.WriteLine();

        bool exit = false;
        while (!exit)
        {
            DisplayMenu();
            var choice = Console.ReadLine()?.Trim();

            try
            {
                switch (choice)
                {
                    case "1":
                        await TestAzureCosts();
                        break;
                    case "2":
                        await TestWeather();
                        break;
                    case "3":
                        await TestAgenda();
                        break;
                    case "4":
                        await TestAgentMessage();
                        break;
                    case "5":
                        await TestReceiveMessages();
                        break;
                    case "6":
                        await TestQueuedMessage();
                        break;
                    case "7":
                        await TestDailyOverview();
                        break;
                    case "8":
                        await TestSendMessage();
                        break;
                    case "9":
                        DisplayUserDetails();
                        break;
                    case "0":
                    case "q":
                    case "quit":
                    case "exit":
                        exit = true;
                        break;
                    default:
                        Console.WriteLine("\n? Invalid choice. Please try again.");
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing menu option");
                Console.WriteLine($"\n? Error: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"   Inner Error: {ex.InnerException.Message}");
                }
            }

            if (!exit)
            {
                Console.WriteLine("\nPress any key to continue...");
                Console.ReadKey();
            }
        }

        Console.WriteLine("\n?? Goodbye!");
    }

    private void DisplayMenu()
    {
        Console.Clear();
        Console.WriteLine("??????????????????????????????????????????????????????????");
        Console.WriteLine("?                     MAIN MENU                          ?");
        Console.WriteLine("??????????????????????????????????????????????????????????");
        Console.WriteLine("?  Service Adapters:                                     ?");
        Console.WriteLine("?  1. Test Azure Costs                                   ?");
        Console.WriteLine("?  2. Test Weather                                       ?");
        Console.WriteLine("?  3. Test Agenda                                        ?");
        Console.WriteLine("?  4. Test Agent (Send Message to AI)                    ?");
        Console.WriteLine("?                                                        ?");
        Console.WriteLine("?  Full Workflows:                                       ?");
        Console.WriteLine("?  5. Receive Messages (Simulate Timer)                  ?");
        Console.WriteLine("?  6. Handle Queued Message (Simulate ServiceBus)        ?");
        Console.WriteLine("?  7. Generate Daily Overview                            ?");
        Console.WriteLine("?  8. Send Test Message                                  ?");
        Console.WriteLine("?                                                        ?");
        Console.WriteLine("?  Configuration:                                        ?");
        Console.WriteLine("?  9. Display User Details                               ?");
        Console.WriteLine("?                                                        ?");
        Console.WriteLine("?  0. Exit                                               ?");
        Console.WriteLine("??????????????????????????????????????????????????????????");
        Console.Write("\nSelect an option: ");
    }

    private async Task TestAzureCosts()
    {
        Console.WriteLine("\n??????????????????????????????????????????????????????????");
        Console.WriteLine("?              AZURE COSTS TEST                          ?");
        Console.WriteLine("??????????????????????????????????????????????????????????\n");

        try
        {
            Console.WriteLine("1. Testing Current Month Costs...");
            var currentMonthCosts = await _azureCostServiceAdapter.GetCurrentMonthCosts();
            DisplayCostSummary(currentMonthCosts);

            Console.WriteLine("\n2. Testing Previous Month Costs...");
            var previousMonthCosts = await _azureCostServiceAdapter.GetPreviousMonthCosts();
            DisplayCostSummary(previousMonthCosts);

            Console.WriteLine("\n3. Testing Custom Date Range (Last 7 Days)...");
            var endDate = DateTime.UtcNow.Date;
            var startDate = endDate.AddDays(-7);
            var customPeriodCosts = await _azureCostServiceAdapter.GetCostsForPeriod(startDate, endDate);
            DisplayCostSummary(customPeriodCosts);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("401") || ex.InnerException?.Message.Contains("401") == true)
        {
            Console.WriteLine("\n? Authentication Error (401 Unauthorized)\n");
            Console.WriteLine("This error means you're not authenticated or don't have permission to access Azure Cost Management.\n");
            
            Console.WriteLine("?? Troubleshooting Steps:\n");
            Console.WriteLine("1. Ensure you're logged in to Azure CLI:");
            Console.WriteLine("   az login\n");
            
            Console.WriteLine("2. Set the correct subscription:");
            Console.WriteLine("   az account set --subscription \"6e3989a8-f17c-46a2-b3c4-944f5e8b3a60\"\n");
            
            Console.WriteLine("3. Verify your current subscription:");
            Console.WriteLine("   az account show\n");
            
            Console.WriteLine("4. Check if you have 'Cost Management Reader' role:");
            Console.WriteLine("   az role assignment list --all --assignee $(az ad signed-in-user show --query id -o tsv)\n");
            
            Console.WriteLine("5. If you don't have the role, ask your Azure admin to grant it:");
            Console.WriteLine("   Role: 'Cost Management Reader'");
            Console.WriteLine("   Scope: Subscription level\n");
            
            Console.WriteLine("?? Alternative: You can also use the Azure Portal:");
            Console.WriteLine("   Subscriptions ? Access Control (IAM) ? Add role assignment");
            
            _logger.LogError(ex, "Azure Costs authentication failed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing Azure Costs");
            throw;
        }
    }

    private void DisplayCostSummary(AzureCostSummary summary)
    {
        Console.WriteLine($"\n?? Period: {summary.Period}");
        Console.WriteLine($"?? Total Cost: {summary.TotalCost:F2} {summary.Currency}");

        if (summary.TopServices.Any())
        {
            Console.WriteLine($"\n?? Top {summary.TopServices.Count} Services:");
            foreach (var service in summary.TopServices)
            {
                Console.WriteLine($"   • {service.ServiceName,-30} {service.Cost,10:F2} {summary.Currency} ({service.Percentage,5:F1}%)");
            }
        }
        else
        {
            Console.WriteLine("   No services found (possibly no costs in this period)");
        }
    }

    private async Task TestWeather()
    {
        Console.WriteLine("\n??????????????????????????????????????????????????????????");
        Console.WriteLine("?                  WEATHER TEST                          ?");
        Console.WriteLine("??????????????????????????????????????????????????????????\n");

        Console.Write("Enter latitude (default 52.3676): ");
        var latInput = Console.ReadLine();
        var latitude = string.IsNullOrWhiteSpace(latInput) ? "52.3676" : latInput;

        Console.Write("Enter longitude (default 4.9041): ");
        var lonInput = Console.ReadLine();
        var longitude = string.IsNullOrWhiteSpace(lonInput) ? "4.9041" : lonInput;

        var forecast = await _weatherServiceAdapter.GetWeather(latitude, longitude);

        Console.WriteLine($"\n?? Location: {latitude}, {longitude}");

        if (forecast.Daily?.Time != null && forecast.Daily.Time.Length > 0)
        {
            Console.WriteLine("\n?? Daily Forecast:");
            for (int i = 0; i < Math.Min(5, forecast.Daily.Time.Length); i++)
            {
                var alerts = forecast.Daily.Weather_Alerts.Length > i ? forecast.Daily.Weather_Alerts[i] : 0;
                var alertText = alerts > 0 ? " ?? Weather Alert!" : "";
                Console.WriteLine($"   {forecast.Daily.Time[i]}{alertText}");
            }
        }
        else
        {
            Console.WriteLine("\n??  No forecast data available");
        }

        if (forecast.Hourly != null)
        {
            Console.WriteLine("\n? Hourly data is available");
        }
    }

    private async Task TestAgenda()
    {
        Console.WriteLine("\n??????????????????????????????????????????????????????????");
        Console.WriteLine("?                  AGENDA TEST                           ?");
        Console.WriteLine("??????????????????????????????????????????????????????????\n");

        Console.WriteLine("??  Note: Agenda requires authentication tokens.");
        Console.WriteLine("This test requires a valid user token to access calendar events.");
        Console.WriteLine("Skipping for now - use the full workflow (option 6) to test with auth.\n");
        
        // Note: This would require token management which is complex for a simple test
        // In a real scenario, you'd need to implement authentication flow
        Console.WriteLine("To test agenda integration:");
        Console.WriteLine("1. Ensure you have valid authentication configured");
        Console.WriteLine("2. Use option 6 (Handle Queued Message) with a calendar-related query");
        Console.WriteLine("3. The agent will handle authentication and retrieve calendar events");
    }

    private async Task TestAgentMessage()
    {
        Console.WriteLine("\n??????????????????????????????????????????????????????????");
        Console.WriteLine("?              AGENT MESSAGE TEST                        ?");
        Console.WriteLine("??????????????????????????????????????????????????????????\n");

        Console.Write("Enter your message to the AI agent: ");
        var message = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(message))
        {
            Console.WriteLine("? Message cannot be empty.");
            return;
        }

        var userDetails = new UserDetails(
            _userDetails.PhoneNumber,
            _userDetails.PersonalMailTag,
            _userDetails.WorkMailTag);

        Console.WriteLine("\n?? Sending message to agent...");
        var response = await _agentServiceAdapter.HandleMessageAsync(message, userDetails);

        Console.WriteLine("\n?? Agent Response:");
        Console.WriteLine($"   {response}");
    }

    private async Task TestReceiveMessages()
    {
        Console.WriteLine("\n??????????????????????????????????????????????????????????");
        Console.WriteLine("?           RECEIVE MESSAGES TEST                        ?");
        Console.WriteLine("??????????????????????????????????????????????????????????\n");

        Console.WriteLine("?? Simulating timer trigger - checking for new messages...");
        await _handleReceiveMessagesHandler.Handle(new HandleReceiveMessagesCommand());
        Console.WriteLine("? Message check complete.");
    }

    private async Task TestQueuedMessage()
    {
        Console.WriteLine("\n??????????????????????????????????????????????????????????");
        Console.WriteLine("?         HANDLE QUEUED MESSAGE TEST                     ?");
        Console.WriteLine("??????????????????????????????????????????????????????????\n");

        Console.Write("Enter test message: ");
        var message = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(message))
        {
            Console.WriteLine("? Message cannot be empty.");
            return;
        }

        var userDetails = new UserDetails(
            _userDetails.PhoneNumber,
            _userDetails.PersonalMailTag,
            _userDetails.WorkMailTag);

        Console.WriteLine("\n?? Processing message through full pipeline...");
        await _handleQueuedMessageHandler.Handle(new HandleQueuedMessageCommand(message, userDetails));
        Console.WriteLine("? Message processed successfully.");
    }

    private async Task TestDailyOverview()
    {
        Console.WriteLine("\n??????????????????????????????????????????????????????????");
        Console.WriteLine("?           DAILY OVERVIEW TEST                          ?");
        Console.WriteLine("??????????????????????????????????????????????????????????\n");

        Console.WriteLine("?? Generating daily overview...");
        await _handleDailyOverviewHandler.Handle(new HandleDailyOverviewCommand(DateTime.UtcNow));
        Console.WriteLine("? Daily overview generated and sent.");
    }

    private async Task TestSendMessage()
    {
        Console.WriteLine("\n??????????????????????????????????????????????????????????");
        Console.WriteLine("?              SEND MESSAGE TEST                         ?");
        Console.WriteLine("??????????????????????????????????????????????????????????\n");

        Console.Write("Enter message to send: ");
        var messageText = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(messageText))
        {
            Console.WriteLine("? Message cannot be empty.");
            return;
        }

        Console.WriteLine("\n?? Sending message...");
        var messageId = await _messageServiceAdapter.SendMessageAsync(new Message(messageText));
        
        Console.WriteLine($"? Message sent successfully!");
        Console.WriteLine($"   Message ID: {messageId}");
    }

    private void DisplayUserDetails()
    {
        Console.WriteLine("\n??????????????????????????????????????????????????????????");
        Console.WriteLine("?              USER CONFIGURATION                        ?");
        Console.WriteLine("??????????????????????????????????????????????????????????\n");

        Console.WriteLine($"?? Phone Number: {_userDetails.PhoneNumber}");
        Console.WriteLine($"?? Personal Mail Tag: {_userDetails.PersonalMailTag}");
        Console.WriteLine($"?? Work Mail Tag: {_userDetails.WorkMailTag}");
    }
}
