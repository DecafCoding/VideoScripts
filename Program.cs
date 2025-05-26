using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VideoScripts.Configuration;
using VideoScripts.Core;
using VideoScripts.Features.ShowClusters;

namespace VideoScripts;

internal class Program
{
    static async Task Main(string[] args)
    {
        // Setup configuration
        var config = ConfigurationSetup.BuildConfiguration();

        // Setup dependency injection
        var services = new ServiceCollection();
        ServiceConfiguration.ConfigureServices(services, config);
        var serviceProvider = services.BuildServiceProvider();

        // Setup and migrate database
        await DatabaseSetup.InitializeDatabaseAsync(config);

        // Display startup information
        ConsoleOutput.DisplayStartupInfo(config);

        // Get the show clusters service
        var showClustersService = serviceProvider.GetRequiredService<ShowClustersService>();

        // Main menu - ask user what they want to do
        var choice = GetUserChoice();

        if (choice == "1")
        {
            // Show clusters
            await showClustersService.DisplayClusterMenuAsync();
        }
        else if (choice == "2")
        {
            // Continue with normal processing
            await ExecuteNormalProcessingAsync(config, serviceProvider);
        }

        // Clean up
        await serviceProvider.DisposeAsync();
    }

    /// <summary>
    /// Gets user's choice for what to do
    /// </summary>
    private static string GetUserChoice()
    {
        ConsoleOutput.DisplaySectionHeader("MAIN MENU");
        Console.WriteLine("What would you like to do?");
        Console.WriteLine();
        Console.WriteLine("1. View Project Clusters");
        Console.WriteLine("2. Process Videos (Import/Transcripts/Topics/Summaries)");
        Console.WriteLine();

        while (true)
        {
            var input = ConsoleOutput.GetUserInput("Enter your choice (1 or 2):");

            if (input == "1" || input == "2")
            {
                return input;
            }

            Console.WriteLine("Please enter 1 or 2.");
        }
    }

    /// <summary>
    /// Executes the normal processing workflow (existing functionality)
    /// </summary>
    private static async Task ExecuteNormalProcessingAsync(IConfiguration config, ServiceProvider serviceProvider)
    {
        // Initialize Google Sheets service
        var googleService = GoogleSheetsSetup.InitializeGoogleService(config);
        if (googleService == null) return;

        var spreadsheetId = googleService.FindSpreadsheetIdByName(config["Google:SpreadsheetName"]);
        if (spreadsheetId == null)
        {
            ConsoleOutput.DisplayError($"Spreadsheet '{config["Google:SpreadsheetName"]}' not found.");
            return;
        }

        // Get processing handlers from DI
        var processingOrchestrator = serviceProvider.GetRequiredService<ProcessingOrchestrator>();

        // Execute main processing workflow
        await processingOrchestrator.ExecuteMainWorkflowAsync(googleService, spreadsheetId);
    }
}